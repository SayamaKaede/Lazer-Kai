// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
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
    /// Taking anchors out of a stream's shape while the panel is open on it.
    /// </summary>
    /// <remarks>
    /// The shape is not in the beatmap, so nothing selects it and the quick delete that works through a selection
    /// never reached its anchors: a shape could be drawn and then not tidied up. Two anchors stay whatever is asked:
    /// the first is where the shape starts, and a shape of two has nothing left to be once its end is gone.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneStreamAnchors : TestSceneOsuEditor
    {
        private StreamConversionPanel panel => Editor.ChildrenOfType<StreamConversionPanel>().Single();

        private Slider drawn = null!;

        private PathControlPointPiece<Slider>[] anchors => Editor.ChildrenOfType<PathControlPointPiece<Slider>>().ToArray();

        [SetUpSteps]
        public void StartFromNothing()
        {
            AddStep("clear the beatmap", () => EditorBeatmap.Clear());
            AddStep("go somewhere quiet", () => EditorClock.Seek(2000));

            AddStep("hand a shape of three over", () => panel.Open(drawn = shape(), fromTheBeatmap: false));
            AddUntilStep("its anchors are up", () => anchors.Length, () => Is.EqualTo(3));
        }

        [Test]
        public void TestTheAnchorUnderThePointerGoes()
        {
            deleteTheAnchor(1);
            AddAssert("it went", () => drawn.Path.ControlPoints.Count, () => Is.EqualTo(2));
        }

        [Test]
        public void TestTheFirstAnchorStays()
        {
            deleteTheAnchor(0);
            AddAssert("all three are there", () => drawn.Path.ControlPoints.Count, () => Is.EqualTo(3));
        }

        [Test]
        public void TestTheEndStaysOnceThereAreOnlyTwo()
        {
            deleteTheAnchor(1);
            AddUntilStep("two are left", () => anchors.Length, () => Is.EqualTo(2));

            deleteTheAnchor(1);
            AddAssert("the end is still there", () => drawn.Path.ControlPoints.Count, () => Is.EqualTo(2));
        }

        /// <summary>The wheel button, which is what the editor deletes with (and what a swapped right button sends).</summary>
        private void deleteTheAnchor(int index)
        {
            AddStep($"delete anchor {index}", () =>
            {
                InputManager.MoveMouseTo(anchors[index]);
                InputManager.Click(MouseButton.Middle);
            });
        }

        private Slider shape()
        {
            var slider = new Slider
            {
                StartTime = EditorClock.CurrentTime,
                Position = new Vector2(150, 200),
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(100, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(100, 100)),
                }),
            };

            slider.ApplyDefaults(EditorBeatmap.ControlPointInfo, EditorBeatmap.Difficulty);
            return slider;
        }
    }
}
