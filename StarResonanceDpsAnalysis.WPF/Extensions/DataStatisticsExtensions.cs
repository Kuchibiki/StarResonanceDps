using StarResonanceDpsAnalysis.WPF.ViewModels;

namespace StarResonanceDpsAnalysis.WPF.Extensions;

public static class DataStatisticsExtensions
{
    public static DataStatistics FromSkillsToDamageTaken(
        this IReadOnlyList<SkillItemViewModel> skills,
        TimeSpan durationMs)
    {
        return CreateStatistics(skills, s => s.TotalTakenDamage, durationMs);
    }

    public static void UpdateDamageTaken(
        this IReadOnlyList<SkillItemViewModel> skills,
        TimeSpan durationMs,
        DataStatistics stats)
    {
        UpdateStatistics(skills, s => s.TotalTakenDamage, durationMs, stats);
    }

    public static DataStatistics FromSkillsToHealing(
        this IReadOnlyList<SkillItemViewModel> skills,
        TimeSpan durationMs)
    {
        return CreateStatistics(skills, s => s.TotalHeal, durationMs);
    }

    public static void UpdateHealing(
        this IReadOnlyList<SkillItemViewModel> skills,
        TimeSpan durationMs,
        DataStatistics stats)
    {
        UpdateStatistics(skills, s => s.TotalHeal, durationMs, stats);
    }

    public static DataStatistics FromSkillsToDamage(
        this IReadOnlyList<SkillItemViewModel> skills,
        TimeSpan durationMs)
    {
        return CreateStatistics(skills, s => s.TotalDamage, durationMs);
    }

    public static void UpdateDamage(
        this IReadOnlyList<SkillItemViewModel> skills,
        TimeSpan durationMs,
        DataStatistics stats)
    {
        UpdateStatistics(skills, s => s.TotalDamage, durationMs, stats);
    }

    // Creates a new DataStatistics instance using the shared aggregation logic.
    private static DataStatistics CreateStatistics(
        IReadOnlyList<SkillItemViewModel> skills,
        Func<SkillItemViewModel, long> totalSelector,
        TimeSpan duration)
    {
        var stats = new DataStatistics();
        UpdateStatistics(skills, totalSelector, duration, stats);
        return stats;
    }

    // Shared aggregation logic for damage, healing and damage taken.
    private static void UpdateStatistics(
        IReadOnlyList<SkillItemViewModel> skills,
        Func<SkillItemViewModel, long> totalSelector,
        TimeSpan duration,
        DataStatistics stats)
    {
        stats.Total = skills.Sum(totalSelector);
        stats.Hits = skills.Sum(s => s.HitCount);
        stats.LuckyCount = skills.Sum(s => s.LuckyCount);

        var totalCritHits = skills.Sum(s => s.CritCount);
        stats.CritCount = totalCritHits;
        stats.CritRate = stats.Hits > 0
            ? (double)totalCritHits / stats.Hits
            : 0;

        if (duration.Ticks == 0)
        {
            stats.Average = Double.NaN;
            return;
        }
        stats.Average = (double)(stats.Total * TimeSpan.TicksPerSecond) / duration.Ticks;
    }
}
