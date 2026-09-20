// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// How far apart a stream's circles are, which is set by dragging the end of the shape while the panel is open.
    /// </summary>
    /// <remarks>
    /// Every drag of an anchor put the shape's length back on the beat divisor, so the stretch the shape's own end
    /// belongs to could only be a whole number of ticks long, and its spacing stepped from one tick to the next
    /// rather than following the pointer. Snapping is what keeps spacing the same across a beatmap, so it stays,
    /// with a setting per stretch to turn it off.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneStreamSpacingSnap : TestSceneOsuEditor
    {
        private StreamConversionPanel panel => Editor.ChildrenOfType<StreamConversionPanel>().Single();

        private Slider drawn = null!;

        private PathControlPointPiece<Slider> lastAnchor => Editor.ChildrenOfType<PathControlPointPiece<Slider>>().Last();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [SetUpSteps]
        public void StartFromNothing()
        {
            AddStep("clear the beatmap", () => EditorBeatmap.Clear());
            AddStep("go somewhere quiet", () => EditorClock.Seek(2000));

            // the panel remembers what it was last told, and the settings outlive a test.
            AddStep("back to the stock answers", setTheStockAnswers);
        }

        [TearDownSteps]
        public void PutTheSettingsBack()
        {
            AddStep("release the button", () => InputManager.ReleaseButton(MouseButton.Left));
            AddStep("back to the stock answers", setTheStockAnswers);
        }

        private void setTheStockAnswers()
        {
            config.SetValue(OsuSetting.EditorStreamSnapSpacingWhole, true);
            config.SetValue(OsuSetting.EditorStreamSnapSpacingLastOfSeveral, false);
        }

        [Test]
        public void TestTheEndFollowsThePointerOnlyWithSnappingOff()
        {
            var snapped = new List<double>();
            var free = new List<double>();

            handAShapeOver();

            AddAssert("snapping is on to begin with", () => panel.SnapSpacing.Value, () => Is.True);
            dragTheEnd(snapped);
            // a tick apart at a time over the same drag, against the many lengths a free end passes through.
            AddAssert("the length steps between ticks", () => distinct(snapped), () => Is.LessThan(6));

            AddStep("turn snapping off", () => panel.SnapSpacing.Value = false);
            dragTheEnd(free);
            AddAssert("the length follows the pointer", () => distinct(free), () => Is.GreaterThan(9));
        }

        /// <summary>
        /// The setting belongs to the stretch, and a new one starts out as the last was left — including the next
        /// run, which is what makes it worth setting at all.
        /// </summary>
        [Test]
        public void TestTheNextRunStartsOutHowTheLastOneWasLeft()
        {
            handAShapeOver();
            AddStep("turn snapping off", () => panel.SnapSpacing.Value = false);
            AddStep("close it without taking it", () => panel.Hide());

            handAShapeOver();
            AddAssert("snapping is still off", () => panel.SnapSpacing.Value, () => Is.False);
        }

        /// <summary>
        /// A shape drawn in one piece is laid along the ticks; one being shaped into corners is being placed by hand,
        /// and its end follows the hand. The two are remembered apart, so shaping one does not answer for the other.
        /// </summary>
        [Test]
        public void TestTheTwoKindsOfShapeAreRememberedApart()
        {
            handAShapeOver();
            AddAssert("a shape in one piece snaps", () => panel.SnapSpacing.Value, () => Is.True);

            AddStep("turn snapping off", () => panel.SnapSpacing.Value = false);
            AddStep("close it without taking it", () => panel.Hide());

            handAShapeOver(withACorner: true);
            AddStep("look at the last stretch", () => panel.Selector.Current.Value = 1);
            AddAssert("a shape with a corner does not", () => panel.SnapSpacing.Value, () => Is.False);

            AddStep("turn snapping on", () => panel.SnapSpacing.Value = true);
            AddStep("close it without taking it", () => panel.Hide());

            handAShapeOver();
            AddAssert("the shape in one piece kept its own answer", () => panel.SnapSpacing.Value, () => Is.False);
        }

        private void handAShapeOver(bool withACorner = false)
        {
            AddStep("hand a shape over", () => panel.Open(drawn = shape(withACorner), fromTheBeatmap: false));
            AddUntilStep("its anchors are up", () => Editor.ChildrenOfType<PathControlPointPiece<Slider>>().Any());
        }

        /// <summary>
        /// Pulls the shape's last anchor out a little at a time, noting how long the shape is after every step.
        /// </summary>
        private void dragTheEnd(List<double> lengths)
        {
            Vector2 from = Vector2.Zero;

            AddStep("take hold of the last anchor", () =>
            {
                lengths.Clear();
                from = lastAnchor.ScreenSpaceDrawQuad.Centre;
                InputManager.MoveMouseTo(from);
                InputManager.PressButton(MouseButton.Left);
            });

            // from where it was taken hold of rather than from where it is now: a drag only starts once the pointer
            // has gone a little way from where the button went down.
            for (int step = 1; step <= 20; step++)
            {
                int pixels = step * 6;

                AddStep($"pull it out {pixels}px", () =>
                {
                    InputManager.MoveMouseTo(from + new Vector2(pixels, 0));
                    lengths.Add(drawn.Path.Distance);
                });
            }

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        private int distinct(IEnumerable<double> lengths) => lengths.Select(l => Math.Round(l, 2)).Distinct().Count();

        private Slider shape(bool withACorner = false)
        {
            var points = withACorner
                ? new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(100, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(100, 100)),
                }
                : new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(200, 0)),
                };

            var slider = new Slider
            {
                StartTime = EditorClock.CurrentTime,
                Position = new Vector2(150, 200),
                Path = new SliderPath(points),
            };

            // without its defaults the path has no velocity behind it, and a shape with no length in time is not one
            // anything can be laid along.
            slider.ApplyDefaults(EditorBeatmap.ControlPointInfo, EditorBeatmap.Difficulty);
            return slider;
        }

    }
}
