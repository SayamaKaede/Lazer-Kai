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
    /// A slider's body takes its volume from the head rather than having one of its own, and a spinner's sound is edited
    /// at its end, where it plays.
    /// </summary>
    public partial class TestSceneSliderAndSpinnerSounds : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset, false);

        private Slider slider = null!;

        private void addSlider() => AddStep("add slider", () =>
        {
            EditorBeatmap.Clear();
            EditorBeatmap.Add(slider = new Slider
            {
                Position = new Vector2(256, 256),
                StartTime = 0,
                Path = new SliderPath(new[] { new PathControlPoint(Vector2.Zero), new PathControlPoint(new Vector2(250, 0)) }),
                Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 70) },
                NodeSamples =
                {
                    new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 70) },
                    new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 70) },
                }
            });
        });

        [Test]
        public void TestTheHeadsVolumeCarriesTheBody()
        {
            addSlider();
            clickNodePiece(0);
            setVolume(40);

            AddAssert("head is 40", () => slider.NodeSamples[0].Single().Volume, () => Is.EqualTo(40));
            AddAssert("body is 40", () => slider.Samples.Single().Volume, () => Is.EqualTo(40));
            AddAssert("tail is still 70", () => slider.NodeSamples[1].Single().Volume, () => Is.EqualTo(70));
        }

        [Test]
        public void TestTheTailsVolumeLeavesTheBody()
        {
            addSlider();
            clickNodePiece(1);
            setVolume(40);

            AddAssert("tail is 40", () => slider.NodeSamples[1].Single().Volume, () => Is.EqualTo(40));
            AddAssert("body is still 70", () => slider.Samples.Single().Volume, () => Is.EqualTo(70));
        }

        [Test]
        public void TestTheBodyHasNoVolume()
        {
            addSlider();
            clickBodyPiece();

            AddUntilStep("popover is open", () => popover != null);
            AddAssert("no volume in it", () => popover!.ChildrenOfType<SamplePointPiece.VolumeControl>().Any(), () => Is.False);
        }

        /// <summary>
        /// With more than one object selected, the popover sets everything they have, the head and body together.
        /// </summary>
        [Test]
        public void TestTheBodyStillSetsTheVolumeOfASelection()
        {
            addSlider();
            AddStep("add circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 2000, Position = new Vector2(100) }));
            AddStep("select both", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));
            clickBodyPiece();
            setVolume(40);

            AddAssert("head is 40", () => slider.NodeSamples[0].Single().Volume, () => Is.EqualTo(40));
            AddAssert("body is 40", () => slider.Samples.Single().Volume, () => Is.EqualTo(40));
        }

        [Test]
        public void TestASpinnersSoundSitsAtItsEnd()
        {
            AddStep("add spinner, and a circle where it ends", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Spinner { StartTime = 1000, EndTime = 3000, Position = new Vector2(256, 192) });
                EditorBeatmap.Add(new HitCircle { StartTime = 3000, Position = new Vector2(100) });
            });

            AddUntilStep("its sound is where the circle's is", () => pieceOf(EditorBeatmap.HitObjects[0]).ScreenSpaceDrawQuad.Centre.X,
                () => Is.EqualTo(pieceOf(EditorBeatmap.HitObjects[1]).ScreenSpaceDrawQuad.Centre.X).Within(1));
        }

        [Test]
        public void TestSeekingFindsASpinnersSoundAtItsEnd()
        {
            AddStep("add spinner", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Spinner { StartTime = 1000, EndTime = 3000, Position = new Vector2(256, 192) });
            });

            AddStep("seek to 0", () => EditorClock.Seek(0));
            seekSamplePoint(Key.Right);
            AddUntilStep("at its end", () => EditorClock.CurrentTimeAccurate, () => Is.EqualTo(3000).Within(1));
            AddUntilStep("popover is open", () => popover != null);

            AddStep("seek to 5000", () => EditorClock.Seek(5000));
            seekSamplePoint(Key.Left);
            AddUntilStep("at its end", () => EditorClock.CurrentTimeAccurate, () => Is.EqualTo(3000).Within(1));
        }

        private SamplePointPiece.SampleEditPopover? popover => this.ChildrenOfType<SamplePointPiece.SampleEditPopover>().SingleOrDefault(p => p.IsPresent);

        private SamplePointPiece pieceOf(HitObject hitObject)
            => this.ChildrenOfType<SamplePointPiece>().Single(p => p is not NodeSamplePointPiece && p.HitObject == hitObject);

        private void clickBodyPiece() => AddStep("click body piece", () =>
        {
            InputManager.MoveMouseTo(pieceOf(slider));
            InputManager.Click(MouseButton.Left);
        });

        private void clickNodePiece(int node) => AddStep($"click node {node} piece", () =>
        {
            InputManager.MoveMouseTo(this.ChildrenOfType<NodeSamplePointPiece>().Single(p => p.HitObject == slider && p.NodeIndex == node));
            InputManager.Click(MouseButton.Left);
        });

        private void setVolume(int volume)
        {
            AddUntilStep("popover is open", () => popover != null);
            AddStep($"set volume {volume}", () => popover!.ChildrenOfType<SamplePointPiece.VolumeControl>().Single().Current.Value = volume);
        }

        private void seekSamplePoint(Key direction) => AddStep($"seek sample point {direction}", () =>
        {
            InputManager.PressKey(Key.ControlLeft);
            InputManager.PressKey(Key.ShiftLeft);
            InputManager.Key(direction);
            InputManager.ReleaseKey(Key.ShiftLeft);
            InputManager.ReleaseKey(Key.ControlLeft);
        });
    }
}
