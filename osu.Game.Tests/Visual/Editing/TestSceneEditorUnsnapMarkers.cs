// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Framework.Graphics.Colour;
using osu.Game.Graphics;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The mark the timeline puts over objects that do not sit on the beat.
    /// </summary>
    [TestFixture]
    public partial class TestSceneEditorUnsnapMarkers : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        /// <summary>
        /// The beat the object is placed on, taken from the beatmap rather than assumed, so the test still means
        /// what it says if the test beatmap's timing changes.
        /// </summary>
        private double snappedTime;

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("clear the beatmap", () =>
            {
                config.SetValue(OsuSetting.EditorShowUnsnapMarkers, true);

                EditorBeatmap.Clear();
                snappedTime = EditorBeatmap.ControlPointInfo.GetClosestSnappedTime(3000);
            });
        }

        /// <summary>
        /// An object on the beat is not marked. Marking everything would be the same as marking nothing.
        /// </summary>
        [Test]
        public void TestNothingIsMarkedWhenSnapped()
        {
            AddStep("place it on the beat", () => place(snappedTime));

            AddUntilStep("it is in the timeline", markers, () => Is.Not.Empty);
            AddAssert("no mark is showing", () => markers().All(m => m.Alpha == 0), () => Is.True);
        }

        /// <summary>
        /// Past what the ranking criteria allows, the object is marked.
        /// </summary>
        /// <remarks>
        /// Two and a half milliseconds is the case worth having: it is over the line and so must be marked, but
        /// it reads as "3 ms" nowhere and would be missed by a rule written against the displayed number.
        /// </remarks>
        [TestCase(2.5)]
        [TestCase(5)]
        public void TestOffTheBeatIsMarked(double offset)
        {
            AddStep($"place it {offset} ms off", () => place(snappedTime + offset));

            AddUntilStep("a mark is showing", () => markers().Any(m => m.Alpha > 0), () => Is.True);

            AddAssert($"it reads {offset} ms out",
                () => markers().First(m => m.Alpha > 0).Unsnap, () => Is.EqualTo(offset).Within(0.01));

            AddAssert("marked as a problem", () => markers().First(m => m.Alpha > 0).Colour.TopLeft.Equals((SRGBColour)colours.Red1), () => Is.True);
        }

        /// <summary>
        /// Within what the ranking criteria allows, nothing is shown.
        /// </summary>
        /// <remarks>
        /// A fraction out is every object end, by the way slider velocity divides. The rest is allowed outright,
        /// and marking it would be saying a map is wrong when it is not.
        /// </remarks>
        [TestCase(0.4)]
        [TestCase(1.4)]
        [TestCase(1.9)]
        public void TestAToleratedUnsnapIsNotMarked(double offset)
        {
            AddStep($"place it {offset} ms off", () => place(snappedTime + offset));

            AddUntilStep("it is in the timeline", markers, () => Is.Not.Empty);
            AddAssert("no mark is showing", () => markers().All(m => m.Alpha == 0), () => Is.True);
        }

        /// <summary>
        /// A reverse is a moment the player has to hit, so it gets a mark like the two ends do.
        /// </summary>
        /// <remarks>
        /// The verifier has always reported unsnapped reverses. The timeline was marking only where an object
        /// began and ended, so a slider could be marked nowhere and still be called a problem.
        /// </remarks>
        /// <remarks>
        /// A slider's length is decided by its velocity, so a test cannot simply ask for one whose reverses land
        /// on the beat. What is checked instead is that every moment the player has to hit is being read: with
        /// two reverses there are four of them, and each mark has to be reporting its own.
        /// </remarks>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TestEveryReverseGetsAMark(int repeatCount)
        {
            AddStep($"place a slider with {repeatCount} reverses, off the beat", () => EditorBeatmap.Add(new Slider
            {
                StartTime = snappedTime + 5,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
                RepeatCount = repeatCount,
            }));

            AddUntilStep("a mark for the head, each reverse and the tail",
                () => markers().Length, () => Is.EqualTo(repeatCount + 2));

            AddAssert("each one reads its own moment", () => markers().Select(m => m.Unsnap).OrderBy(u => u),
                () => Is.EqualTo(expectedUnsnaps()).Within(0.01));
        }

        /// <summary>
        /// How far off the beat each moment of the placed slider is, worked out here rather than assumed.
        /// </summary>
        private IEnumerable<double> expectedUnsnaps()
        {
            var slider = (Slider)EditorBeatmap.HitObjects.Single();

            double span = slider.Duration / (slider.RepeatCount + 1);

            return Enumerable.Range(0, slider.RepeatCount + 2)
                             .Select(i => slider.StartTime + span * i)
                             .Select(t => Math.Abs(t - EditorBeatmap.ControlPointInfo.GetClosestSnappedTime(t)))
                             .OrderBy(u => u);
        }

        /// <summary>
        /// And it can be turned off, for someone who does not want it.
        /// </summary>
        [Test]
        public void TestItCanBeTurnedOff()
        {
            AddStep("place it off the beat", () => place(snappedTime + 5));
            AddUntilStep("a mark is showing", () => markers().Any(m => m.Alpha > 0), () => Is.True);

            AddStep("turn it off", () => config.SetValue(OsuSetting.EditorShowUnsnapMarkers, false));
            AddUntilStep("no mark is showing", () => markers().All(m => m.Alpha == 0), () => Is.True);

            AddStep("turn it back on", () => config.SetValue(OsuSetting.EditorShowUnsnapMarkers, true));
            AddUntilStep("it is back", () => markers().Any(m => m.Alpha > 0), () => Is.True);
        }

        /// <summary>
        /// The mark sits over the object it is about, so it must not be something to click on instead of it.
        /// </summary>
        [Test]
        public void TestTheMarkTakesNoInput()
        {
            AddStep("place it off the beat", () => place(snappedTime + 5));
            AddUntilStep("a mark is showing", () => markers().Any(m => m.Alpha > 0), () => Is.True);

            AddAssert("it is not there to be clicked",
                () => markers().All(m => !m.ReceivePositionalInputAt(m.ScreenSpaceDrawQuad.Centre)), () => Is.True);
        }

        private void place(double time) => EditorBeatmap.Add(new HitCircle { StartTime = time });

        private UnsnapMarker[] markers() => Editor.ChildrenOfType<UnsnapMarker>().ToArray();
    }
}
