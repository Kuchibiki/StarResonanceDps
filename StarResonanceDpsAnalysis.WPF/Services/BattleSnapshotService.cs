using System.IO;
using System.Reflection;
using System.Collections.ObjectModel; // Added for ObservableCollection
using System.Windows; // Added for Application.Current.Dispatcher
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StarResonanceDpsAnalysis.Core.Data;
using StarResonanceDpsAnalysis.Core.Data.Models;
using StarResonanceDpsAnalysis.Core.Statistics;
using StarResonanceDpsAnalysis.WPF.Config;
using StarResonanceDpsAnalysis.WPF.Models;

namespace StarResonanceDpsAnalysis.WPF.Services;

public record SnapshotInfo(string Title, string FilePath)
{
	public static SnapshotInfo FromSnapshotData(BattleSnapshotData d)
	{
		return new SnapshotInfo($"{d.StartedAt:HH:mm:ss} ({d.Duration:mm\\:ss})", d.FilePath);
	}
}

/// <summary>
/// 战斗快照服务 - 负责保存和加载战斗快照
/// </summary>
public class BattleSnapshotService
{
    private const int AbsoluteMinDurationSeconds = 10; // 绝对最小战斗时长(秒),低于此值的战斗永远不保存
	private readonly IConfigManager _configManager;
	private readonly ILogger<BattleSnapshotService> _logger;
	private readonly string _snapshotDirectory;
	private readonly FileSystemWatcher _fileWatcher; // Watcher for real-time file detection

	public BattleSnapshotService(ILogger<BattleSnapshotService> logger, IConfigManager configManager)
	{
		_logger = logger;
		_configManager = configManager;
		_snapshotDirectory = Path.Combine(Environment.CurrentDirectory, "BattleSnapshots");

        // 确保目录存在
		if (!Directory.Exists(_snapshotDirectory))
		{
			Directory.CreateDirectory(_snapshotDirectory);
		}

        // 启动时加载现有快照
		LoadSnapshots();

		// Setup FileSystemWatcher to detect new JSON files in real-time (e.g., manual copies)
		_fileWatcher = new FileSystemWatcher(_snapshotDirectory, "*.json")
		{
			EnableRaisingEvents = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
		};
		_fileWatcher.Created += OnSnapshotFileCreated;
	}

	private int MaxSnapshots => _configManager.CurrentConfig.MaxHistoryCount;

	/// <summary>
	/// 当前战斗快照列表(最新的N条，N由配置决定)
	/// Changed to ObservableCollection for real-time UI updates
	/// </summary>
	public ObservableCollection<SnapshotInfo> CurrentSnapshots { get; } = new();

	/// <summary>
	/// 全程快照列表(最新的N条，N由配置决定)
	/// Changed to ObservableCollection for real-time UI updates
	/// </summary>
	public ObservableCollection<SnapshotInfo> TotalSnapshots { get; } = new();

	/// <summary>
    /// 保存当前战斗快照
	/// </summary>
	private void OnSnapshotFileCreated(object sender, FileSystemEventArgs e)
	{
		// Brief delay to ensure the file is fully written by the OS
		Thread.Sleep(500);

		var snapshot = LoadSnapshot(e.FullPath);
		if (snapshot != null)
		{
			// Must update ObservableCollection on the UI Thread
			Application.Current.Dispatcher.Invoke(() =>
			{
				UpdateSnapshotList(snapshot);
			});
		}
	}

	/// <summary>
	/// Helper to add a snapshot to the correct list and maintain the MaxSnapshots limit
	/// </summary>
	private void UpdateSnapshotList(BattleSnapshotData snapshot)
	{
		var targetList = snapshot.ScopeType == ScopeTime.Current ? CurrentSnapshots : TotalSnapshots;
		var newInfo = SnapshotInfo.FromSnapshotData(snapshot);

		// Prevent duplicate entries if the file was just saved by this service
		if (targetList.Any(x => x.FilePath == newInfo.FilePath)) return;

		targetList.Insert(0, newInfo);

		// Keep only the latest N entries, release memory and delete disk files for overflow
		while (targetList.Count > MaxSnapshots)
		{
			var oldest = targetList[targetList.Count - 1];
			targetList.RemoveAt(targetList.Count - 1);
			TryDeleteSnapshotFile(oldest.FilePath);
			_logger.LogDebug("Removed old snapshot: {Path}, file deleted", oldest.FilePath);
		}
	}

	/// <param name="storage">数据存储</param>
	/// <param name="duration">战斗时长</param>
	/// <param name="minDurationSeconds">用户设置的最小时长(秒),0表示记录所有(默认记录所有)</param>
	/// <param name="forceUseFullData">强制使用FullDpsData(用于脱战时sectioned数据已被清空的情况)</param>
	public void SaveCurrentSnapshot(IDataStorage storage, TimeSpan duration, int minDurationSeconds = 0,
		bool forceUseFullData = false)
	{
        // ⭐ 硬性限制: 低于10秒的战斗永远不保存
		if (duration.TotalSeconds < AbsoluteMinDurationSeconds)
		{
            _logger.LogInformation("战斗时长不足{Min}秒({Actual:F1}秒),跳过保存当前快照(硬性限制)",
				AbsoluteMinDurationSeconds, duration.TotalSeconds);
			return;
		}

        // ⭐ 用户设置的过滤条件(可选)
		if (minDurationSeconds > 0 && duration.TotalSeconds < minDurationSeconds)
		{
            _logger.LogInformation("战斗时长不足用户设置的{UserMin}秒({Actual:F1}秒),跳过保存当前快照(用户设置)",
				minDurationSeconds, duration.TotalSeconds);
			return;
		}

		try
		{
            // ⭐ 关键修复: 如果forceUseFullData=true,则使用FullDpsData创建快照
			var scope = forceUseFullData ? ScopeTime.Total : ScopeTime.Current;
			var snapshot = CreateSnapshot(storage, duration, scope);

            // 保存到磁盘
			SaveSnapshotToDisk(snapshot);

			// Update UI list in real-time via Dispatcher
			Application.Current.Dispatcher.Invoke(() => UpdateSnapshotList(snapshot));

			_logger.LogInformation("保存当前战斗快照成功: {Time}, 时长: {Duration:F1}秒, 数据源: {Source}, 当前保存数量: {Count}/{Max}",
				snapshot.StartedAt, duration.TotalSeconds, forceUseFullData ? "FullData" : "SectionedData",
				CurrentSnapshots.Count, MaxSnapshots);
		}
		catch (Exception ex)
		{
            _logger.LogError(ex, "保存当前战斗快照失败");
		}
	}

	/// <summary>
    /// 保存全程快照
	/// </summary>
    /// <param name="storage">数据存储</param>
    /// <param name="duration">战斗时长</param>
    /// <param name="minDurationSeconds">用户设置的最小时长(秒),0表示记录所有(默认记录所有)</param>
	public void SaveTotalSnapshot(IDataStorage storage, TimeSpan duration, int minDurationSeconds = 0)
	{
        // ? 硬性限制: 低于10秒的战斗永远不保存
		if (duration.TotalSeconds < AbsoluteMinDurationSeconds)
		{
            _logger.LogInformation("战斗时长不足{Min}秒({Actual:F1}秒),跳过保存全程快照(硬性限制)",
				AbsoluteMinDurationSeconds, duration.TotalSeconds);
			return;
		}

        // ? 用户设置的过滤条件(可选)
		if (minDurationSeconds > 0 && duration.TotalSeconds < minDurationSeconds)
		{
            _logger.LogInformation("战斗时长不足用户设置的{UserMin}秒({Actual:F1}秒),跳过保存全程快照(用户设置)",
				minDurationSeconds, duration.TotalSeconds);
			return;
		}

		try
		{
			var snapshot = CreateSnapshot(storage, duration, ScopeTime.Current);

            // 保存到磁盘
			SaveSnapshotToDisk(snapshot);

			// Update UI list in real-time via Dispatcher
			Application.Current.Dispatcher.Invoke(() => UpdateSnapshotList(snapshot));

			_logger.LogInformation("保存全程快照成功: {Time}, 时长: {Duration:F1}秒, 当前保存数量: {Count}/{Max}",
				snapshot.StartedAt, duration.TotalSeconds, TotalSnapshots.Count, MaxSnapshots);
		}
		catch (Exception ex)
		{
            _logger.LogError(ex, "保存全程快照失败");
		}
	}

	public BattleSnapshotData? LoadSnapshot(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
                _logger.LogWarning("快照文件不存在: {File}", filePath);
				return null;
			}

			var json = File.ReadAllText(filePath);
			var settings = new JsonSerializerSettings()
			{
				ContractResolver = new PrivateSetterContractResolver()
			};
			var snapshot = JsonConvert.DeserializeObject<BattleSnapshotData>(json, settings);

			if (snapshot != null)
			{
				snapshot.FilePath = filePath;
                _logger.LogDebug("成功加载快照: {File}", filePath);
			}
			else
			{
                _logger.LogWarning("反序列化快照失败: {File}", filePath);
			}

			return snapshot;
		}
		catch (Exception ex)
		{
            _logger.LogError(ex, "加载快照失败: {File}", filePath);
			return null;
		}
	}

	/// <summary>
    /// 创建快照
	/// </summary>
	private BattleSnapshotData CreateSnapshot(IDataStorage storage, TimeSpan duration, ScopeTime scopeType)
	{
		var now = DateTime.Now;
		var players = new Dictionary<long, PlayerInfo>();
		var statistics = new Dictionary<long, PlayerStatistics>();

        // 根据类型选择数据源
		var dpsList = storage.GetStatistics(scopeType == ScopeTime.Total);

		ulong teamTotalDamage = 0;
		ulong teamTotalHealing = 0;
		ulong teamTotalTaken = 0;

		foreach (var dpsData in dpsList.Values)
		{
			var damage = (ulong)Math.Max(0, dpsData.AttackDamage.Total);
			var healing = (ulong)Math.Max(0, dpsData.Healing.Total);
			var taken = (ulong)Math.Max(0, dpsData.TakenDamage.Total);

			teamTotalDamage += damage;
			teamTotalHealing += healing;
			teamTotalTaken += taken;

			var foundPlayerInfo = storage.ReadOnlyPlayerInfoDatas.TryGetValue(dpsData.Uid, out var playerInfo);
			players[dpsData.Uid] = foundPlayerInfo ? playerInfo! : new PlayerInfo() { UID = dpsData.Uid };
			statistics[dpsData.Uid] = dpsData;
		}

		return new BattleSnapshotData
		{
			ScopeType = scopeType,
			StartedAt = now.AddTicks(-duration.Ticks),
			EndedAt = now,
			Duration = duration,
			TeamTotalDamage = teamTotalDamage,
			TeamTotalHealing = teamTotalHealing,
			TeamTotalTakenDamage = teamTotalTaken,
			Players = players,
			Statistics = statistics
		};
	}

	/// <summary>
    /// 保存快照到磁盘
	/// </summary>
	private void SaveSnapshotToDisk(BattleSnapshotData snapshot)
	{
		var fileName = $"{snapshot.ScopeType}_{snapshot.StartedAt:yyyy-MM-dd_HH-mm-ss}.json";
		var filePath = Path.Combine(_snapshotDirectory, fileName);

		var json = JsonConvert.SerializeObject(snapshot);

		File.WriteAllText(filePath, json);
		snapshot.FilePath = filePath;
	}

	/// <summary>
    /// 从磁盘加载快照
	/// </summary>
	private void LoadSnapshots()
	{
		try
		{
			if (!Directory.Exists(_snapshotDirectory))
			{
				return;
			}

			var files = Directory.GetFiles(_snapshotDirectory, "*.json")
				.OrderByDescending(f => File.GetCreationTime(f))
				.ToList();

			foreach (var file in files)
			{
				try
				{
					var snapshot = LoadSnapshot(file);
					if (snapshot == null) continue;

					snapshot.FilePath = file;

					if (snapshot.ScopeType == ScopeTime.Current)
					{
						if (CurrentSnapshots.Count < MaxSnapshots)
						{
							CurrentSnapshots.Add(SnapshotInfo.FromSnapshotData(snapshot));
						}
						else
						{
                            // ? 超出限制,删除文件并释放内存
							File.Delete(file);
                            _logger.LogDebug("启动时删除超出限制的旧快照文件: {File}", file);
						}
					}
					else
					{
						if (TotalSnapshots.Count < MaxSnapshots)
						{
							TotalSnapshots.Add(SnapshotInfo.FromSnapshotData(snapshot));
						}
						else
						{
                            // ? 超出限制,删除文件并释放内存
							File.Delete(file);
                            _logger.LogDebug("启动时删除超出限制的旧快照文件: {File}", file);
						}
					}
				}
				catch (Exception ex)
				{
                    _logger.LogWarning(ex, "加载快照文件失败: {File}", file);
                    // 损坏的文件直接删除
					try
					{
						File.Delete(file);
					}
					catch
					{
						// ignore
					}
				}
			}

            _logger.LogInformation("加载快照完成: 当前={Current}/{MaxCurrent}, 全程={Total}/{MaxTotal}",
				CurrentSnapshots.Count, MaxSnapshots, TotalSnapshots.Count, MaxSnapshots);
		}
		catch (Exception ex)
		{
            _logger.LogError(ex, "加载快照失败");
		}
	}

	/// <summary>
    /// 尝试删除快照文件
	/// </summary>
	private void TryDeleteSnapshotFile(string filePath)
	{
		try
		{
			if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
			{
				File.Delete(filePath);
                _logger.LogDebug("成功删除快照文件: {File}", filePath);
			}
		}
		catch (Exception ex)
		{
            _logger.LogWarning(ex, "删除快照文件失败: {File}", filePath);
		}
	}
}

/// <summary>
/// 快照数据模型
/// </summary>
public class BattleSnapshotData
{
	public ScopeTime ScopeType { get; set; }
	public DateTime StartedAt { get; set; }
	public DateTime EndedAt { get; set; }
	public TimeSpan Duration { get; set; }
	public ulong TeamTotalDamage { get; set; }
	public ulong TeamTotalHealing { get; set; }
	public ulong TeamTotalTakenDamage { get; set; }
	public Dictionary<long, PlayerInfo> Players { get; set; } = new();
	public Dictionary<long, PlayerStatistics> Statistics { get; set; } = new();

	/// <summary>
    /// 文件路径(不序列化)
	/// </summary>
	[System.Text.Json.Serialization.JsonIgnore]
	public string FilePath { get; set; } = "";

	/// <summary>
    /// 显示标签
	/// </summary>
	[System.Text.Json.Serialization.JsonIgnore]
	public string DisplayLabel =>
        $"{(ScopeType == ScopeTime.Current ? "현재" : "전체")} {StartedAt:HH:mm:ss} ({Duration:mm\\:ss})";

	public static explicit operator SnapshotInfo(BattleSnapshotData d) => SnapshotInfo.FromSnapshotData(d);
}

public class PrivateSetterContractResolver : DefaultContractResolver
{
	protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
	{
		var property = base.CreateProperty(member, memberSerialization);

		if (!property.Writable)
		{
			var propertyInfo = member as PropertyInfo;
			if (propertyInfo?.GetSetMethod(true) != null)
			{
				property.Writable = true;
			}
		}

		return property;
	}
}
