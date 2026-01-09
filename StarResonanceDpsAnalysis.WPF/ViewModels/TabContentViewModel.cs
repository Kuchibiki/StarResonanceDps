using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OxyPlot;
using StarResonanceDpsAnalysis.WPF.Extensions;
using StarResonanceDpsAnalysis.WPF.Localization;
using StarResonanceDpsAnalysis.WPF.Properties;
using System.Collections.ObjectModel;
using StarResonanceDpsAnalysis.Core.Statistics;
using StarResonanceDpsAnalysis.WPF.Models;
using System.Windows.Threading;
using StarResonanceDpsAnalysis.Core.Data;

namespace StarResonanceDpsAnalysis.WPF.ViewModels;

/// <summary>
/// ViewModel for TabContentPanel component
/// </summary>
public partial class TabContentViewModel : BaseViewModel
{
    [ObservableProperty] private PlotViewModel _plot;
    [ObservableProperty] private DataStatisticsViewModel _stats = new();
    [ObservableProperty] private SkillListViewModel _skillList = new();
    
    // ? 新增：排序选项
    [ObservableProperty] private SkillSortOption _selectedSortOption = SkillSortOption.Total;

    public TabContentViewModel(PlotViewModel plot)
    {
        _plot = plot;
    }

    /// <summary>
    /// ? 排序选项Changed时重新排序技能列表
    /// </summary>
    partial void OnSelectedSortOptionChanged(SkillSortOption value)
    {
        SortSkillList();
    }

    /// <summary>
    /// ? 根据当前排序选项对技能列表排序
    /// </summary>
    [RelayCommand]
    public void SortSkillList()
    {
        var skillItems = _skillList.SkillItems;
        if (skillItems == null || skillItems.Count == 0)
            return;

        var sorted = SelectedSortOption switch
        {
            SkillSortOption.Total => skillItems.OrderByDescending(s => GetTotalValue(s)).ToList(),
            SkillSortOption.AverageDps => skillItems.OrderByDescending(s => GetAverageDps(s)).ToList(),
            _ => skillItems.ToList()
        };

        // 清空并重新填充
        _skillList.SkillItems.Clear();
        foreach (var item in sorted)
        {
            _skillList.SkillItems.Add(item);
        }
    }

    /// <summary>
    /// 获取技能的Total值（根据Plot的StatisticType）
    /// </summary>
    private long GetTotalValue(SkillItemViewModel skill)
    {
        return _plot.StatisticType switch
        {
            StatisticType.Damage => skill.Damage.TotalValue,
            StatisticType.Healing => skill.Heal.TotalValue,
            StatisticType.TakenDamage => skill.TakenDamage.TotalValue,
            StatisticType.NpcTakenDamage => skill.TakenDamage.TotalValue,
            _ => 0
        };
    }

    /// <summary>
    /// 获取技能的Average DPS值
    /// </summary>
    private double GetAverageDps(SkillItemViewModel skill)
    {
        return _plot.StatisticType switch
        {
            StatisticType.Damage => skill.Damage.ValuePerSecond,
            StatisticType.Healing => skill.Heal.ValuePerSecond,
            StatisticType.TakenDamage => skill.TakenDamage.ValuePerSecond,
            StatisticType.NpcTakenDamage => skill.TakenDamage.ValuePerSecond,
            _ => 0
        };
    }
}

/// <summary>
/// ? 技能排序选项枚举
/// </summary>
public enum SkillSortOption
{
    /// <summary>
    /// 按总值排序（默认）
    /// </summary>
    Total,
    
    /// <summary>
    /// 按平均DPS排序
    /// </summary>
    AverageDps
}
