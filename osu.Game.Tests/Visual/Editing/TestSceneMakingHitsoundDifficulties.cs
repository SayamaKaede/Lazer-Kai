// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Tests.Resources;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The new difficulties the hitsound screen makes from a difficulty kept in stable's format: a copy, and one of
    /// circles where sounds are played. Both carry the green lines over as they were, which is what lets hitsounds made
    /// on one difficulty be copied to the others (2026-10-08).
    /// </summary>
    public partial class TestSceneMakingHitsoundDifficulties : StableFormatConversionTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        private BeatmapInfo imported = null!;
        private byte[] original = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();

            AddStep("read the file as it is kept", () => original = SavedFile());
        }

        [Test]
        public void TestAHitsoundDifficultyIsCirclesWhereSoundsArePlayed()
        {
            int points = 0;

            AddStep("count the points sounds are played at", () =>
            {
                var working = Game.BeatmapManager.GetWorkingBeatmap(imported);
                points = working.GetPlayableBeatmap(new OsuRuleset().RulesetInfo).HitObjects.Sum(h => h is Slider slider ? slider.RepeatCount + 2 : 1);
            });

            AddAssert("offered", () => Editor.CanMakeHitsoundDifficulty.Value);
            make(true);

            AddAssert("named as a hitsound difficulty is", () => EditorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo("Hitsounds"));
            AddAssert("a circle for every point", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(points));
            AddAssert("all of them in the middle, starting a combo",
                () => EditorBeatmap.HitObjects.All(h => h is HitCircle circle && circle.Position == new Vector2(256, 192) && circle.NewCombo));
            AddAssert("the red and green lines are as they were", () => timingPointsOf(SavedFile()), () => Is.EqualTo(timingPointsOf(original)));
            AddAssert("the file is stable's", () => FirstLine(SavedFile()), () => Is.EqualTo(FirstLine(original)));
            AddAssert("still on the hitsound screen", () => Editor.Mode.Value, () => Is.EqualTo(EditorScreenMode.Hitsounds));
        }

        [Test]
        public void TestACopyKeepsTheObjectsAndTheGreenLines()
        {
            make(false);

            AddAssert("named as a copy", () => EditorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo("Hard (copy)"));
            AddAssert("the objects are as they were", () => section(SavedFile(), "[HitObjects]"), () => Is.EqualTo(section(original, "[HitObjects]")));
            AddAssert("the red and green lines are as they were", () => timingPointsOf(SavedFile()), () => Is.EqualTo(timingPointsOf(original)));
        }

        /// <summary>
        /// The copy the file menu makes, when asked for a new difficulty of the same ruleset.
        /// </summary>
        [Test]
        public void TestTheFileMenusCopyKeepsTheGreenLines()
        {
            Editor? before = null;

            AddStep("ask for a new difficulty", () => (before = Editor).CreateNewDifficulty(new OsuRuleset().RulesetInfo));
            AddUntilStep("the dialog is up", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog is CreateNewDifficultyDialog);
            AddStep("make a copy", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog!.ChildrenOfType<PopupDialogButton>()
                                             .Single(b => b.Text == EditorDialogsStrings.CreateCopy).TriggerClick());
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.IsLoaded == true);

            AddAssert("the file is stable's", () => FirstLine(SavedFile()), () => Is.EqualTo(FirstLine(original)));
            AddAssert("the red and green lines are as they were", () => timingPointsOf(SavedFile()), () => Is.EqualTo(timingPointsOf(original)));
        }

        /// <summary>
        /// Over the buttons, the line the sounds at the playhead follow: its volume is shown, and changing it puts a green
        /// line in where the playhead is, as stable's timing panel does, which the object there then sounds at.
        /// </summary>
        [Test]
        public void TestTheVolumeIsChangedOnALineAtThePlayhead()
        {
            HitObject target = null!;
            int volumeBefore = 0;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddStep("seek to an object", () =>
            {
                target = EditorBeatmap.HitObjects.OfType<HitCircle>().ElementAt(10);
                ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(target.StartTime);
            });

            AddUntilStep("the volume of the line it follows is shown", () => panel.LineVolume.Value, () => Is.EqualTo(lineAt(target.StartTime + 5).Volume));
            AddStep("remember the object before it", () => volumeBefore = normalVolumeOf(objectBefore(target)));

            AddStep("change the volume to 33", () => panel.ChildrenOfType<FormSliderBar<int>>().Last().Current.Value = 33);

            AddAssert("a green line where the object is says 33", () => lines().Any(l => !l.Uninherited && Math.Abs(l.Time - target.StartTime) <= 5 && l.Volume == 33));
            AddAssert("the object sounds at 33", () => normalVolumeOf(target), () => Is.EqualTo(33));
            AddAssert("the object before it is as it was", () => normalVolumeOf(objectBefore(target)), () => Is.EqualTo(volumeBefore));
        }

        private HitObject objectBefore(HitObject hitObject) => EditorBeatmap.HitObjects.TakeWhile(h => h != hitObject).Last();

        /// <summary>
        /// The custom index is typed, and the values the lines use are there as buttons to use again (2026-10-08).
        /// </summary>
        [Test]
        public void TestTheCustomIndexIsTypedAndUsedVolumesArePresets()
        {
            HitObject target = null!;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddStep("seek to an object", () =>
            {
                target = EditorBeatmap.HitObjects.OfType<HitCircle>().ElementAt(10);
                ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(target.StartTime);
            });
            AddUntilStep("the line's volume is shown", () => panel.LineVolume.Value, () => Is.EqualTo(lineAt(target.StartTime + 5).Volume));

            AddStep("type 7 as the custom index", () =>
            {
                var box = panel.ChildrenOfType<FormNumberBox>().Single();

                InputManager.MoveMouseTo(box);
                InputManager.Click(MouseButton.Left);
                box.Current.Value = "7";
                InputManager.Key(Key.Enter);
            });

            AddAssert("a green line where the object is says 7", () => lines().Any(l => !l.Uninherited && Math.Abs(l.Time - target.StartTime) <= 5 && l.SampleIndex == 7));

            AddAssert("every volume the lines use is a button", () => panel.ChildrenOfType<HitsoundPanel.PresetButton>().Select(b => b.Value),
                () => Is.SupersetOf(lines().Select(l => l.Volume).Distinct()));

            int otherVolume = 0;

            AddStep("press a volume other than the line's", () =>
            {
                // the volumes come after the custom indices.
                var button = panel.ChildrenOfType<HitsoundPanel.PresetButton>().Last(b => b.Value != panel.LineVolume.Value && b.Value > 5);
                otherVolume = button.Value;
                button.TriggerClick();
            });

            AddAssert("the line where the object is has it", () => lines().Any(l => !l.Uninherited && Math.Abs(l.Time - target.StartTime) <= 5 && l.Volume == otherVolume));
        }

        private HitsoundPanel panel => Editor.ChildrenOfType<HitsoundPanel>().Single();

        private IReadOnlyList<SavedTimingLine> lines() => ((SavedTimingLines)Editor.Dependencies.Get(typeof(SavedTimingLines))).Lines.ToList();

        private SavedTimingLine lineAt(double time) => lines().Last(l => l.Time <= time);

        private static int normalVolumeOf(HitObject hitObject) => hitObject.Samples.Single(s => s.Name == HitSampleInfo.HIT_NORMAL).Volume;

        [Test]
        public void TestNotOfferedWithStableCompatibilityOff()
        {
            AddStep("turn stable compatibility off", () => Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, false));
            AddAssert("not offered", () => Editor.CanMakeHitsoundDifficulty.Value, () => Is.False);
        }

        private void make(bool asHitCircles)
        {
            Editor? before = null;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddStep(asHitCircles ? "make a hitsound difficulty" : "copy into a new difficulty", () => (before = Editor).MakeHitsoundDifficulty(asHitCircles));
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.IsLoaded == true);
        }

        private static string[] timingPointsOf(byte[] file) => section(file, "[TimingPoints]");

        private static string[] section(byte[] file, string name) =>
            Encoding.UTF8.GetString(file).Replace("\r\n", "\n").Split('\n')
                    .SkipWhile(l => l.Trim() != name).Skip(1)
                    .TakeWhile(l => !l.Trim().StartsWith('['))
                    .Where(l => l.Trim().Length > 0)
                    .ToArray();
    }
}
