using System;
using System.Collections.Generic;
using System.Windows;
using OxyPlot;
using StarResonanceDpsAnalysis.WPF.Models;
using StarResonanceDpsAnalysis.WPF.ViewModels;

namespace StarResonanceDpsAnalysis.WPF.Views
{
    /// <summary>
    /// Test.xaml 的交互逻辑
    /// </summary>
    public partial class ControlTestView : Window
    {
        public PlotViewModel SkillDistributionPlot { get; }
        public long NormalHitCount { get; private set; }
        public double NormalHitRate { get; private set; }
        public long CritHitCount { get; private set; }
        public double CritHitRate { get; private set; }
        public long LuckyHitCount { get; private set; }
        public double LuckyHitRate { get; private set; }

        /*
        public ObservableCollection<SkillItemViewModel> TestData { get; }
        */

        public ControlTestView()
        {
            InitializeComponent();

            SkillDistributionPlot = CreateSkillDistributionPlot();
            InitializeHitTypeStats();

            DataContext = this;

            /*
            DataContext = this;
            TestData = new ObservableCollection<SkillItemViewModel>
            {
                new SkillItemViewModel { SkillName = "Arcane Burst", TotalValue = 312_450, HitCount = 58, CritRate = 0.32, CritCount = 19, Average = 5_387 },
                new SkillItemViewModel { SkillName = "Shadow Slash", TotalValue = 287_910, HitCount = 64, CritRate = 0.28, CritCount = 18, Average = 4_499 },
                new SkillItemViewModel { SkillName = "Frost Lance", TotalValue = 254_320, HitCount = 44, CritRate = 0.22, CritCount = 10, Average = 5_780 },
                new SkillItemViewModel { SkillName = "Thunder Strike", TotalValue = 233_770, HitCount = 39, CritRate = 0.25, CritCount = 10, Average = 5_994 },
                new SkillItemViewModel { SkillName = "Blazing Edge", TotalValue = 221_480, HitCount = 53, CritRate = 0.31, CritCount = 16, Average = 4_179 },
                new SkillItemViewModel { SkillName = "Stone Breaker", TotalValue = 199_630, HitCount = 36, CritRate = 0.18, CritCount = 7, Average = 5_545 },
                new SkillItemViewModel { SkillName = "Wind Shear", TotalValue = 181_240, HitCount = 71, CritRate = 0.19, CritCount = 14, Average = 2_553 },
                new SkillItemViewModel { SkillName = "Radiant Arrow", TotalValue = 174_900, HitCount = 48, CritRate = 0.34, CritCount = 16, Average = 3_644 },
                new SkillItemViewModel { SkillName = "Iron Impact", TotalValue = 162_550, HitCount = 27, CritRate = 0.21, CritCount = 6, Average = 6_020 },
                new SkillItemViewModel { SkillName = "Lunar Slice", TotalValue = 151_680, HitCount = 42, CritRate = 0.29, CritCount = 12, Average = 3_611 },
                new SkillItemViewModel { SkillName = "Crimson Flare", TotalValue = 143_210, HitCount = 33, CritRate = 0.37, CritCount = 12, Average = 4_340 },
                new SkillItemViewModel { SkillName = "Echo Shot", TotalValue = 136_590, HitCount = 60, CritRate = 0.23, CritCount = 14, Average = 2_276 },
                new SkillItemViewModel { SkillName = "Vortex Coil", TotalValue = 128_470, HitCount = 38, CritRate = 0.26, CritCount = 10, Average = 3_381 },
                new SkillItemViewModel { SkillName = "Gale Rush", TotalValue = 119_320, HitCount = 55, CritRate = 0.17, CritCount = 9, Average = 2_169 },
                new SkillItemViewModel { SkillName = "Solar Pike", TotalValue = 112_640, HitCount = 29, CritRate = 0.24, CritCount = 7, Average = 3_884 },
                new SkillItemViewModel { SkillName = "Venom Spike", TotalValue = 104_980, HitCount = 46, CritRate = 0.21, CritCount = 10, Average = 2_282 },
                new SkillItemViewModel { SkillName = "Aegis Slam", TotalValue = 97_540, HitCount = 22, CritRate = 0.15, CritCount = 3, Average = 4_434 },
                new SkillItemViewModel { SkillName = "Quick Shot", TotalValue = 89_110, HitCount = 73, CritRate = 0.16, CritCount = 12, Average = 1_221 },
                new SkillItemViewModel { SkillName = "Night Bloom", TotalValue = 82_760, HitCount = 31, CritRate = 0.33, CritCount = 10, Average = 2_669 },
                new SkillItemViewModel { SkillName = "Orbital Cut", TotalValue = 76_450, HitCount = 26, CritRate = 0.20, CritCount = 5, Average = 2_940 },
                new SkillItemViewModel { SkillName = "Tempest Jab", TotalValue = 71_120, HitCount = 40, CritRate = 0.14, CritCount = 6, Average = 1_778 },
                new SkillItemViewModel { SkillName = "Phoenix Step", TotalValue = 65_880, HitCount = 18, CritRate = 0.27, CritCount = 5, Average = 3_660 },
                new SkillItemViewModel { SkillName = "Dusk Thrust", TotalValue = 59_470, HitCount = 24, CritRate = 0.19, CritCount = 4, Average = 2_478 },
                new SkillItemViewModel { SkillName = "Sage Pulse", TotalValue = 52_930, HitCount = 34, CritRate = 0.12, CritCount = 4, Average = 1_556 },
                new SkillItemViewModel { SkillName = "Berserk Spin", TotalValue = 46_700, HitCount = 15, CritRate = 0.30, CritCount = 4, Average = 3_113 }
            };

            var maxTotal = TestData.Max(item => item.TotalValue);
            var sumTotal = TestData.Sum(item => item.TotalValue);

            foreach (var item in TestData)
            {
                item.RateToMax = maxTotal > 0 ? item.TotalValue / (double)maxTotal : 0;
                item.RateToTotal = sumTotal > 0 ? item.TotalValue / (double)sumTotal : 0;
            }
            */
        }

        private static PlotViewModel CreateSkillDistributionPlot()
        {
            var plot = new PlotViewModel(new PlotOptions
            {
                XAxisTitle = "Time (s)",
                YAxisTitle = "DPS",
                LineSeriesTitle = "DPS",
                StatisticType = StatisticType.Damage
            });

            var skills = new List<SkillItemViewModel>
            {
                new() { SkillName = "Arcane Burst", TotalValue = 310_000 },
                new() { SkillName = "Shadow Slash", TotalValue = 220_000 },
                new() { SkillName = "Frost Lance", TotalValue = 190_000 },
                new() { SkillName = "Thunder Strike", TotalValue = 140_000 },
                new() { SkillName = "Blazing Edge", TotalValue = 70_000 },
                new() { SkillName = "Stone Breaker", TotalValue = 60_000 },
                new() { SkillName = "Wind Shear", TotalValue = 50_000 },
                new() { SkillName = "Radiant Arrow", TotalValue = 48_000 },
                new() { SkillName = "Iron Impact", TotalValue = 40_000 },
                new() { SkillName = "Lunar Slice", TotalValue = 35_000 },
                new() { SkillName = "Crimson Flare", TotalValue = 34_000 },
                new() { SkillName = "Echo Shot", TotalValue = 33_000 },
                new() { SkillName = "Vortex Coil", TotalValue = 32_000 },
                new() { SkillName = "Gale Rush", TotalValue = 31_000 },
                new() { SkillName = "Gale Rush (Sub)", TotalValue = 30_000 },
                new() { SkillName = "Solar Pike", TotalValue = 27_000 },
                new() { SkillName = "Solar Pike (Sub)", TotalValue = 26_000 },
                new() { SkillName = "Venom Spike", TotalValue = 23_000 },
                new() { SkillName = "Aegis Slam", TotalValue = 22_000 },
                new() { SkillName = "Quick Shot", TotalValue = 21_000 },
                new() { SkillName = "Night Bloom", TotalValue = 20_000 },
                new() { SkillName = "Night Bloom (Sub)", TotalValue = 19_000 },
                new() { SkillName = "Orbital Cut", TotalValue = 18_000 },
                new() { SkillName = "Orbital Cut (Sub)", TotalValue = 17_000 },
                new() { SkillName = "Tempest Jab", TotalValue = 16_000 },
                new() { SkillName = "Tempest Jab (Sub)", TotalValue = 15_000 },
                new() { SkillName = "Phoenix Step", TotalValue = 14_000 },
                new() { SkillName = "Dusk Thrust", TotalValue = 9_000 },
                new() { SkillName = "Sage Pulse", TotalValue = 7_000 },
                new() { SkillName = "Berserk Spin", TotalValue = 4_000 }
            };

            plot.SetPieSeriesData(skills);

            for (var i = 0; i <= 180; i += 3)
            {
                var value = 18_000
                            + 2_800 * Math.Sin(i / 14.0)
                            + 1_600 * Math.Cos(i / 5.0)
                            + (i % 9) * 140;
                plot.LineSeriesData.Points.Add(new DataPoint(i, Math.Max(0, value)));
            }

            plot.RefreshSeries();

            return plot;
        }

        private void InitializeHitTypeStats()
        {
            var normalHits = 1_820L;
            var critHits = 560L;
            var luckyHits = 120L;
            var totalHits = normalHits + critHits + luckyHits;

            NormalHitCount = normalHits;
            CritHitCount = critHits;
            LuckyHitCount = luckyHits;

            NormalHitRate = totalHits > 0 ? (double)normalHits / totalHits : 0;
            CritHitRate = totalHits > 0 ? (double)critHits / totalHits : 0;
            LuckyHitRate = totalHits > 0 ? (double)luckyHits / totalHits : 0;
        }
    }
}
