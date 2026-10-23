// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Input;
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
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.ReadyForUse == true);

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
        /// With objects selected, what the green line's boxes are given goes to those objects and to nothing else: a line
        /// is put in where a run of them begins, and another at the object after it, saying what that one already did.
        /// The playhead, where the line goes with nothing selected, has no say in it (2026-10-14).
        /// </summary>
        [TestCase(new[] { 30, 31, 32 })]
        [TestCase(new[] { 30, 32, 35 })]
        public void TestALineChangedWithObjectsSelectedChangesThoseAlone(int[] picked)
        {
            HitObject[] selected = null!;
            Dictionary<double, int> linesBefore = null!;
            Dictionary<HitObject, int[]> soundsBefore = null!;
            int volume = 0;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddStep("seek to an object", () => ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(EditorBeatmap.HitObjects[10].StartTime));
            AddStep("select others", () =>
            {
                selected = picked.Select(i => EditorBeatmap.HitObjects[i]).ToArray();
                linesBefore = lineVolumes();
                soundsBefore = EditorBeatmap.HitObjects.ToDictionary(h => h, volumesOf);

                EditorBeatmap.SelectedHitObjects.AddRange(selected);
            });
            AddUntilStep("the panel is for them", () => panel.Target.Select(p => p.HitObject).Distinct(), () => Is.EquivalentTo(selected));

            AddStep("press a volume none of them has", () =>
            {
                // the volumes come after the custom indices.
                var button = panel.ChildrenOfType<HitsoundPanel.PresetButton>().Last(b => b.Value > 5 && timesOf(selected).All(t => linesBefore[t] != b.Value));
                volume = button.Value;
                button.TriggerClick();
            });

            AddAssert("the lines say it where they are", () => timesOf(selected).Select(t => lineVolumes()[t]), () => Is.All.EqualTo(volume));
            AddAssert("and what they said everywhere else", () => lineVolumes().Where(v => !timesOf(selected).Contains(v.Key)),
                () => Is.EquivalentTo(linesBefore.Where(v => !timesOf(selected).Contains(v.Key))));

            AddAssert("they sound it", () => selected.SelectMany(volumesOf), () => Is.All.EqualTo(volume));
            AddAssert("nothing else sounds otherwise", () => EditorBeatmap.HitObjects.Except(selected).All(h => volumesOf(h).SequenceEqual(soundsBefore[h])));

            AddUntilStep("the volume shown is theirs", () => panel.LineVolume.Value, () => Is.EqualTo(volume));
        }

        /// <summary>
        /// What the lines say the volume is at every time something sounds.
        /// </summary>
        private Dictionary<double, int> lineVolumes() => timesOf(EditorBeatmap.HitObjects).Distinct().ToDictionary(t => t, t => lineAt(t + 5).Volume);

        private static IEnumerable<double> timesOf(IEnumerable<HitObject> hitObjects) => hitObjects.SelectMany(h => HitPoints.Of(h, true)).Select(p => p.Time);

        private static int[] volumesOf(HitObject hitObject) => HitPoints.Of(hitObject, true).SelectMany(p => p.Samples).Select(s => s.Volume).ToArray();

        /// <summary>
        /// The custom index goes up and down by the arrows at the end of its box, and 0 is shown as "Default", which is
        /// what stable calls it: in the box, and on its button, which stays first (2026-10-11).
        /// </summary>
        [Test]
        public void TestTheCustomIndexGoesUpAndDownAndZeroIsDefault()
        {
            HitObject target = null!;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddStep("seek to an object", () =>
            {
                target = EditorBeatmap.HitObjects.OfType<HitCircle>().ElementAt(10);
                ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(target.StartTime);
            });
            AddUntilStep("the line's volume is shown", () => panel.LineVolume.Value, () => Is.EqualTo(lineAt(target.StartTime + 5).Volume));

            AddAssert("its custom index is 0", () => lineAt(target.StartTime + 5).SampleIndex, () => Is.Zero);
            AddAssert("shown as Default", () => (indexBox.Current.Value, indexBox.PlaceholderText.ToString()), () => Is.EqualTo((string.Empty, @"Default")));

            step(1);
            AddAssert("up: a green line where the object is says 1", () => indexAt(target.StartTime), () => Is.EqualTo(1));
            AddUntilStep("and so does the box", () => indexBox.Current.Value, () => Is.EqualTo(@"1"));

            step(1);
            AddAssert("up again: 2", () => indexAt(target.StartTime), () => Is.EqualTo(2));

            step(-1);
            step(-1);
            AddAssert("down twice: 0", () => indexAt(target.StartTime), () => Is.Zero);
            AddUntilStep("shown as Default again", () => indexBox.Current.Value, () => Is.Empty);

            step(-1);
            AddAssert("no lower than that", () => indexAt(target.StartTime), () => Is.Zero);

            AddStep("type 7 as the custom index", () =>
            {
                InputManager.MoveMouseTo(indexBox);
                InputManager.Click(MouseButton.Left);
                indexBox.Current.Value = "7";
                InputManager.Key(Key.Enter);
            });

            AddUntilStep("7 is among the buttons", () => panel.IndexPresets.Any(b => b.Value == 7));
            AddAssert("which are in the order of their numbers", () => panel.IndexPresets.Select(b => b.Value), () => Is.Ordered.And.Unique);
            AddAssert("Default first", () => (panel.IndexPresets.First().Value, panel.IndexPresets.First().Label), () => Is.EqualTo((0, @"Default")));
            AddAssert("the rest as their numbers", () => panel.IndexPresets.Skip(1).Select(b => b.Label), () => Is.EqualTo(panel.IndexPresets.Skip(1).Select(b => b.Value.ToString())));

            void step(int by) => AddStep(by > 0 ? "press the up arrow" : "press the down arrow",
                () => panel.ChildrenOfType<HitsoundPanel.IndexStepButton>().Single(b => b.Step == by).TriggerClick());
        }

        private FormNumberBox indexBox => panel.ChildrenOfType<FormNumberBox>().Single();

        private int indexAt(double time) => lines().Last(l => l.Time <= time + 5).SampleIndex;

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
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.ID == imported.ID);
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
        /// With "always apply" on, the hitsound difficulty is copied once, and then onto every object placed or moved in
        /// time; an object whose hitsounds are changed by hand keeps them, through an undo and a redo too (2026-10-23).
        /// </summary>
        [Test]
        public void TestAlwaysApplyCopiesOntoWhatIsPlacedAndMoved()
        {
            double matched = 0;
            double other = 0;
            double moved = 0;

            AddStep("note three circles", () =>
            {
                var circles = EditorBeatmap.HitObjects.OfType<HitCircle>().ToArray();
                matched = circles[10].StartTime;
                other = circles[11].StartTime;
                moved = circles[12].StartTime;
            });

            makeAHitsoundDifficultyWithAClapAt(() => matched);

            AddAssert("the circle here has no clap", () => describe(objectAt(matched)), () => Has.None.Contains("hitclap:drum"));
            AddStep("turn always apply on", () => alwaysApply.Current.Value = true);
            AddAssert("the clap is copied at once", () => string.Join(' ', describe(objectAt(matched))), () => Does.Contain("hitclap:drum"));

            AddStep("take the circle out", () => EditorBeatmap.Remove(objectAt(matched)));
            AddStep("place one where it was", () => EditorBeatmap.Add(new HitCircle
            {
                StartTime = matched,
                Position = new Vector2(256, 192),
                Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) },
            }));
            AddUntilStep("the placed one claps", () => string.Join(' ', describe(objectAt(matched))), () => Does.Contain("hitclap:drum"));

            AddStep("take it out again", () => EditorBeatmap.Remove(objectAt(matched)));
            AddStep("move another circle there", () =>
            {
                var circle = objectAt(moved);
                circle.StartTime = matched;
                EditorBeatmap.Update(circle);
            });
            AddUntilStep("the moved one claps", () => string.Join(' ', describe(objectAt(matched))), () => Does.Contain("hitclap:drum"));

            // a finish, which the hitsound difficulty does not play there: one it does would be kept either way.
            AddAssert("another has no finish", () => describe(objectAt(other)), () => Has.None.StartsWith(HitSampleInfo.HIT_FINISH));
            AddStep("give it a finish by hand", () =>
            {
                // as one change to undo, as the buttons make it.
                EditorBeatmap.BeginChange();
                var circle = objectAt(other);
                HitsoundEditing.SetAddition(new[] { new HitPoint(circle, null, circle.StartTime) }, HitSampleInfo.HIT_FINISH, true, HitSampleInfo.BANK_NORMAL);
                EditorBeatmap.Update(circle);
                EditorBeatmap.EndChange();
            });
            AddWaitStep("let the update go through", 2);
            AddAssert("it keeps the finish", () => describe(objectAt(other)), () => Has.Some.StartsWith(HitSampleInfo.HIT_FINISH));

            AddStep("undo", () => InputManager.Keys(PlatformAction.Undo));
            AddAssert("the finish is gone", () => describe(objectAt(other)), () => Has.None.StartsWith(HitSampleInfo.HIT_FINISH));
            AddStep("redo", () => InputManager.Keys(PlatformAction.Redo));
            AddAssert("the finish is back", () => describe(objectAt(other)), () => Has.Some.StartsWith(HitSampleInfo.HIT_FINISH));
        }

        /// <summary>
        /// "Always apply" is kept for the difficulty, and is on again the next time it is opened, without copying
        /// everything again (2026-10-23).
        /// </summary>
        [Test]
        public void TestAlwaysApplyIsKeptForTheDifficulty()
        {
            double matched = 0;

            AddStep("note a circle", () => matched = EditorBeatmap.HitObjects.OfType<HitCircle>().ToArray()[10].StartTime);

            makeAHitsoundDifficultyWithAClapAt(() => matched);

            AddStep("turn always apply on", () => alwaysApply.Current.Value = true);
            AddStep("save", () => Editor.Save());

            AddStep("switch to the hitsound difficulty", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.DifficultyName == "Hitsounds")!));
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.DifficultyName == "Hitsounds");
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            waitForPanel();
            AddAssert("off there", () => alwaysApply.Current.Value, () => Is.False);

            AddStep("switch back", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!));
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.ID == imported.ID);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            waitForPanel();
            AddAssert("on again here", () => alwaysApply.Current.Value, () => Is.True);
            AddWaitStep("let anything copied go through", 2);
            AddAssert("nothing to undo: not copied again", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).CanUndo.Value, () => Is.False);

            AddStep("take the circle out", () => EditorBeatmap.Remove(objectAt(matched)));
            AddStep("place one where it was", () => EditorBeatmap.Add(new HitCircle
            {
                StartTime = matched,
                Position = new Vector2(256, 192),
                Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) },
            }));
            AddUntilStep("the placed one claps", () => string.Join(' ', describe(objectAt(matched))), () => Does.Contain("hitclap:drum"));

            AddStep("turn it off", () => alwaysApply.Current.Value = false);
            AddStep("save", () => Editor.Save());
            AddStep("switch to the hitsound difficulty", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.DifficultyName == "Hitsounds")!));
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.DifficultyName == "Hitsounds");
            AddStep("switch back", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!));
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.ID == imported.ID);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            waitForPanel();
            AddAssert("still off", () => alwaysApply.Current.Value, () => Is.False);
        }

        /// <summary>
        /// Makes a hitsound difficulty, gives its circle at a time a clap from drum, saves it and goes back to this one,
        /// on the hitsound screen.
        /// </summary>
        private void makeAHitsoundDifficultyWithAClapAt(Func<double> time)
        {
            make(true);

            AddStep("give the circle a clap from drum", () =>
            {
                var circle = objectAt(time());
                HitsoundEditing.SetHitsound(new[] { new HitPoint(circle, null, circle.StartTime) },
                    new PointHitsound(HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_DRUM, new[] { HitSampleInfo.HIT_CLAP }));
                EditorBeatmap.Update(circle);
            });
            AddStep("save", () => Editor.Save());

            AddStep("switch back", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!));
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.ID == imported.ID);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            waitForPanel();
            AddAssert("the hitsound difficulty is the one to copy from", () => panel.ChildrenOfType<FormDropdown<string>>().Single().Current.Value, () => Is.EqualTo("Hitsounds"));
        }

        private FormCheckBox alwaysApply => panel.ChildrenOfType<FormCheckBox>().Single(c => c.Caption == LazerKaiHitsoundStrings.AlwaysCopyFrom);

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

            // ready for use, not just loaded: the editor is loaded before its compose screen is, which goes through the
            // objects on another thread as it loads. A step that changed them meanwhile failed whichever test it was in
            // with "Collection was modified", in some runs of everything and in none of the fixture alone (2026-10-12).
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.ReadyForUse == true);
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
