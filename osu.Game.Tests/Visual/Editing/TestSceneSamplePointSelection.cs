// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A change made through one of the timeline's sound points goes to every point that is selected with it: all of a
    /// selected object's, or those a drag box took in. Only what was changed is carried over.
    /// </summary>
    public partial class TestSceneSamplePointSelection : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset, false);

        private Slider slider = null!;

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("add a slider and a circle", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(slider = new Slider
                {
                    Position = new Vector2(100, 192),
                    StartTime = 1000,
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(300, 0) }),
                    Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT, volume: 70) },
                    NodeSamples =
                    {
                        new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT, volume: 70) },
                        new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT, volume: 70) },
                    }
                });
                EditorBeatmap.Add(new HitCircle { StartTime = 4000, Position = new Vector2(300) });
            });
            // once it is in: the node sounds given when it is made are not the ones it ends up with.
            AddStep("put a whistle on the tail", () =>
            {
                slider.NodeSamples[1].Add(new HitSampleInfo(HitSampleInfo.HIT_WHISTLE, HitSampleInfo.BANK_SOFT, volume: 70));
                EditorBeatmap.Update(slider);
            });
            AddStep("look at the slider", () => EditorClock.Seek(1000));
        }

        [Test]
        public void TestASelectedSlidersPointsChangeTogether()
        {
            AddAssert("the whistle is only on the tail to begin with", () => whistles(), () => Is.EqualTo(new[] { false, false, true }));
            AddStep("select the slider", () => EditorBeatmap.SelectedHitObjects.Add(slider));
            AddUntilStep("its points show as selected", () => pointsOf(slider).All(p => p.IsSelected));

            clickNode(0);
            setVolume(40);

            AddAssert("head, body and tail are 40", () => volumes(), () => Is.EqualTo(new[] { 40, 40, 40 }));
            AddAssert("each keeps its own sounds: the whistle only on the tail", () => whistles(), () => Is.EqualTo(new[] { false, false, true }));
        }

        /// <remarks>
        /// The head is the point left out: a change to it alone takes the body with it anyway, so leaving out the tail
        /// would not tell the box apart from the head alone.
        /// </remarks>
        [Test]
        public void TestADragBoxTakesOnlyThePointsInIt()
        {
            dragOverBodyAndTail();

            AddAssert("body and tail are selected, the head is not", () => new[] { nodeOf(0).IsSelected, pointOf(-1).IsSelected, nodeOf(1).IsSelected },
                () => Is.EqualTo(new[] { false, true, true }));

            clickNode(1);
            setVolume(40);

            AddAssert("body and tail are 40, the head still 70", () => volumes(), () => Is.EqualTo(new[] { 70, 40, 40 }));
        }

        [Test]
        public void TestAPointOutsideTheSelectionChangesAlone()
        {
            AddStep("select the circle", () => EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.Last()));

            clickNode(1);
            setVolume(40);

            AddAssert("only the tail is 40", () => volumes(), () => Is.EqualTo(new[] { 70, 70, 40 }));
        }

        [Test]
        public void TestSelectingAgainAfterADragTakesAllOfTheObject()
        {
            dragOverBodyAndTail();
            AddAssert("the head is not selected", () => nodeOf(0).IsSelected, () => Is.False);

            AddStep("select the slider again", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            AddUntilStep("all its points are selected", () => pointsOf(slider).All(p => p.IsSelected));
        }

        private IEnumerable<SamplePointPiece> pointsOf(HitObject hitObject) => this.ChildrenOfType<SamplePointPiece>().Where(p => p.HitObject == hitObject);

        private SamplePointPiece pointOf(int node) => node < 0
            ? pointsOf(slider).Single(p => p is not NodeSamplePointPiece)
            : nodeOf(node);

        private NodeSamplePointPiece nodeOf(int node) => pointsOf(slider).OfType<NodeSamplePointPiece>().Single(p => p.NodeIndex == node);

        private int[] volumes() => new[] { slider.NodeSamples[0].First().Volume, slider.Samples.First().Volume, slider.NodeSamples[1].First().Volume };

        private bool[] whistles() => new[] { slider.NodeSamples[0], slider.Samples, slider.NodeSamples[1] }.Select(s => s.Any(h => h.Name == HitSampleInfo.HIT_WHISTLE)).ToArray();

        /// <summary>
        /// Drags a box on the objects' row from just after the slider back to between its head and body points, which
        /// takes in the body and tail points but not the head's. From after it, so that the press is not on the slider.
        /// </summary>
        private void dragOverBodyAndTail()
        {
            AddStep("press just after the slider", () =>
            {
                var quad = this.ChildrenOfType<TimelineHitObjectBlueprint>().First(b => b.Item == slider).ScreenSpaceDrawQuad;
                // clear of the handle past the tail, which drags the slider's length rather than a box.
                InputManager.MoveMouseTo(new Vector2(quad.TopRight.X + 80, quad.Centre.Y));
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("drag back past the body, short of the head", () =>
            {
                var quad = this.ChildrenOfType<TimelineHitObjectBlueprint>().First(b => b.Item == slider).ScreenSpaceDrawQuad;
                InputManager.MoveMouseTo(new Vector2((nodeOf(0).ScreenSpaceDrawQuad.Centre.X + pointOf(-1).ScreenSpaceDrawQuad.Centre.X) / 2, quad.Centre.Y));
            });
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        private void clickNode(int node) => AddStep($"click node {node}", () =>
        {
            InputManager.MoveMouseTo(nodeOf(node));
            InputManager.Click(MouseButton.Left);
        });

        private void setVolume(int volume)
        {
            AddUntilStep("popover is open", () => this.ChildrenOfType<SamplePointPiece.SampleEditPopover>().Any(p => p.IsPresent));
            AddStep($"set volume {volume}", () => this.ChildrenOfType<SamplePointPiece.SampleEditPopover>().Single().ChildrenOfType<SamplePointPiece.VolumeControl>().Single().Current.Value = volume);
        }
    }
}
