// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
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
            AddStep("click below the buttons", () =>
            {
                InputManager.MoveMouseTo(panel.ToScreenSpace(new Vector2(panel.DrawWidth / 2, panel.DrawHeight - 20)));
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
