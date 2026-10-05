// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Localisation;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Turning a slider into a stream, which this fork does through a panel rather than in one go.
    /// </summary>
    /// <remarks>
    /// The shortcut opens the panel and shows what the slider would become; nothing is settled until the conversion
    /// is taken. Until then there is nothing on the undo stack. The division starts from the editor's, since that is
    /// what the hand is already playing to, but moving it here moves only the panel's -- reaching for a different
    /// stream should not move every other snap in the beatmap.
    /// </remarks>
    public partial class TestSceneSliderStreamConversion : TestSceneOsuEditor
    {
        /// <summary>The division the panel is building on, which is its own once it has opened.</summary>
        private BindableBeatDivisor beatDivisor => (BindableBeatDivisor)panel.Dependencies.Get(typeof(BindableBeatDivisor));

        [Test]
        public void TestSimpleConversion()
        {
            Slider slider = null;

            AddStep("select first slider", () =>
            {
                slider = (Slider)EditorBeatmap.HitObjects.First(h => h is Slider);
                EditorClock.Seek(slider.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            convertToStream();

            AddAssert("stream created", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 0.25, pathPosition: 0.25),
                (time: 0.5, pathPosition: 0.5),
                (time: 0.75, pathPosition: 0.75),
                (time: 1, pathPosition: 1)));

            takeTheConversion();

            AddStep("undo", () => Editor.Undo());
            AddAssert("slider restored", () => sliderRestored(slider));

            AddStep("select first slider", () =>
            {
                slider = (Slider)EditorBeatmap.HitObjects.First(h => h is Slider);
                EditorClock.Seek(slider.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });
            convertToStream();
            AddStep("change beat divisor", () => beatDivisor.Value = 8);

            AddAssert("stream created", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 0.125, pathPosition: 0.125),
                (time: 0.25, pathPosition: 0.25),
                (time: 0.375, pathPosition: 0.375),
                (time: 0.5, pathPosition: 0.5),
                (time: 0.625, pathPosition: 0.625),
                (time: 0.75, pathPosition: 0.75),
                (time: 0.875, pathPosition: 0.875),
                (time: 1, pathPosition: 1)));
        }

        /// <summary>
        /// The count says how many circles to lay down. The division goes on saying how far apart they are, so
        /// asking for more carries the stream past the end of the slider rather than packing them in tighter.
        /// </summary>
        [Test]
        public void TestTheCountFitsMoreInThanTheDivisionDoes()
        {
            Slider slider = null;

            AddStep("select first slider", () =>
            {
                slider = (Slider)EditorBeatmap.HitObjects.First(h => h is Slider);
                EditorClock.Seek(slider.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            convertToStream();

            AddAssert("the division gives five", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 0.25, pathPosition: 0.25),
                (time: 0.5, pathPosition: 0.5),
                (time: 0.75, pathPosition: 0.75),
                (time: 1, pathPosition: 1)));

            AddStep("ask for nine", () => panel.NoteCount.Value = 9);

            // still a quarter apart -- the four extra land after the slider has finished, and the path is still
            // covered end to end because that is what the positions are spread over.
            AddAssert("nine of them, a quarter apart", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 0.25, pathPosition: 0.125),
                (time: 0.5, pathPosition: 0.25),
                (time: 1, pathPosition: 0.5),
                (time: 1.5, pathPosition: 0.75),
                (time: 2, pathPosition: 1)));

            AddStep("move the panel's division", () => panel.Divisor.Value = 8);
            AddAssert("the count follows it again", () => panel.NoteCount.Value, () => Is.EqualTo(9));
        }

        [Test]
        public void TestConversionWithNonMatchingDivisor()
        {
            Slider slider = null;

            AddStep("select second slider", () =>
            {
                slider = (Slider)EditorBeatmap.HitObjects.Where(h => h is Slider).ElementAt(1);
                EditorClock.Seek(slider.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });
            convertToStream();

            AddStep("change beat divisor", () =>
            {
                beatDivisor.ValidDivisors.Value = BeatDivisorPresetCollection.TRIPLETS;
                beatDivisor.Value = 3;
            });

            // the division does not divide this slider evenly, so the last circle lands short of its end in time.
            // The stream still covers the whole shape: the positions are spread from one end of the path to the
            // other, which is the progress acceleration and easing are laid over. The game's own conversion leaves
            // the stream short of the slider's end instead.
            AddAssert("stream created", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 2 / 3d, pathPosition: 1)));
        }

        [Test]
        public void TestConversionWithRepeats()
        {
            Slider slider = null;

            AddStep("select first slider with repeats", () =>
            {
                slider = (Slider)EditorBeatmap.HitObjects.First(h => h is Slider s && s.RepeatCount > 0);
                EditorClock.Seek(slider.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });
            AddStep("change beat divisor", () => beatDivisor.Value = 2);

            convertToStream();

            AddAssert("stream created", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 0.25, pathPosition: 0.5),
                (time: 0.5, pathPosition: 1),
                (time: 0.75, pathPosition: 0.5),
                (time: 1, pathPosition: 0)));
        }

        [Test]
        public void TestConversionPreservesSliderProperties()
        {
            Slider slider = null;

            AddStep("select second new-combo-starting slider", () =>
            {
                slider = (Slider)EditorBeatmap.HitObjects.Where(h => h is Slider s && s.NewCombo).ElementAt(1);
                EditorClock.Seek(slider.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            convertToStream();

            AddAssert("stream created", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 0.25, pathPosition: 0.25),
                (time: 0.5, pathPosition: 0.5),
                (time: 0.75, pathPosition: 0.75),
                (time: 1, pathPosition: 1)));

            takeTheConversion();

            AddStep("undo", () => Editor.Undo());
            AddAssert("slider restored", () => sliderRestored(slider));
        }

        private void takeTheConversion()
            => AddStep("take the conversion", () => panel.ChildrenOfType<RoundedButton>()
                                                        .Single(b => b.Text.Equals(EditorStrings.ConvertToStream))
                                                        .TriggerClick());

        private StreamConversionPanel panel => Editor.ChildrenOfType<StreamConversionPanel>().Single();

        private void convertToStream()
        {
            AddStep("convert to stream", () =>
            {
                InputManager.PressKey(Key.LControl);
                InputManager.PressKey(Key.LShift);
                InputManager.Key(Key.F);
                InputManager.ReleaseKey(Key.LShift);
                InputManager.ReleaseKey(Key.LControl);
            });
        }

        [Test]
        public void TestFloatEdgeCaseConversion()
        {
            Slider slider = null;

            AddStep("select first slider", () =>
            {
                slider = (Slider)EditorBeatmap.HitObjects.First(h => h is Slider);
                EditorClock.Seek(slider.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            AddStep("change to these specific circumstances", () =>
            {
                EditorBeatmap.Difficulty.SliderMultiplier = 1;
                var timingPoint = EditorBeatmap.ControlPointInfo.TimingPointAt(slider.StartTime);
                timingPoint.BeatLength = 352.941176470588;
                slider.Path.ControlPoints[^1].Position = new Vector2(-110, 16);
                slider.Path.ExpectedDistance.Value = 100;
            });

            convertToStream();

            AddAssert("stream created", () => streamCreatedFor(slider,
                (time: 0, pathPosition: 0),
                (time: 0.25, pathPosition: 0.25),
                (time: 0.5, pathPosition: 0.5),
                (time: 0.75, pathPosition: 0.75),
                (time: 1, pathPosition: 1)));
        }

        private bool streamCreatedFor(Slider slider, params (double time, double pathPosition)[] expectedCircles)
        {
            if (EditorBeatmap.HitObjects.Contains(slider))
                return false;

            foreach ((double expectedTime, double expectedPathPosition) in expectedCircles)
            {
                double time = slider.StartTime + slider.Duration * expectedTime;

                // on the path, and on whole coordinates as the beatmap keeps them (2026-10-05): a stream laid along a
                // curve lands on fractions, which are cut the way every other position is.
                Vector2 position = Editor.ChildrenOfType<OsuHitObjectComposer>().Single()
                                         .RoundPositionIfAsked(slider.Position + slider.Path.PositionAt(expectedPathPosition));

                if (!EditorBeatmap.HitObjects.OfType<HitCircle>().Any(h => matches(h, time, position, slider.NewCombo && expectedTime == 0)))
                    return false;
            }

            return true;

            bool matches(HitCircle circle, double time, Vector2 position, bool startsNewCombo) =>
                Precision.AlmostEquals(circle.StartTime, time, 1)
                && Precision.AlmostEquals(circle.Position, position, 0.01f)
                && circle.NewCombo == startsNewCombo
                && circle.Samples.SequenceEqual(slider.HeadCircle.Samples);
        }

        private bool sliderRestored(Slider slider)
        {
            var objects = EditorBeatmap.HitObjects.Where(h => h.StartTime >= slider.StartTime && h.GetEndTime() <= slider.EndTime).ToList();

            if (objects.Count > 1)
                return false;

            var hitObject = objects.Single();
            if (!(hitObject is Slider restoredSlider))
                return false;

            return Precision.AlmostEquals(slider.StartTime, restoredSlider.StartTime)
                   && Precision.AlmostEquals(slider.GetEndTime(), restoredSlider.GetEndTime())
                   && Precision.AlmostEquals(slider.Position, restoredSlider.Position, 0.01f)
                   && Precision.AlmostEquals(slider.EndPosition, restoredSlider.EndPosition, 0.01f);
        }
    }
}
