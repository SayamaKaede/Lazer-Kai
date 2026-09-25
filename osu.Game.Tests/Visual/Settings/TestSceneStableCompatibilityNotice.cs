// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Settings.Sections.LazerKai;

namespace osu.Game.Tests.Visual.Settings
{
    /// <summary>
    /// Turning stable compatibility on says what to know about it, once a run.
    /// </summary>
    public partial class TestSceneStableCompatibilityNotice : OsuTestScene
    {
        [Cached(typeof(IDialogOverlay))]
        private readonly DialogOverlay dialogOverlay = new DialogOverlay();

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private SessionStatics sessionStatics { get; set; } = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("off, and not shown this run", () =>
            {
                config.SetValue(OsuSetting.StableCompatibility, false);
                sessionStatics.SetValue(Static.StableCompatibilityNoticeShown, false);
            });

            AddStep("create the subsection", () => content.Child = new ExportSettings
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 400,
            });
        }

        private readonly Container content;

        public TestSceneStableCompatibilityNotice()
        {
            // the overlay lives as long as the scene: it is what the subsection was given, and replacing it would leave
            // the next one holding a disposed one.
            Children = new Drawable[]
            {
                content = new Container { RelativeSizeAxes = Axes.Both },
                dialogOverlay,
            };
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("back to default", () => config.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
        }

        [Test]
        public void TestTurningItOnSaysWhatToKnow()
        {
            AddStep("turn it on", () => config.SetValue(OsuSetting.StableCompatibility, true));
            AddUntilStep("the notes are shown", () => dialogOverlay.CurrentDialog, () => Is.InstanceOf<StableCompatibilityNoticeDialog>());

            AddStep("OK", () => dialogOverlay.CurrentDialog!.PerformAction<PopupDialogOkButton>());
            AddUntilStep("gone", () => dialogOverlay.CurrentDialog, () => Is.Null);
            AddAssert("still on", () => config.Get<bool>(OsuSetting.StableCompatibility), () => Is.True);
        }

        /// <summary>
        /// Once a run: again every time would be in the way of somebody trying it out. The next run says it again.
        /// </summary>
        [Test]
        public void TestTheNotesAreShownOnceARun()
        {
            AddStep("turn it on", () => config.SetValue(OsuSetting.StableCompatibility, true));
            AddUntilStep("the notes are shown", () => dialogOverlay.CurrentDialog, () => Is.InstanceOf<StableCompatibilityNoticeDialog>());
            AddStep("OK", () => dialogOverlay.CurrentDialog!.PerformAction<PopupDialogOkButton>());
            AddUntilStep("gone", () => dialogOverlay.CurrentDialog, () => Is.Null);

            AddStep("off and on again", () =>
            {
                config.SetValue(OsuSetting.StableCompatibility, false);
                config.SetValue(OsuSetting.StableCompatibility, true);
            });
            AddWaitStep("give it time to show", 5);
            AddAssert("not again", () => dialogOverlay.CurrentDialog, () => Is.Null);
        }

        [Test]
        public void TestTurningItOffSaysNothing()
        {
            AddStep("on, with the notes already shown", () =>
            {
                sessionStatics.SetValue(Static.StableCompatibilityNoticeShown, true);
                config.SetValue(OsuSetting.StableCompatibility, true);
                sessionStatics.SetValue(Static.StableCompatibilityNoticeShown, false);
            });

            AddStep("turn it off", () => config.SetValue(OsuSetting.StableCompatibility, false));
            AddWaitStep("give it time to show", 5);
            AddAssert("nothing shown", () => dialogOverlay.CurrentDialog, () => Is.Null);
        }
    }
}
