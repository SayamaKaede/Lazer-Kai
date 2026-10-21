// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The hitsound screen: the buttons and stable's keys change what is selected or, with nothing selected, what sounds
    /// at the playhead — down to one end of a slider (2026-10-08).
    /// </summary>
    [TestFixture]
    public partial class TestSceneHitsoundMode : TestSceneOsuEditor
    {
        private HitCircle first = null!;
        private Slider slider = null!;
        private HitCircle last = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(Ruleset.Value, false);

        private HitsoundPanel panel => Editor.ChildrenOfType<HitsoundPanel>().Single();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("add a circle, a slider with a reverse, and a circle", () =>
            {
                double start = Beatmap.Value.Beatmap.ControlPointInfo.TimingPoints.First().Time;

                EditorBeatmap.Add(first = new HitCircle { StartTime = start, Position = new Vector2(100), Samples = normal() });
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = start + 1000,
                    Position = new Vector2(100, 200),
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(200, 0) }),
                    RepeatCount = 1,
                    Samples = normal(),
                    NodeSamples = new List<IList<HitSampleInfo>> { normal(), normal(), normal() },
                });
                EditorBeatmap.Add(last = new HitCircle { StartTime = start + 5000, Position = new Vector2(300), Samples = normal() });
                EditorBeatmap.Add(spinner = new Spinner { StartTime = start + 7000, EndTime = start + 8000, Position = new Vector2(256, 192), Samples = normal() });
            });
        }

        private Spinner spinner = null!;

        private TimelineHitObjectBlueprint blueprintOf(HitObject hitObject) => Editor.ChildrenOfType<TimelineHitObjectBlueprint>().Single(b => b.Item == hitObject);

        /// <summary>
        /// A spinner sounds only as it ends, and its end is what is outlined when it is selected on the hitsound screen,
        /// rather than all of it as though more of it had been (2026-10-08).
        /// </summary>
        [Test]
        public void TestASpinnersEndIsOutlined()
        {
            switchToHitsounds();
            seekTo(() => spinner.StartTime);

            AddStep("click the spinner's end", () =>
            {
                var blueprint = blueprintOf(spinner);
                InputManager.MoveMouseTo(blueprint.ToScreenSpace(new Vector2(blueprint.DrawWidth, blueprint.DrawHeight / 2)));
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("its end is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].HitObject == spinner);
            AddUntilStep("its end is ringed", () => blueprintOf(spinner).OutlinedPoints, () => Is.EqualTo(new[] { 1f }));
            AddAssert("it is not outlined as a whole", () => blueprintOf(spinner).OutlinedWhole, () => Is.False);
        }

        [Test]
        public void TestTheEndOfASliderIsChangedOnItsOwn()
        {
            switchToHitsounds();
            seekTo(() => slider.EndTime);

            AddUntilStep("the tail is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].IsTail);

            AddStep("press W", () => InputManager.Key(Key.W));

            AddAssert("the tail whistles", () => hasWhistle(slider.NodeSamples[2]));
            AddAssert("the head and the reverse do not", () => !hasWhistle(slider.NodeSamples[0]) && !hasWhistle(slider.NodeSamples[1]));
            AddAssert("nor does the body", () => !hasWhistle(slider.Samples));
            AddAssert("the whistle is lit", () => HitsoundEditing.AdditionState(panel.Target, HitSampleInfo.HIT_WHISTLE), () => Is.EqualTo(Graphics.UserInterface.TernaryState.True));
        }

        [Test]
        public void TestTheKeysSetTheSamplesetAndTheAdditions()
        {
            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddUntilStep("the first circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == first);

            press(Key.ShiftLeft, Key.E);
            AddAssert("its sampleset is soft", () => normalOf(first).Bank, () => Is.EqualTo(HitSampleInfo.BANK_SOFT));

            AddStep("press W", () => InputManager.Key(Key.W));
            AddAssert("it whistles", () => hasWhistle(first.Samples));

            press(Key.ControlLeft, Key.R);
            AddAssert("its additions are drum", () => whistleOf(first), () => Is.EqualTo((HitSampleInfo.BANK_DRUM, false)));

            press(Key.ControlLeft, Key.Q);
            AddAssert("its additions follow the sampleset", () => whistleOf(first), () => Is.EqualTo((HitSampleInfo.BANK_SOFT, true)));

            press(Key.AltLeft, Key.W);
            AddAssert("alt does what ctrl does", () => whistleOf(first), () => Is.EqualTo((HitSampleInfo.BANK_NORMAL, false)));
        }

        /// <summary>
        /// Additions that follow the sampleset are on its bank already, and choosing that bank for them makes it their
        /// own: a change the sound alone does not show, which was taken for none (2026-10-08).
        /// </summary>
        [Test]
        public void TestChoosingTheBankAdditionsFollowMakesItTheirOwn()
        {
            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddUntilStep("the first circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == first);

            press(Key.ShiftLeft, Key.E);
            AddStep("press W", () => InputManager.Key(Key.W));
            AddAssert("its additions follow the sampleset", () => whistleOf(first), () => Is.EqualTo((HitSampleInfo.BANK_SOFT, true)));

            press(Key.ControlLeft, Key.E);
            AddAssert("they are soft of their own", () => whistleOf(first), () => Is.EqualTo((HitSampleInfo.BANK_SOFT, false)));

            press(Key.ControlLeft, Key.Q);
            AddAssert("they follow it again", () => whistleOf(first), () => Is.EqualTo((HitSampleInfo.BANK_SOFT, true)));
        }

        [Test]
        public void TestASelectionIsChangedRatherThanThePlayhead()
        {
            switchToHitsounds();
            seekTo(() => first.StartTime);

            AddStep("select the last circle", () => EditorBeatmap.SelectedHitObjects.Add(last));
            AddStep("press E", () => InputManager.Key(Key.E));

            AddAssert("the last circle has a finish", () => last.Samples.Any(s => s.Name == HitSampleInfo.HIT_FINISH));
            AddAssert("the circle at the playhead does not", () => first.Samples.All(s => s.Name != HitSampleInfo.HIT_FINISH));
        }

        [Test]
        public void TestNothingChangesWhereNothingSounds()
        {
            switchToHitsounds();
            seekTo(() => first.StartTime + 500);

            AddUntilStep("nothing is what the buttons are for", () => panel.Target, () => Is.Empty);
            AddStep("press W", () => InputManager.Key(Key.W));

            AddAssert("nothing whistles", () => EditorBeatmap.HitObjects.SelectMany(allSamples).All(s => !hasWhistle(s)));
        }

        [Test]
        public void TestAChangeIsUndoneAsOne()
        {
            switchToHitsounds();
            seekTo(() => slider.EndTime);
            AddUntilStep("the tail is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].IsTail);

            AddStep("press W", () => InputManager.Key(Key.W));
            AddAssert("the tail whistles", () => hasWhistle(((Slider)EditorBeatmap.HitObjects[1]).NodeSamples[2]));

            press(Key.ControlLeft, Key.Z);
            AddAssert("it no longer does", () => !hasWhistle(((Slider)EditorBeatmap.HitObjects[1]).NodeSamples[2]));
        }

        /// <summary>
        /// The same keys belong to the compose screen while it is showing, where with nothing selected they choose what
        /// the next object is placed with.
        /// </summary>
        [Test]
        public void TestTheComposeScreenKeepsItsKeys()
        {
            seekTo(() => first.StartTime);

            AddStep("press W", () => InputManager.Key(Key.W));
            AddAssert("the circle at the playhead does not whistle", () => !hasWhistle(first.Samples));

            switchToHitsounds();
            AddStep("back to the compose screen", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("the buttons are gone", () => panel.Alpha, () => Is.Zero);

            AddStep("press W", () => InputManager.Key(Key.W));
            AddAssert("the circle at the playhead still does not whistle", () => !hasWhistle(first.Samples));
        }

        /// <summary>
        /// On the hitsound screen a point on the timeline is selected by clicking it, where on the compose screen that
        /// opens the box to edit it: the tail of a slider can be selected on its own, which clicking the slider cannot do.
        /// </summary>
        [Test]
        public void TestClickingAPointSelectsItAlone()
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            clickPoint(() => slider, 2);

            AddUntilStep("the tail alone is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].IsTail);
            AddAssert("the slider is selected", () => EditorBeatmap.SelectedHitObjects, () => Is.EquivalentTo(new[] { slider }));

            AddStep("press W", () => InputManager.Key(Key.W));

            AddAssert("the tail whistles", () => hasWhistle(slider.NodeSamples[2]));
            AddAssert("the rest of the slider does not", () => !hasWhistle(slider.NodeSamples[0]) && !hasWhistle(slider.NodeSamples[1]) && !hasWhistle(slider.Samples));
        }

        [Test]
        public void TestCtrlClickingAPointAddsIt()
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            clickPoint(() => slider, 0);
            waitOutDoubleClick();
            clickPoint(() => slider, 2, Key.ControlLeft);

            AddUntilStep("the head and the tail are what the buttons are for",
                () => panel.Target.Select(p => p.Node).OrderBy(n => n), () => Is.EqualTo(new int?[] { 0, 2 }));
        }

        /// <summary>
        /// A click on a slider in the timeline selects the part of it clicked, as un!stable's timeline does: its head or
        /// its tail where their circles are (2026-10-08).
        /// </summary>
        [TestCase(0)]
        [TestCase(2)]
        public void TestClickingAnEndOfASliderSelectsThatEnd(int node)
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            clickSlider(node == 0 ? 0 : 1);

            AddUntilStep("that end alone is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].HitObject == slider && panel.Target[0].Node == node);
            AddUntilStep("that end is ringed", () => blueprintOf(slider).OutlinedPoints, () => Is.EqualTo(new[] { node / 2f }));
            AddAssert("the slider is not outlined as a whole", () => blueprintOf(slider).OutlinedWhole, () => Is.False);
        }

        /// <summary>
        /// The part of a slider pressed is selected as the mouse goes down, not as it comes up: with the drag threshold
        /// removed, which the timeline asks for, a press that moves at all is a drag and never a click, and a hand is
        /// seldom that still (2026-10-11).
        /// </summary>
        [TestCase(0)]
        [TestCase(2)]
        public void TestPressingAnEndOfASliderSelectsThatEnd(int node)
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            AddStep("press on that end", () => pressSlider(node == 0 ? 0 : 1));
            AddUntilStep("that end alone is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].HitObject == slider && panel.Target[0].Node == node);
            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Left));
            AddUntilStep("as it still is", () => panel.Target.Count == 1 && panel.Target[0].HitObject == slider && panel.Target[0].Node == node);
        }

        /// <summary>
        /// The same press, moved before it is released: a drag, after which no click comes.
        /// </summary>
        [TestCase(0)]
        [TestCase(2)]
        public void TestAPressThatBecomesADragStillSelectsThatEnd(int node)
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            AddStep("press on that end, move, release", () =>
            {
                var at = pressSlider(node == 0 ? 0 : 1);

                // past the distance a test's input manager waits before a drag (10), which the game's does not wait for
                // over the timeline. Into the slider, so that the pointer stays on it.
                InputManager.MoveMouseTo(at + new Vector2(node == 0 ? 12 : -12, 0));
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddUntilStep("that end alone is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].HitObject == slider && panel.Target[0].Node == node);
        }

        /// <summary>
        /// A press on one of several objects selected may be the start of a drag of them all, so it leaves them selected
        /// until it is known to be a click, as it does everywhere else in the editor.
        /// </summary>
        [Test]
        public void TestPressingOneOfSeveralSelectedKeepsThemUntilReleased()
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            AddStep("select the slider and the last circle", () => EditorBeatmap.SelectedHitObjects.AddRange(new HitObject[] { slider, last }));
            AddStep("press on the slider's head", () => pressSlider(0));
            AddAssert("both are still selected", () => EditorBeatmap.SelectedHitObjects, () => Is.EquivalentTo(new HitObject[] { slider, last }));

            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Left));
            AddWaitStep("let the click land", 3);
            AddAssert("the head alone is what the buttons are for",
                () => string.Join(" ", panel.Target.Select(p => $"{p.HitObject.GetType().Name}:{p.Node?.ToString() ?? "body"}")),
                () => Is.EqualTo("Slider:0"));
        }

        /// <summary>
        /// A mark is as easily pressed with a hand that moves.
        /// </summary>
        [Test]
        public void TestPressingAMarkSelectsItsPoint()
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            AddStep("press on the tail's mark, move a little, release", () =>
            {
                var piece = Editor.ChildrenOfType<NodeSamplePointPiece>().Single(p => p.HitObject == slider && p.NodeIndex == 2);

                InputManager.MoveMouseTo(piece);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(piece.ScreenSpaceDrawQuad.Centre + new Vector2(0, -12));
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddUntilStep("the tail alone is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].HitObject == slider && panel.Target[0].Node == 2);
        }

        private Vector2 pressSlider(float fraction)
        {
            var blueprint = blueprintOf(slider);
            var at = blueprint.ToScreenSpace(new Vector2(blueprint.DrawWidth * fraction, blueprint.DrawHeight / 2));

            InputManager.MoveMouseTo(at);
            InputManager.PressButton(MouseButton.Left);
            return at;
        }

        [Test]
        public void TestClickingTheBodyOfASliderSelectsTheBody()
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            clickSlider(0.25f);

            AddUntilStep("the body alone is what the buttons are for", () => panel.Target.Count == 1 && panel.Target[0].HitObject == slider && panel.Target[0].Node == null);
            AddUntilStep("the slider is outlined as a whole", () => blueprintOf(slider).OutlinedWhole);
            AddAssert("no end is ringed", () => blueprintOf(slider).OutlinedPoints, () => Is.Empty);
        }

        [Test]
        public void TestCtrlClickingTheOtherEndAddsIt()
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            clickSlider(0);
            waitOutDoubleClick();
            clickSlider(1, Key.ControlLeft);

            AddUntilStep("the head and the tail are what the buttons are for", () => pointsOf(panel.Target), () => Is.EqualTo("0,2"));
        }

        /// <summary>
        /// Two clicks in quick succession on one slider are a double click, which on the timeline goes to the object's
        /// start in place of the second click. On the hitsound screen the second still selects what it lands on.
        /// </summary>
        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        public void TestAQuickSecondClickStillSelects(float first, float second)
        {
            switchToHitsounds();
            seekTo(() => slider.StartTime);

            clickSlider(first);
            clickSlider(second, Key.ControlLeft);

            AddUntilStep("the head and the tail are what the buttons are for", () => pointsOf(panel.Target), () => Is.EqualTo("0,2"));
        }

        /// <summary>
        /// A key gives what the buttons are for the sampleset, additions' bank and additions of the sounds heard just
        /// before (2026-10-08).
        /// </summary>
        [Test]
        public void TestAKeyMakesTheSoundsTheSameAsTheOnesBefore()
        {
            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddUntilStep("the first circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == first);

            press(Key.ShiftLeft, Key.E);
            AddStep("press W", () => InputManager.Key(Key.W));
            press(Key.ControlLeft, Key.R);

            seekTo(() => slider.StartTime);
            AddUntilStep("the slider's head is what the buttons are for", () => pointsOf(panel.Target), () => Is.EqualTo("0"));

            AddStep("press A", () => InputManager.Key(Key.A));

            AddAssert("what was done is said", () => panel.Status,
                () => Is.EqualTo(LazerKaiHitsoundStrings.StatusCopiedPrevious(first.StartTime.ToEditorFormattedString(), slider.StartTime.ToEditorFormattedString(), 1)));
            AddAssert("the head's sampleset is soft", () => slider.NodeSamples[0].Single(s => s.Name == HitSampleInfo.HIT_NORMAL).Bank, () => Is.EqualTo(HitSampleInfo.BANK_SOFT));
            AddAssert("it whistles from drum", () => slider.NodeSamples[0].Single(s => s.Name == HitSampleInfo.HIT_WHISTLE).Bank, () => Is.EqualTo(HitSampleInfo.BANK_DRUM));
            AddAssert("the reverse is as it was", () => slider.NodeSamples[1].Select(s => (s.Name, s.Bank)), () => Is.EqualTo(new[] { (HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_NORMAL) }));
        }

        /// <summary>
        /// On the hitsound screen, copying copies the hitsounds rather than the objects, and one pasted goes onto what the
        /// buttons are for (2026-10-08).
        /// </summary>
        [Test]
        public void TestCopyingAndPastingTheHitsoundsOfOnePoint()
        {
            int objects = 0;

            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddUntilStep("the first circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == first);

            AddAssert("nothing is said before anything is copied", () => panel.Status.ToString(), () => Is.Empty);

            press(Key.ShiftLeft, Key.R);
            AddStep("press E", () => InputManager.Key(Key.E));
            press(Key.ControlLeft, Key.C);
            AddAssert("the copy is said", () => panel.Status, () => Is.EqualTo(LazerKaiHitsoundStrings.StatusCopied(first.StartTime.ToEditorFormattedString(), 1)));

            seekTo(() => last.StartTime);
            AddUntilStep("the last circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == last);
            AddStep("count the objects", () => objects = EditorBeatmap.HitObjects.Count);
            press(Key.ControlLeft, Key.V);
            AddAssert("the paste is said", () => panel.Status, () => Is.EqualTo(LazerKaiHitsoundStrings.StatusPasted(last.StartTime.ToEditorFormattedString(), 1)));

            AddAssert("its sampleset is drum", () => normalOf(last).Bank, () => Is.EqualTo(HitSampleInfo.BANK_DRUM));
            AddAssert("it has a finish", () => last.Samples.Any(s => s.Name == HitSampleInfo.HIT_FINISH));
            AddAssert("no object was pasted", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(objects));
        }

        /// <summary>
        /// Several pasted keep how far apart they were, from the playhead on: a pattern goes from one bar to the next.
        /// </summary>
        [Test]
        public void TestPastingSeveralKeepsHowFarApartTheyWere()
        {
            HitCircle later = null!;
            HitCircle laterStill = null!;

            AddStep("add two circles as far apart as the first circle and the slider", () =>
            {
                EditorBeatmap.Add(later = new HitCircle { StartTime = first.StartTime + 10000, Position = new Vector2(200), Samples = normal() });
                EditorBeatmap.Add(laterStill = new HitCircle { StartTime = first.StartTime + 11000, Position = new Vector2(250), Samples = normal() });
            });

            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddStep("press W", () => InputManager.Key(Key.W));
            seekTo(() => slider.StartTime);
            AddStep("press R", () => InputManager.Key(Key.R));

            AddStep("select the first circle and the slider's head", () =>
            {
                var selection = Editor.ChildrenOfType<TimelineBlueprintContainer>().Single().SamplePoints;
                selection.SelectPoint(first, SamplePointSelection.Body, false);
                selection.SelectPoint(slider, 0, true);
            });
            AddUntilStep("both are what the buttons are for", () => panel.Target.Count, () => Is.EqualTo(2));
            press(Key.ControlLeft, Key.C);
            AddAssert("the copy is said, from the first to the last", () => panel.Status,
                () => Is.EqualTo(LazerKaiHitsoundStrings.StatusCopied($"{first.StartTime.ToEditorFormattedString()} ~ {slider.StartTime.ToEditorFormattedString()}", 2)));

            // what was copied stays selected: the paste goes where the playhead is all the same (2026-10-10).
            seekTo(() => later.StartTime);
            press(Key.ControlLeft, Key.V);
            AddAssert("the paste is said, from the first to the last", () => panel.Status,
                () => Is.EqualTo(LazerKaiHitsoundStrings.StatusPasted($"{later.StartTime.ToEditorFormattedString()} ~ {laterStill.StartTime.ToEditorFormattedString()}", 2)));

            AddAssert("the first of the two whistles", () => hasWhistle(later.Samples));
            AddAssert("the second claps", () => laterStill.Samples.Any(s => s.Name == HitSampleInfo.HIT_CLAP) && !hasWhistle(laterStill.Samples));
            AddAssert("what was pasted onto is selected", () => EditorBeatmap.SelectedHitObjects, () => Is.EquivalentTo(new[] { later, laterStill }));

            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());
            AddAssert("the paste is no longer said", () => panel.Status.ToString(), () => Is.Empty);
        }

        /// <summary>
        /// What is chosen for the next circle goes back to auto, or to nothing, once one is placed, except in a column kept
        /// (2026-10-10).
        /// </summary>
        [Test]
        public void TestWhatIsChosenForPlacingGoesUnlessKept()
        {
            double firstTime = 0;
            double secondTime = 0;

            switchToHitsounds();
            seekTo(() => firstTime = EditorBeatmap.SnapTime(first.StartTime + 3000, null));
            AddUntilStep("nothing is what the buttons are for", () => panel.Target, () => Is.Empty);
            AddStep("press W", () => InputManager.Key(Key.W));
            press(Key.ShiftLeft, Key.R);
            AddStep("press 2", () => InputManager.Key(Key.Number2));
            AddAssert("that circle whistles", () => hasWhistle(placedAt(firstTime)!.Samples));

            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());
            seekTo(() => secondTime = EditorBeatmap.SnapTime(first.StartTime + 3500, null));

            // auto follows the object before here, which has no lines of its own: the drum circle just placed.
            AddUntilStep("the sampleset for the next is auto again", () => panel.ChildrenOfType<DrawableTernaryButton>().First(b => b.Description == "Auto").Current.Value,
                () => Is.EqualTo(Graphics.UserInterface.TernaryState.True));

            AddStep("press 2", () => InputManager.Key(Key.Number2));
            AddAssert("the next does not whistle", () => !hasWhistle(placedAt(secondTime)!.Samples));

            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());
            AddStep("keep the hitsounds", () => panel.ChildrenOfType<HitsoundPanel.KeepToggle>().ElementAt(2).TriggerClick());
            seekTo(() => EditorBeatmap.SnapTime(first.StartTime + 4000, null));
            AddStep("press W", () => InputManager.Key(Key.W));
            AddStep("press 2", () => InputManager.Key(Key.Number2));
            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());
            seekTo(() => EditorBeatmap.SnapTime(first.StartTime + 4500, null));
            AddStep("press 2", () => InputManager.Key(Key.Number2));
            AddAssert("the whistle is kept for the one after", () => hasWhistle(placedAt(EditorBeatmap.SnapTime(first.StartTime + 4500, null))!.Samples));
        }

        /// <summary>
        /// The timeline's points say the additions after the sampleset, and the additions' bank where it is another, as
        /// "S/D WC" (2026-10-08).
        /// </summary>
        [Test]
        public void TestTheTimelineSaysTheAdditions()
        {
            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddUntilStep("the first circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == first);

            press(Key.ShiftLeft, Key.E);
            AddStep("press W", () => InputManager.Key(Key.W));
            AddStep("press R", () => InputManager.Key(Key.R));

            AddUntilStep("its point says the whistle and the clap", () => labelOf(first), () => Does.StartWith("S WC"));

            press(Key.ControlLeft, Key.R);
            AddUntilStep("and their bank once it is another", () => labelOf(first), () => Does.StartWith("S/D WC"));
        }

        /// <summary>
        /// 2 puts a circle in on the beat at the playhead, as a hitsound difficulty has them: in the middle, starting a
        /// combo (2026-10-10).
        /// </summary>
        [Test]
        public void TestTwoPlacesACircleAtThePlayhead()
        {
            double time = 0;
            int objects = 0;

            switchToHitsounds();
            AddStep("count the objects", () => objects = EditorBeatmap.HitObjects.Count);
            seekTo(() => time = EditorBeatmap.SnapTime(first.StartTime + 3000, null));
            AddStep("select the last circle", () => EditorBeatmap.SelectedHitObjects.Add(last));

            AddStep("press 2", () => InputManager.Key(Key.Number2));

            AddAssert("a circle more", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(objects + 1));
            AddAssert("it is at the playhead, in the middle, starting a combo", () => placedAt(time) is HitCircle circle
                                                                                    && circle.Position == OsuPlayfield.BASE_SIZE / 2 && circle.NewCombo);
            AddAssert("it alone is selected", () => EditorBeatmap.SelectedHitObjects, () => Is.EqualTo(new[] { placedAt(time) }));
        }

        /// <summary>
        /// Where something sounds already, 2 gives it what was chosen rather than stacking a circle on it (2026-10-10).
        /// </summary>
        [Test]
        public void TestPlacingWhereSomethingSoundsGivesItTheSounds()
        {
            int objects = 0;

            switchToHitsounds();
            seekTo(() => EditorBeatmap.SnapTime(first.StartTime + 3000, null));
            AddUntilStep("nothing is what the buttons are for", () => panel.Target, () => Is.Empty);
            AddStep("press W, for the next circle", () => InputManager.Key(Key.W));

            seekTo(() => slider.EndTime);
            AddStep("count the objects", () => objects = EditorBeatmap.HitObjects.Count);
            AddStep("press 2", () => InputManager.Key(Key.Number2));

            AddAssert("nothing is put in", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(objects));
            AddAssert("the slider's tail whistles", () => hasWhistle(slider.NodeSamples[2]));
            AddAssert("the tail alone is selected", () => pointsOf(panel.Target), () => Is.EqualTo("2"));
        }

        private HitObject? placedAt(double time) => EditorBeatmap.HitObjects.OfType<HitCircle>().SingleOrDefault(h => h.StartTime == time);

        /// <summary>
        /// With nothing selected and nothing heard at the playhead, the buttons are for the next circle placed.
        /// </summary>
        [Test]
        public void TestTheButtonsSetWhatTwoPlaces()
        {
            double time = 0;

            switchToHitsounds();
            seekTo(() => time = EditorBeatmap.SnapTime(first.StartTime + 3000, null));
            AddUntilStep("nothing is what the buttons are for", () => panel.Target, () => Is.Empty);

            AddStep("press W", () => InputManager.Key(Key.W));
            press(Key.ShiftLeft, Key.R);
            AddAssert("nothing changed", () => EditorBeatmap.HitObjects.SelectMany(allSamples).All(s => !hasWhistle(s)));

            AddStep("press 2", () => InputManager.Key(Key.Number2));

            AddAssert("the circle whistles", () => hasWhistle(placedAt(time)!.Samples));
            AddAssert("its sampleset is drum", () => normalOf(placedAt(time)!).Bank, () => Is.EqualTo(HitSampleInfo.BANK_DRUM));
        }

        /// <summary>
        /// The playhead moves on by the gap before a circle placed, if the hitsound screen's setting says so (2026-10-10).
        /// </summary>
        [Test]
        public void TestPlacingSeeksOnBySetting()
        {
            double time = 0;

            AddStep("turn the setting on", () => config.SetValue(OsuSetting.EditorHitsoundAdvanceByPreviousSpacing, true));

            switchToHitsounds();
            seekTo(() => time = EditorBeatmap.SnapTime(last.StartTime + 1000, null));
            AddStep("press 2", () => InputManager.Key(Key.Number2));

            AddUntilStep("the playhead is as far on as the gap before", () => EditorClock.CurrentTime, () => Is.EqualTo(EditorBeatmap.SnapTime(time + (time - last.StartTime), null)).Within(1));
            AddStep("turn the setting off", () => config.SetValue(OsuSetting.EditorHitsoundAdvanceByPreviousSpacing, false));
        }

        /// <summary>
        /// Ctrl+V puts a circle in to have the hitsounds where nothing is heard; ctrl+shift+V pastes the hitsounds alone
        /// (2026-10-10).
        /// </summary>
        [Test]
        public void TestPastingPutsACircleInWhereNothingSounds()
        {
            double time = 0;
            int objects = 0;

            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddUntilStep("the first circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == first);
            AddStep("press W", () => InputManager.Key(Key.W));
            press(Key.ControlLeft, Key.C);

            seekTo(() => time = EditorBeatmap.SnapTime(first.StartTime + 3000, null));
            press(Key.ControlLeft, Key.V);
            AddAssert("a circle is put in that whistles", () => placedAt(time) is HitCircle circle && hasWhistle(circle.Samples));

            AddStep("count the objects", () => objects = EditorBeatmap.HitObjects.Count);
            seekTo(() => EditorBeatmap.SnapTime(first.StartTime + 4000, null));
            AddStep("press ctrl+shift+V", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.PressKey(Key.ShiftLeft);
                InputManager.Key(Key.V);
                InputManager.ReleaseKey(Key.ShiftLeft);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddAssert("nothing is put in", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(objects));
            AddAssert("that is said", () => panel.Status, () => Is.EqualTo(LazerKaiHitsoundStrings.StatusNothingToPasteOnto));
        }

        /// <summary>
        /// Shift with 2 gives the circle put in the hitsounds heard before it.
        /// </summary>
        [Test]
        public void TestShiftTwoPlacesACircleWithThePreviousHitsounds()
        {
            double time = 0;

            switchToHitsounds();
            seekTo(() => first.StartTime);
            AddUntilStep("the first circle is what the buttons are for", () => panel.Target.SingleOrDefault().HitObject == first);
            AddStep("press W", () => InputManager.Key(Key.W));

            seekTo(() => time = EditorBeatmap.SnapTime(first.StartTime + 500, null));
            AddStep("press E, for the next circle", () => InputManager.Key(Key.E));
            press(Key.ShiftLeft, Key.Number2);

            AddAssert("the circle put in whistles", () => EditorBeatmap.HitObjects.Single(h => h.StartTime == time).Samples.Any(s => s.Name == HitSampleInfo.HIT_WHISTLE));
            AddAssert("without the finish chosen for the next circle", () => EditorBeatmap.HitObjects.Single(h => h.StartTime == time).Samples.All(s => s.Name != HitSampleInfo.HIT_FINISH));
            AddAssert("what was done is said", () => panel.Status,
                () => Is.EqualTo(LazerKaiHitsoundStrings.StatusCopiedPrevious(first.StartTime.ToEditorFormattedString(), time.ToEditorFormattedString(), 1)));
        }

        /// <summary>
        /// The compose screen's toolboxes cannot be used under the hitsound screen, and only got in the way of seeing the
        /// playfield through it (2026-10-10).
        /// </summary>
        [Test]
        public void TestTheToolboxesAreHiddenOnTheHitsoundScreen()
        {
            switchToHitsounds();
            AddUntilStep("the toolboxes are hidden", () => toolboxes().All(t => t.Parent!.Alpha == 0));

            AddStep("back to the compose screen", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("they are back", () => toolboxes().All(t => t.Parent!.Alpha == 1));

            IEnumerable<ExpandingToolboxContainer> toolboxes() => Editor.ChildrenOfType<HitObjectComposer>().Single().ChildrenOfType<ExpandingToolboxContainer>();
        }

        /// <summary>
        /// While playing, the additions' bank of the last sound heard with additions stays lit until the next, rather than
        /// flashing as each is heard (2026-10-10).
        /// </summary>
        [Test]
        public void TestTheAdditionsBankHeardStaysLitWhilePlaying()
        {
            switchToHitsounds();

            // not with the buttons, which would make drum the bank chosen for the next addition, lit either way.
            AddStep("give the first circle a whistle from drum", () =>
            {
                HitsoundEditing.SetHitsound(new[] { new HitPoint(first, null, first.StartTime) },
                    new PointHitsound(HitSampleInfo.BANK_NORMAL, HitSampleInfo.BANK_DRUM, new[] { HitSampleInfo.HIT_WHISTLE }));
                EditorBeatmap.Update(first);
            });

            AddStep("play from just before it", () =>
            {
                EditorClock.Seek(first.StartTime - 100);
                EditorClock.Start();
            });

            AddUntilStep("past it and before the slider, the drum additions are lit", () => EditorClock.CurrentTime > first.StartTime + 50 && EditorClock.CurrentTime < slider.StartTime
                                                                                             && additionsDrum().Current.Value == Graphics.UserInterface.TernaryState.True);
            AddStep("stop", () => EditorClock.Stop());

            // the sampleset's column comes first, with a drum of its own.
            DrawableTernaryButton additionsDrum() => panel.ChildrenOfType<DrawableTernaryButton>().Where(b => b.Description == "Drum").ElementAt(1);
        }

        private string labelOf(HitObject hitObject) =>
            Editor.ChildrenOfType<SamplePointPiece>().Single(p => p.HitObject == hitObject && p is not NodeSamplePointPiece)
                  .ChildrenOfType<Graphics.Sprites.OsuSpriteText>().First().Text.ToString();

        /// <summary>
        /// Lets enough time pass that the next click is not taken for the second of a double click, which reaches no
        /// object: two steps follow each other in a few milliseconds.
        /// </summary>
        private void waitOutDoubleClick()
        {
            double clickedAt = 0;

            AddStep("note the time", () => clickedAt = Time.Current);
            AddUntilStep("wait out a double click", () => Time.Current > clickedAt + 500);
        }

        private static string pointsOf(IEnumerable<HitPoint> points) =>
            string.Join(",", points.Select(p => p.Node?.ToString() ?? "body").OrderBy(s => s, System.StringComparer.Ordinal));

        private void clickSlider(float fraction, Key? modifier = null)
        {
            AddStep($"click the slider {fraction:P0} of the way along{(modifier != null ? $" with {modifier}" : string.Empty)}", () =>
            {
                var blueprint = Editor.ChildrenOfType<TimelineHitObjectBlueprint>().Single(b => b.Item == slider);

                if (modifier != null)
                    InputManager.PressKey(modifier.Value);

                InputManager.MoveMouseTo(blueprint.ToScreenSpace(new Vector2(blueprint.DrawWidth * fraction, blueprint.DrawHeight / 2)));
                InputManager.Click(MouseButton.Left);

                if (modifier != null)
                    InputManager.ReleaseKey(modifier.Value);
            });
        }

        [Test]
        public void TestClickingTheMiddleLetsGoOfTheSelection()
        {
            switchToHitsounds();

            AddStep("select the last circle", () => EditorBeatmap.SelectedHitObjects.Add(last));
            AddStep("click beside the buttons", () =>
            {
                // beside rather than below: with the stable-format notice up (2026-10-15) the last row of buttons reaches
                // nearly to the bottom, so the middle of the bottom edge is a button.
                InputManager.MoveMouseTo(panel.ToScreenSpace(new Vector2(panel.DrawWidth - 20, panel.DrawHeight - 20)));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("nothing is selected", () => EditorBeatmap.SelectedHitObjects, () => Is.Empty);
        }

        /// <summary>
        /// The arrow keys stop where a sound is played, a slider's reverse and tail included, rather than on every beat.
        /// </summary>
        [Test]
        public void TestTheArrowsStopWhereSoundsArePlayed()
        {
            switchToHitsounds();
            seekTo(() => first.StartTime);

            pressArrow(Key.Right, () => slider.StartTime);
            pressArrow(Key.Right, () => slider.StartTime + slider.SpanDuration);
            pressArrow(Key.Right, () => slider.EndTime);
            pressArrow(Key.Left, () => slider.StartTime + slider.SpanDuration);
        }

        /// <summary>
        /// Held while playing, the left key goes back a point a repeat, going on from the point the last went to
        /// (2026-10-11).
        /// </summary>
        /// <remarks>
        /// Going by the playhead, a repeat found the point the one before it had gone to: it had played on past it since.
        /// So the key held went back to one point over and over, for as long as it was held.
        ///
        /// The press and its repeat are handed over rather than typed, with the time between them put in by a seek: how
        /// far a test plays on between two repeats is not something it can count on. Repeats come 70ms apart.
        /// </remarks>
        [Test]
        public void TestARepeatOfLeftGoesOnFromThePointTheLastWentTo()
        {
            switchToHitsounds();
            seekTo(() => slider.EndTime + 50);

            AddStep("play", () => EditorClock.Start());
            AddUntilStep("playing", () => EditorClock.IsRunning);

            AddStep("press left, play on, and repeat", () =>
            {
                panel.OnPressed(new KeyBindingPressEvent<GlobalAction>(InputManager.CurrentState, GlobalAction.EditorSeekBackwards));

                EditorClock.Seek(slider.EndTime + 70);
                panel.OnPressed(new KeyBindingPressEvent<GlobalAction>(InputManager.CurrentState, GlobalAction.EditorSeekBackwards, true));

                EditorClock.Stop();
            });

            AddAssert("at the reverse, the point before the one the press went to", () => EditorClock.CurrentTime,
                () => Is.InRange(slider.StartTime + slider.SpanDuration - 1, slider.StartTime + slider.SpanDuration + 200));
        }

        private void pressArrow(Key key, System.Func<double> expected)
        {
            AddStep($"press {key}", () => InputManager.Key(key));
            AddUntilStep("arrived", () => EditorClock.CurrentTime, () => Is.EqualTo(expected()).Within(1));
        }

        private void clickPoint(System.Func<HitObject> hitObject, int node, Key? modifier = null)
        {
            AddStep($"click point {node}{(modifier != null ? $" with {modifier}" : string.Empty)}", () =>
            {
                var piece = Editor.ChildrenOfType<NodeSamplePointPiece>().Single(p => p.HitObject == hitObject() && p.NodeIndex == node);

                if (modifier != null)
                    InputManager.PressKey(modifier.Value);

                InputManager.MoveMouseTo(piece);
                InputManager.Click(MouseButton.Left);

                if (modifier != null)
                    InputManager.ReleaseKey(modifier.Value);
            });
        }

        private void switchToHitsounds()
        {
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("the buttons are up", () => panel.Alpha, () => Is.EqualTo(1));
        }

        private void seekTo(System.Func<double> time)
        {
            AddStep("seek", () => EditorClock.Seek(time()));
            AddUntilStep("arrived", () => EditorClock.CurrentTime, () => Is.EqualTo(time()).Within(1));
        }

        private void press(Key modifier, Key key) => AddStep($"press {modifier}+{key}", () =>
        {
            InputManager.PressKey(modifier);
            InputManager.Key(key);
            InputManager.ReleaseKey(modifier);
        });

        private static List<HitSampleInfo> normal() => new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_NORMAL) };

        private static bool hasWhistle(IEnumerable<HitSampleInfo> samples) => samples.Any(s => s.Name == HitSampleInfo.HIT_WHISTLE);

        private static HitSampleInfo normalOf(HitObject hitObject) => hitObject.Samples.Single(s => s.Name == HitSampleInfo.HIT_NORMAL);

        private static (string bank, bool auto) whistleOf(HitObject hitObject)
        {
            var whistle = hitObject.Samples.Single(s => s.Name == HitSampleInfo.HIT_WHISTLE);
            return (whistle.Bank, whistle.EditorAutoBank);
        }

        private static IEnumerable<IList<HitSampleInfo>> allSamples(HitObject hitObject)
        {
            yield return hitObject.Samples;

            if (hitObject is IHasRepeats repeats)
            {
                foreach (var node in repeats.NodeSamples)
                    yield return node;
            }
        }
    }
}
