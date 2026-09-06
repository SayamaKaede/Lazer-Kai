// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Visualisations;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The two layouts the timeline can be read in. Stable's rests the objects on the bottom edge and stacks the
    /// velocity and volume rows above them, where lazer's centres the objects with a row on either side.
    /// </summary>
    [TestFixture]
    public partial class TestSceneTimelineStyle : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [SetUpSteps]
        public void SetUpStyleSteps()
        {
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);

            // a slider, so that there is a velocity row to place as well as a volume one.
            AddStep("lay down a slider", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Slider
                {
                    StartTime = 500,
                    Position = new Vector2(200, 200),
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
                });
            });

            AddStep("go to it", () => EditorClock.Seek(500));
            AddUntilStep("it is on the timeline", () => objects().Any());
        }

        [TearDownSteps]
        public void TearDownStyleSteps()
        {
            // a test that fails mid-drag never reaches its own release, and the button would stay down into the next one.
            AddStep("let go of the mouse", () => InputManager.ReleaseButton(MouseButton.Left));

            AddStep("back to the stock layout", () => config.GetBindable<EditorTimelineStyle>(OsuSetting.EditorTimelineStyle).SetDefault());
            AddStep("back to the stock rows", () => config.GetBindable<bool>(OsuSetting.EditorHideTimelinePoints).SetDefault());
            AddStep("let the timeline be dragged again", () => config.GetBindable<bool>(OsuSetting.EditorTimelineDragToSeek).SetDefault());
        }

        /// <summary>
        /// What "resting on the bottom edge" means: the objects and the ticks, which are drawn from that edge, end at
        /// the same place.
        /// </summary>
        [Test]
        public void TestObjectsRestOnTheBottomEdgeInStable()
        {
            float restingInLazer = 0;

            AddStep("use the lazer layout", () => setStyle(EditorTimelineStyle.Lazer));
            AddUntilStep("read where the object ends", () =>
            {
                restingInLazer = objectBottom();
                return restingInLazer > 0;
            });

            AddAssert("it is clear of the bottom edge", () => objectBottom(), () => Is.LessThan(bottomTickEdge() - 1));

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("the object moved down", () => objectBottom(), () => Is.GreaterThan(restingInLazer));
            AddAssert("it is resting on the bottom edge", () => objectBottom(), () => Is.EqualTo(bottomTickEdge()).Within(1));
        }

        /// <summary>
        /// There is nothing under an object resting on the bottom edge, so the row that sat there has to move above it.
        /// </summary>
        [Test]
        public void TestBothRowsSitAboveTheObjectInStable()
        {
            AddStep("use the lazer layout", () => setStyle(EditorTimelineStyle.Lazer));
            AddUntilStep("volume is under the object", () => volumeTop(), () => Is.GreaterThanOrEqualTo(objectBottom() - 1));

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("volume moved over the object", () => volumeTop(), () => Is.LessThan(objectTop()));
            AddAssert("velocity sits above volume", () => velocityBottom(), () => Is.LessThanOrEqualTo(volumeTop() + 1));
        }

        /// <summary>
        /// Ticks are short marks along the bottom edge in stable's layout rather than lines across the whole height.
        /// </summary>
        [Test]
        public void TestTicksAreKeptToTheBottomEdgeInStable()
        {
            float acrossTheWholeHeight = 0;

            AddStep("use the lazer layout", () => setStyle(EditorTimelineStyle.Lazer));
            AddUntilStep("read how tall the ticks are", () =>
            {
                acrossTheWholeHeight = tallestTick();
                return acrossTheWholeHeight > 0;
            });

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("they are shorter", () => tallestTick(), () => Is.LessThan(acrossTheWholeHeight));
        }

        /// <summary>
        /// Hiding the rows makes the objects bigger, and in stable's layout they still rest on the bottom edge.
        /// </summary>
        [Test]
        public void TestTheObjectsStayOnTheBottomEdgeWhenTheRowsAreHidden()
        {
            float stockHeight = 0;

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("read how tall an object is", () =>
            {
                stockHeight = objectBottom() - objectTop();
                return stockHeight > 0;
            });

            AddStep("hide the rows", () => config.SetValue(OsuSetting.EditorHideTimelinePoints, true));

            AddUntilStep("the object grew", () => objectBottom() - objectTop(), () => Is.GreaterThan(stockHeight));
            AddAssert("it is still on the bottom edge", () => objectBottom(), () => Is.EqualTo(bottomTickEdge()).Within(1));
        }

        /// <summary>
        /// Only the objects' own band takes a drag as a selection, so a drag begun anywhere else on the timeline moves
        /// the playhead. In the stable layout that is most of the height, and it is easy to catch while reaching for a
        /// selection.
        /// </summary>
        [Test]
        public void TestTheTimelineCanBeStoppedFromMovingOnADrag()
        {
            double startedAt = 0;

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddStep("go to the middle of the song", () => EditorClock.Seek(2000));

            AddStep("remember where we are", () => startedAt = EditorClock.CurrentTime);
            AddStep("drag over the empty part", () => dragAcrossTheTimelineAboveTheObjects());
            AddUntilStep("the playhead moved", () => EditorClock.CurrentTime, () => Is.Not.EqualTo(startedAt));

            AddStep("stop the timeline being dragged", () => config.SetValue(OsuSetting.EditorTimelineDragToSeek, false));

            AddStep("go back", () => EditorClock.Seek(2000));
            AddStep("remember where we are", () => startedAt = EditorClock.CurrentTime);
            AddStep("drag over the empty part again", () => dragAcrossTheTimelineAboveTheObjects());
            AddAssert("the playhead stayed put", () => EditorClock.CurrentTime, () => Is.EqualTo(startedAt));
        }

        /// <summary>
        /// The point of turning the drag off is to be able to reach for a selection up there instead, so the drag has
        /// to become one rather than simply doing nothing.
        /// </summary>
        [Test]
        public void TestTheEmptyPartSelectsOnceTheTimelineNoLongerMoves()
        {
            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddStep("stop the timeline being dragged", () => config.SetValue(OsuSetting.EditorTimelineDragToSeek, false));

            AddStep("go to the object", () => EditorClock.Seek(500));
            AddStep("deselect everything", () => EditorBeatmap.SelectedHitObjects.Clear());

            // in steps of their own, so that frames pass in between: the selection is worked out while the box is on
            // screen rather than when the button comes back up.
            AddStep("press above the objects", () =>
            {
                var area = timeline().ScreenSpaceDrawQuad;
                InputManager.MoveMouseTo(new Vector2(area.Centre.X - area.Width * 0.05f, area.TopLeft.Y + area.Height * 0.2f));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag across the object", () =>
            {
                var area = timeline().ScreenSpaceDrawQuad;
                InputManager.MoveMouseTo(new Vector2(area.Centre.X + area.Width * 0.05f, area.TopLeft.Y + area.Height * 0.2f));
            });

            AddUntilStep("the object came out selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(1));

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        /// <summary>
        /// What separates the two bottom-edge layouts: stable grows its ticks in from the bottom alone, the way stable
        /// itself does, and this fork's grows one in from the top as well.
        /// </summary>
        [Test]
        public void TestOnlyTheForksLayoutGrowsTicksFromBothEdges()
        {
            AddStep("use the lazer layout", () => setStyle(EditorTimelineStyle.Lazer));
            AddUntilStep("nothing is drawn from an edge", () => ticksFromAnEdge(), () => Is.Zero);

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("they come in from the bottom alone", () => ticksFromAnEdge(), () => Is.EqualTo(1));

            AddStep("use this fork's layout", () => setStyle(EditorTimelineStyle.LazerKai));
            AddUntilStep("they come in from both", () => ticksFromAnEdge(), () => Is.EqualTo(2));

            AddAssert("and are the same height either way", () =>
            {
                var heights = visibleTicks().GroupBy(t => t.Anchor).Select(g => g.Max(t => t.ScreenSpaceDrawQuad.Height)).ToArray();

                return heights.Length == 2 && Precision.AlmostEquals(heights[0], heights[1], 0.5f);
            });
        }

        /// <summary>
        /// This fork's layout is stable's in every other respect, so the objects rest on the bottom edge there too.
        /// </summary>
        [Test]
        public void TestTheForksLayoutRestsOnTheBottomEdgeAsWell()
        {
            AddStep("use this fork's layout", () => setStyle(EditorTimelineStyle.LazerKai));
            AddUntilStep("the object is on the bottom edge", () => objectBottom(), () => Is.EqualTo(bottomTickEdge()).Within(1));
            AddAssert("volume sits over the object", () => volumeTop(), () => Is.LessThan(objectTop()));
        }

        /// <summary>
        /// With a drag no longer moving the playhead, a selection can be begun anywhere on the timeline, but the
        /// strip above the objects was still drawn as though there were nothing to do there.
        /// </summary>
        [Test]
        public void TestTheStripAboveTheObjectsLightsUpOnceItTakesSelections()
        {
            AddStep("let a drag move the playhead", () => config.SetValue(OsuSetting.EditorTimelineDragToSeek, true));

            AddStep("point above the objects", pointAboveTheObjects);
            AddWaitStep("let it light up if it is going to", 5);
            AddAssert("it stays dark", theStripIsLit, () => Is.False);

            AddStep("stop a drag moving the playhead", () => config.SetValue(OsuSetting.EditorTimelineDragToSeek, false));

            AddStep("look away", () => InputManager.MoveMouseTo(Editor));
            AddStep("point above the objects again", pointAboveTheObjects);
            AddUntilStep("it lights up", theStripIsLit, () => Is.True);
        }

        /// <summary>
        /// Read off the colour it is drawn in: what is being checked is that the strip answers the pointer, not that
        /// it was sized some particular way.
        /// </summary>
        private bool theStripIsLit()
            => Editor.ChildrenOfType<TimelineBlueprintContainer.SelectableAreaBackground>().Single().Colour.TopLeft.SRGB.R > 0;

        /// <summary>
        /// Well clear of the objects' own band, which takes the middle 60% of the timeline. A tenth of the way down
        /// is inside the strip a drag would otherwise have moved the playhead from.
        /// </summary>
        private void pointAboveTheObjects()
        {
            var area = timeline().ScreenSpaceDrawQuad;

            InputManager.MoveMouseTo(new Vector2(area.Centre.X, area.TopLeft.Y + area.Height * 0.1f));
        }

        /// <summary>How many of the timeline's edges the ticks are drawn from.</summary>
        private int ticksFromAnEdge() =>
            visibleTicks().Select(t => t.Anchor).Distinct().Count(a => a == Anchor.TopLeft || a == Anchor.BottomLeft);

        /// <summary>
        /// A drag starting well above the objects, where a mapper reaching for a selection would begin one.
        /// </summary>
        private void dragAcrossTheTimelineAboveTheObjects()
        {
            var area = timeline().ScreenSpaceDrawQuad;
            float aboveTheObjects = area.TopLeft.Y + area.Height * 0.2f;

            InputManager.MoveMouseTo(new Vector2(area.Centre.X, aboveTheObjects));
            InputManager.PressButton(MouseButton.Left);
            InputManager.MoveMouseTo(new Vector2(area.Centre.X - area.Width * 0.2f, aboveTheObjects));
            InputManager.ReleaseButton(MouseButton.Left);
        }

        /// <summary>
        /// A spinner is nobody's combo colour, and stable's timeline leaves it out of them. So does this fork's
        /// layout: only upstream's own layout gives a spinner a combo colour.
        /// </summary>
        [Test]
        public void TestOnlyUpstreamsLayoutColoursASpinner()
        {
            AddStep("lay down a spinner", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Spinner { StartTime = 1000, Duration = 1000 });
            });

            AddStep("use the lazer layout", () => setStyle(EditorTimelineStyle.Lazer));
            AddUntilStep("it takes a combo colour", () => spinnerIsGrey(), () => Is.False);

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("it is grey", () => spinnerIsGrey(), () => Is.True);

            AddStep("use the Lazer-Kai layout", () => setStyle(EditorTimelineStyle.LazerKai));
            AddUntilStep("it is grey there too", () => spinnerIsGrey(), () => Is.True);
        }

        /// <summary>
        /// The left of the object's bar: a spinner runs for a while, so the bar is a gradient away from the colour
        /// it was given, and that colour is the end it starts at.
        /// </summary>
        private bool spinnerIsGrey()
        {
            var circle = objects().Single().ChildrenOfType<TimelineHitObjectBlueprint.ExtendableCircle>().First();

            return Precision.AlmostEquals(circle.Colour.TopLeft.SRGB.R, OsuColour.Gray(0.6f).R, 0.01f)
                   && Precision.AlmostEquals(circle.Colour.TopLeft.SRGB.G, OsuColour.Gray(0.6f).G, 0.01f)
                   && Precision.AlmostEquals(circle.Colour.TopLeft.SRGB.B, OsuColour.Gray(0.6f).B, 0.01f);
        }

        private Timeline timeline() => Editor.ChildrenOfType<Timeline>().Single();

        private void setStyle(EditorTimelineStyle style) => config.SetValue(OsuSetting.EditorTimelineStyle, style);

        private IEnumerable<TimelineHitObjectBlueprint> objects() =>
            Editor.ChildrenOfType<TimelineHitObjectBlueprint>();

        private float objectTop() => objects().Single().ScreenSpaceDrawQuad.TopLeft.Y;
        private float objectBottom() => objects().Single().ScreenSpaceDrawQuad.BottomLeft.Y;

        private float volumeTop() => Editor.ChildrenOfType<SamplePointPiece>().First().ScreenSpaceDrawQuad.TopLeft.Y;
        private float volumeBottom() => Editor.ChildrenOfType<SamplePointPiece>().First().ScreenSpaceDrawQuad.BottomLeft.Y;

        private float velocityBottom() => Editor.ChildrenOfType<DifficultyPointPiece>().Single().ScreenSpaceDrawQuad.BottomLeft.Y;

        /// <summary>The lowest edge any tick reaches, which is the edge they are drawn from.</summary>
        private float bottomTickEdge() => visibleTicks().Max(t => t.ScreenSpaceDrawQuad.BottomLeft.Y);

        /// <summary>
        /// The mark at the start of a bar reaches further than the beats inside it, so the bars can be counted
        /// without reading the colours. Only in this fork's layout: stable's is stable's.
        /// </summary>
        [Test]
        public void TestOnlyThisForksLayoutStretchesTheBarTick()
        {
            float inStable = 0;

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("read how long the bar tick is", () =>
            {
                inStable = tallestTick();
                return inStable > 0;
            });

            AddStep("use the Lazer-Kai layout", () => setStyle(EditorTimelineStyle.LazerKai));
            AddUntilStep("it reaches further", () => tallestTick(), () => Is.GreaterThan(inStable));

            AddStep("back to the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("and stable's is unchanged", () => tallestTick(), () => Is.EqualTo(inStable).Within(0.5f));
        }

        /// <summary>
        /// A mark reaching a tenth of the height reads as a mark; at the width upstream draws its full-height lines
        /// at, it starts to read as an object instead. The layouts that keep the marks to the edges draw them
        /// thinner for it.
        /// </summary>
        [Test]
        public void TestTheEdgeLayoutsDrawThinnerTicks()
        {
            float acrossTheWholeHeight = 0;

            AddStep("use the lazer layout", () => setStyle(EditorTimelineStyle.Lazer));
            AddUntilStep("read how wide a tick is", () =>
            {
                acrossTheWholeHeight = widestTick();
                return acrossTheWholeHeight > 0;
            });

            AddStep("use the stable layout", () => setStyle(EditorTimelineStyle.Stable));
            AddUntilStep("they are thinner", () => widestTick(), () => Is.LessThan(acrossTheWholeHeight));

            AddStep("use the Lazer-Kai layout", () => setStyle(EditorTimelineStyle.LazerKai));
            AddUntilStep("thinner there too", () => widestTick(), () => Is.LessThan(acrossTheWholeHeight));

            AddStep("back to the lazer layout", () => setStyle(EditorTimelineStyle.Lazer));
            AddUntilStep("upstream's are as they were", () => widestTick(), () => Is.EqualTo(acrossTheWholeHeight).Within(0.5f));
        }

        private float tallestTick() => visibleTicks().Max(t => t.ScreenSpaceDrawQuad.Height);

        private float widestTick() => visibleTicks().Max(t => t.ScreenSpaceDrawQuad.Width);

        private IEnumerable<PointVisualisation> visibleTicks() =>
            Editor.ChildrenOfType<TimelineTickDisplay>().Single().ChildrenOfType<PointVisualisation>().Where(t => t.Alpha > 0);
    }
}
