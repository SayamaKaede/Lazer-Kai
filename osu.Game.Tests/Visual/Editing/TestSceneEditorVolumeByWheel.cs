// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Alt and the wheel change the volume in the editor, as they do on every other screen (2026-10-11).
    /// </summary>
    /// <remarks>
    /// Elsewhere a wheel nothing took changes the volume, and scrolling lists let go of it while alt is held. The editor
    /// seeks with the wheel, so it had no such thing, and alt and the wheel did nothing outside the timeline.
    /// </remarks>
    public partial class TestSceneEditorVolumeByWheel : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Cached]
        private readonly VolumeOverlay volume = new VolumeOverlay();

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; } = null!;

        private double masterVolume => frameworkConfig.Get<double>(FrameworkSetting.VolumeUniversal);

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("add the volume overlay", () =>
            {
                if (volume.Parent == null)
                    Add(volume);
            });
            AddStep("volume to full", () => frameworkConfig.SetValue(FrameworkSetting.VolumeUniversal, 1d));
            AddUntilStep("the overlay is out of sight", () => volume.State.Value, () => Is.EqualTo(Framework.Graphics.Containers.Visibility.Hidden));
        }

        public override void TearDownSteps()
        {
            AddStep("volume back to full", () => frameworkConfig.SetValue(FrameworkSetting.VolumeUniversal, 1d));
            base.TearDownSteps();
        }

        [Test]
        public void TestAltAndTheWheelOverTheComposerChangeTheVolume()
        {
            double time = 0;

            AddStep("move over the composer", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<ComposeBlueprintContainer>().First()));
            AddStep("note the time", () => time = EditorClock.CurrentTime);

            altScrollDown();

            AddAssert("the volume went down", () => masterVolume, () => Is.LessThan(1));
            AddAssert("nothing was sought", () => EditorClock.CurrentTime, () => Is.EqualTo(time));
        }

        /// <summary>
        /// Over the timeline they zoom it, as they always have.
        /// </summary>
        [Test]
        public void TestAltAndTheWheelOverTheTimelineStillZoom()
        {
            float zoom = 0;

            AddStep("move over the timeline", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<Timeline>().First()));
            AddStep("note the zoom", () => zoom = Editor.ChildrenOfType<Timeline>().First().Zoom);

            altScrollDown();

            AddAssert("the zoom changed", () => Editor.ChildrenOfType<Timeline>().First().Zoom, () => Is.Not.EqualTo(zoom));
            AddAssert("the volume did not", () => masterVolume, () => Is.EqualTo(1));
        }

        [Test]
        public void TestTheWheelAloneStillSeeks()
        {
            double time = 0;

            AddStep("move over the composer", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<ComposeBlueprintContainer>().First()));
            AddStep("note the time", () => time = EditorClock.CurrentTime);
            AddRepeatStep("scroll down", () => InputManager.ScrollVerticalBy(-1), 3);

            AddUntilStep("the time moved on", () => EditorClock.CurrentTime, () => Is.GreaterThan(time));
            AddAssert("the volume did not change", () => masterVolume, () => Is.EqualTo(1));
        }

        // the first notch only brings the volume overlay out.
        private void altScrollDown() => AddRepeatStep("alt and the wheel, down", () =>
        {
            InputManager.PressKey(Key.AltLeft);
            InputManager.ScrollVerticalBy(-1);
            InputManager.ReleaseKey(Key.AltLeft);
        }, 5);
    }
}
