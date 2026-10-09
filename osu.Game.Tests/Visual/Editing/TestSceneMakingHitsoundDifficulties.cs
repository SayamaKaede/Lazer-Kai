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
using osu.Game.Screens.Edit.GreenLines;
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

        /// <summary>
        /// The hitsounds of the hitsound difficulty are copied onto this one from its file, its green lines first: where
        /// that one plays nothing, a sound here is given what its line says and loses its additions, rather than keeping
        /// what it had (2026-10-08).
        /// </summary>
        [Test]
        public void TestCopyingTheHitsoundsOfTheHitsoundDifficulty()
        {
            double matched = 0;
            double unmatched = 0;
            Dictionary<double, double> velocities = null!;
            string[] samplesBefore = null!;

            AddStep("note two circles", () =>
            {
                var circles = EditorBeatmap.HitObjects.OfType<HitCircle>().ToArray();
                matched = circles[10].StartTime;
                unmatched = circles[11].StartTime;
            });

            make(true);

            AddStep("give the first a clap from drum", () =>
            {
                var circle = objectAt(matched);
                HitsoundEditing.SetHitsound(new[] { new HitPoint(circle, null, circle.StartTime) },
                    new PointHitsound(HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_DRUM, new[] { HitSampleInfo.HIT_CLAP }));
                EditorBeatmap.Update(circle);
            });
            AddStep("take the second out", () => EditorBeatmap.Remove(objectAt(unmatched)));
            AddStep("seek to the first", () => ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(matched));
            waitForPanel();
            AddUntilStep("the line's volume is shown", () => panel.LineVolume.Value, () => Is.EqualTo(lineAt(matched + 5).Volume));
            AddStep("change the volume to 33", () => panel.ChildrenOfType<FormSliderBar<int>>().Last().Current.Value = 33);
            AddStep("save", () => Editor.Save());

            AddStep("switch back", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!));
            AddUntilStep("the editor opens it", () => Editor?.IsLoaded == true && EditorBeatmap.BeatmapInfo.ID == imported.ID);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            waitForPanel();

            AddStep("give the second circle here a whistle, and a sampleset and volume its line does not say", () =>
            {
                var circle = objectAt(unmatched);
                var point = new[] { new HitPoint(circle, null, circle.StartTime) };

                HitsoundEditing.SetAddition(point, HitSampleInfo.HIT_WHISTLE, true, HitSampleInfo.BANK_NORMAL);
                HitsoundEditing.SetBank(point, HitSampleInfo.ALL_BANKS.First(b => b != bankOf(lineAt(unmatched + 5))));
                circle.Samples = circle.Samples.Select(s => s.With(newVolume: lineAt(unmatched + 5).Volume == 77 ? 78 : 77)).ToList();
                EditorBeatmap.Update(circle);
            });
            AddStep("note the velocities and the first circle's sounds", () =>
            {
                velocities = EditorBeatmap.HitObjects.OfType<Slider>().ToDictionary(s => s.StartTime, s => s.SliderVelocityMultiplier);
                samplesBefore = describe(objectAt(matched));
            });

            AddAssert("the hitsound difficulty is the one to copy from", () => panel.ChildrenOfType<FormDropdown<string>>().Single().Current.Value, () => Is.EqualTo("Hitsounds"));
            AddStep("copy", () => panel.ChildrenOfType<RoundedButton>().Single(b => b.Text == LazerKaiHitsoundStrings.CopyFromButton).TriggerClick());

            AddAssert("the first circle claps from drum over soft", () => string.Join(' ', describe(objectAt(matched))), () => Does.Contain("hitclap:drum").And.Contain("hitnormal:soft"));
            AddAssert("its line says 33", () => lineAt(matched + 5).Volume, () => Is.EqualTo(33));
            AddAssert("it sounds at 33", () => normalVolumeOf(objectAt(matched)), () => Is.EqualTo(33));

            AddAssert("the second has no additions", () => objectAt(unmatched).Samples.Select(s => s.Name), () => Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL }));
            AddAssert("it sounds as its line says", () => (normalOf(objectAt(unmatched)).Bank, normalVolumeOf(objectAt(unmatched))),
                () => Is.EqualTo((bankOf(lineAt(unmatched + 5)), lineAt(unmatched + 5).Volume)));

            AddAssert("the velocities are as they were", () => EditorBeatmap.HitObjects.OfType<Slider>().ToDictionary(s => s.StartTime, s => s.SliderVelocityMultiplier),
                () => Is.EqualTo(velocities));

            AddStep("undo", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddAssert("the first circle sounds as it did", () => describe(objectAt(matched)), () => Is.EqualTo(samplesBefore));
        }

        /// <summary>
        /// The green lines a few milliseconds from a sound are listed, with whether it takes them: stable reads a sound's
        /// line 5 ms after it (2026-10-08). One it takes all the same changes nothing heard, and is listed only when asked
        /// for (2026-10-10).
        /// </summary>
        [Test]
        public void TestGreenLinesCloseToASoundAreListed()
        {
            AddStep("only those that change something", () => Game.LocalConfig.SetValue(OsuSetting.EditorHitsoundShowHarmlessUnsnaps, false));

            HitObject target = null!;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddStep("put lines in 3 and 7 ms after a circle", () =>
            {
                target = EditorBeatmap.HitObjects.OfType<HitCircle>().ElementAt(10);

                var editing = new GreenLineEditing(EditorBeatmap, (SavedTimingLines)Editor.Dependencies.Get(typeof(SavedTimingLines)));

                var soon = editing.Add(target.StartTime + 3)!.Value;
                editing.Replace(soon, soon.WithVolume(33));

                var later = editing.Add(target.StartTime + 7)!.Value;
                editing.Replace(later, later.WithVolume(44));

                editing.Confirm();
            });

            AddUntilStep("the one 7 ms after is listed as not taken", () => nearLines().Any(l => l.LineTime == target.StartTime + 7 && l.After && l.Distance == 7 && !l.Taken));
            AddAssert("the one 3 ms after, which is taken, is not listed", () => nearLines().Any(l => l.LineTime == target.StartTime + 3), () => Is.False);

            AddStep("click it", () => nearLines().First(l => l.LineTime == target.StartTime + 7).TriggerClick());
            AddUntilStep("the playhead goes to it", () => ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).CurrentTime, () => Is.EqualTo(target.StartTime + 7).Within(1));

            AddStep("ask for those that change nothing too", () => Game.LocalConfig.SetValue(OsuSetting.EditorHitsoundShowHarmlessUnsnaps, true));
            AddUntilStep("the one 3 ms after is listed as taken", () => nearLines().Any(l => l.LineTime == target.StartTime + 3 && l.After && l.Distance == 3 && l.Taken));
            AddStep("back to the default", () => Game.LocalConfig.SetValue(OsuSetting.EditorHitsoundShowHarmlessUnsnaps, false));
        }

        /// <summary>
        /// A placed object's sampleset can be set to auto, to follow its green line as a sampleset of 0 does in stable:
        /// changing the line's then changes its, where one of its own stays, and it is saved as 0 (2026-10-10).
        /// </summary>
        [Test]
        public void TestAnObjectSetToAutoFollowsItsLine()
        {
            HitObject target = null!;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            waitForPanel();
            AddStep("seek to a circle", () =>
            {
                target = EditorBeatmap.HitObjects.OfType<HitCircle>().ElementAt(10);
                ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(target.StartTime);
            });
            AddUntilStep("it is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == target);

            pressWithShift(Key.R);
            AddAssert("its sampleset is drum of its own", () => (normalOf(target).Bank, HitsoundEditing.FollowsLine(normalOf(target))),
                () => Is.EqualTo((HitSampleInfo.BANK_DRUM, false)));

            setLineSampleSet(HitsoundPanel.GreenLineSampleSet.Soft);
            AddAssert("its line going soft leaves it drum", () => normalOf(target).Bank, () => Is.EqualTo(HitSampleInfo.BANK_DRUM));

            pressWithShift(Key.Q);
            AddAssert("set to auto, it is soft as its line", () => (normalOf(target).Bank, HitsoundEditing.FollowsLine(normalOf(target))),
                () => Is.EqualTo((HitSampleInfo.BANK_SOFT, true)));

            setLineSampleSet(HitsoundPanel.GreenLineSampleSet.Normal);
            AddAssert("its line going normal takes it along", () => normalOf(target).Bank, () => Is.EqualTo(HitSampleInfo.BANK_NORMAL));

            AddStep("save", () => Editor.Save());
            AddAssert("it is written with a sampleset of 0", () => objectLineAt(SavedFile(), target.StartTime).Split(',')[5], () => Does.StartWith("0:"));
        }

        private void pressWithShift(Key key) => AddStep($"press shift+{key}", () =>
        {
            InputManager.PressKey(Key.ShiftLeft);
            InputManager.Key(key);
            InputManager.ReleaseKey(Key.ShiftLeft);
        });

        private void setLineSampleSet(HitsoundPanel.GreenLineSampleSet sampleSet) => AddStep($"set the line's sampleset to {sampleSet}",
            () => panel.ChildrenOfType<FormEnumDropdown<HitsoundPanel.GreenLineSampleSet>>().Single().Current.Value = sampleSet);

        private static string objectLineAt(byte[] file, double time) =>
            section(file, "[HitObjects]").First(l => l.Split(',')[2] == Math.Floor(time).ToString(System.Globalization.CultureInfo.InvariantCulture));

        private IEnumerable<HitsoundPanel.NearLine> nearLines() => panel.ChildrenOfType<HitsoundPanel.NearLine>();

        /// <summary>
        /// A difficulty switched to is open before its screens are loaded.
        /// </summary>
        private void waitForPanel() => AddUntilStep("the hitsound screen is loaded", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);

        private HitObject objectAt(double time) => EditorBeatmap.HitObjects.First(h => Math.Abs(h.StartTime - time) < 1);

        private static string[] describe(HitObject hitObject) => hitObject.Samples.Select(s => $"{s.Name}:{s.Bank}:{s.Volume}").ToArray();

        private static HitSampleInfo normalOf(HitObject hitObject) => hitObject.Samples.Single(s => s.Name == HitSampleInfo.HIT_NORMAL);

        private static string bankOf(SavedTimingLine line) => line.SampleSet switch
        {
            2 => HitSampleInfo.BANK_SOFT,
            3 => HitSampleInfo.BANK_DRUM,
            _ => HitSampleInfo.BANK_NORMAL,
        };

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
