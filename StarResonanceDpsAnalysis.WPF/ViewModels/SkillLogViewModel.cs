using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarResonanceDpsAnalysis.Core.Data;
using StarResonanceDpsAnalysis.WPF.Config;
using StarResonanceDpsAnalysis.WPF.Models;
using StarResonanceDpsAnalysis.WPF.Services;
using StarResonanceDpsAnalysis.WPF.Views;

namespace StarResonanceDpsAnalysis.WPF.ViewModels;

public partial class SkillLogViewModel : ObservableObject
{
    private readonly ISkillLogService _skillLogService;
    private readonly IConfigManager? _configManager;
    private readonly IWindowManagementService? _windowManagementService;

    public ObservableCollection<SkillLogItem> Logs => _skillLogService.Logs;

    // Design-time constructor
    public SkillLogViewModel()
    {
        var dataStorage = new DataStorageV2(Microsoft.Extensions.Logging.Abstractions.NullLogger<DataStorageV2>.Instance);
        var configManager = new DesignConfigManager();
        _skillLogService = new SkillLogService(dataStorage, configManager);
        
        // Add dummy data for design time
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            _skillLogService.AddLog(new SkillLogItem { Timestamp = System.DateTime.Now, SkillName = "Test Skill", TotalValue = 1234, Count = 1, CritCount = 1 });
            _skillLogService.AddLog(new SkillLogItem { Timestamp = System.DateTime.Now.AddSeconds(1), SkillName = "Another Skill", TotalValue = 2345, Count = 1, LuckyCount = 1 });
        }
    }

    public SkillLogViewModel(ISkillLogService skillLogService, IConfigManager configManager, IWindowManagementService windowManagementService)
    {
        _skillLogService = skillLogService;
        _configManager = configManager;
        _windowManagementService = windowManagementService;
        
        // 检查 UID 是否已设置
        CheckUidConfiguration();
    }

    private void CheckUidConfiguration()
    {
        if (_configManager == null)
            return;
            
        var currentUid = _configManager.CurrentConfig.Uid;
        if (currentUid == 0)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                var result = MessageBox.Show(
                    "检测到您还未设置角色 UID，技能日记需要 UID 才能正常工作。\n\n" +
                    "如何获取 UID：进入游戏后，左下角玩家编号就是 UID\n\n" +
                    "是否现在前往设置？",
                    "需要设置 UID",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes && _windowManagementService != null)
                {
                    _windowManagementService.SettingsView.Show();
                    _windowManagementService.SettingsView.Activate();
                }
            });
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _skillLogService.Clear();
    }

    [RelayCommand]
    private void Close()
    {
        // 查找并关闭 SkillLogView 窗口
        var window = Application.Current?.Windows.OfType<SkillLogView>().FirstOrDefault();
        window?.Close();
    }
}
