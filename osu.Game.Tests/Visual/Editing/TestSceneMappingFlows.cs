// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Goes through a mapper's work in the order it is done, and looks at what each difficulty's file says at the end.
    /// </summary>
    public partial class TestSceneMappingFlows : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override bool IsolateSavingFromDatabase => false;

        protected override WorkingBeatmap CreateWorkingBeatmap(IBeatmap beatmap, Storyboard? storyboard = null) => new DummyWorkingBeatmap(Audio, null);

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("make new beatmap unique", () => EditorBeatmap.Metadata.Title = Guid.NewGuid().ToString());
        }

        [Test]
        public void TestANewMapWithASecondDifficultyMadeFromNothing()
        {
            AddStep("name the first difficulty", () => EditorBeatmap.BeatmapInfo.DifficultyName = "Normal");
            AddStep("put the red lines", () =>
            {
                EditorBeatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
                EditorBeatmap.ControlPointInfo.Add(8000, new TimingControlPoint { BeatLength = 400 });
            });
            AddStep("place with velocities and volumes", () => EditorBeatmap.AddRange(new HitObject[]
            {
                circle(1000, HitSampleInfo.BANK_SOFT, 60),
                slider(2000, 0.85, HitSampleInfo.BANK_SOFT, 60),
                slider(4000, 0.85, HitSampleInfo.BANK_NORMAL, 80),
                slider(9000, 1.2, HitSampleInfo.BANK_DRUM, 70),
                circle(11000, HitSampleInfo.BANK_DRUM, 70),
            }));
            AddStep("save", () => Editor.Save());

            AddStep("make a difficulty from nothing", () => Editor.CreateNewDifficulty(new OsuRuleset().RulesetInfo));
            AddUntilStep("wait for dialog", () => DialogOverlay.CurrentDialog is CreateNewDifficultyDialog);
            AddStep("with no objects", () => DialogOverlay.CurrentDialog!.PerformOkAction());
            AddUntilStep("wait for it", () => Editor.ChildrenOfType<EditorBeatmap>().SingleOrDefault()?.BeatmapInfo.DifficultyName is string name && name != "Normal");
            AddUntilStep("wait for editor", () => Editor.ReadyForUse && DialogOverlay.IsLoaded);

            AddStep("name it", () => EditorBeatmap.BeatmapInfo.DifficultyName = "Hard");
            AddStep("place its own", () => EditorBeatmap.AddRange(new HitObject[]
            {
                slider(1500, 1.4, HitSampleInfo.BANK_NORMAL, 90),
                circle(3000, HitSampleInfo.BANK_NORMAL, 90),
                slider(8500, 1.6, HitSampleInfo.BANK_NORMAL, 90),
            }));
            AddStep("save", () => Editor.Save());

            AddAssert("the first says its own velocities and volumes", () => soundsOf("Normal"), () => Is.EqualTo(new[]
            {
                (1000.0, 60, 1.0),
                (2000.0, 60, 0.85),
                (4000.0, 80, 0.85),
                (9000.0, 70, 1.2),
                (11000.0, 70, 1.2),
            }));
            AddAssert("the second says its own, and none of the first's", () => soundsOf("Hard"), () => Is.EqualTo(new[]
            {
                (1500.0, 90, 1.4),
                (3000.0, 90, 1.4),
                (8500.0, 90, 1.6),
            }));
        }

        private static HitCircle circle(double time, string bank, int volume) => new HitCircle
        {
            StartTime = time,
            Position = new Vector2(100),
            Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bank, volume: volume) },
        };

        private static Slider slider(double time, double velocity, string bank, int volume) => new Slider
        {
            StartTime = time,
            Position = new Vector2(100),
            SliderVelocityMultiplier = velocity,
            Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(150, 0) }),
            Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bank, volume: volume) },
        };

        /// <summary>
        /// For each object in a difficulty's file as saved: its time, and the volume and velocity its file says there.
        /// </summary>
        private (double time, int volume, double velocity)[] soundsOf(string difficulty)
        {
            var b = Beatmap.Value.BeatmapSetInfo.Beatmaps.Single(d => d.DifficultyName == difficulty);
            var files = new RealmFileStore(Realm, Dependencies.Get<GameHost>().Storage);

            using var store = new RealmBackedResourceStore<BeatmapSetInfo>(b.BeatmapSet!.ToLive(Realm), files.Store, Realm);
            string file = Encoding.UTF8.GetString(store.Get(b.File!.Filename));
            var lines = SavedTimingLines.Read(Encoding.UTF8.GetBytes(file));

            return file.Replace("\r\n", "\n").Split('\n')
                       .SkipWhile(l => l.Trim() != "[HitObjects]").Skip(1)
                       .TakeWhile(l => !l.Trim().StartsWith('['))
                       .Where(l => l.Trim().Length > 0)
                       .Select(l => double.Parse(l.Split(',')[2], System.Globalization.CultureInfo.InvariantCulture))
                       .Select(t => (t, lines.Last(l => l.Time <= t + 5).Volume, lines.Last(l => l.Time <= t).SliderVelocity))
                       .ToArray();
        }
    }
}
