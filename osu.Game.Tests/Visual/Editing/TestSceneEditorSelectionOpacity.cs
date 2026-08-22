// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The user sets how solid the box drawn around a selection is. Its own fixture, because the setting lives in the
    /// config and would otherwise carry from one test into the next.
    /// </summary>
    [TestFixture]
    public partial class TestSceneEditorSelectionOpacity : OsuTestScene
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Cached(typeof(SelectionRotationHandler))]
        private SelectionRotationHandler rotationHandler = new SelectionRotationHandler();

        [Cached(typeof(SelectionScaleHandler))]
        private SelectionScaleHandler scaleHandler = new SelectionScaleHandler();

        private SelectionBox selectionBox = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create selection box", () => Child = new Container
            {
                Size = new Vector2(200),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Child = selectionBox = new SelectionBox { RelativeSizeAxes = Axes.Both },
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("restore opacity", () => config.GetBindable<float>(OsuSetting.EditorSelectionOpacity).SetDefault());
        }

        [Test]
        public void TestOpacityFollowsSetting()
        {
            AddStep("set half opacity", () => config.SetValue(OsuSetting.EditorSelectionOpacity, 0.5f));
            AddUntilStep("box is half solid", () => Precision.AlmostEquals(selectionBox.Alpha, 0.5f));
        }

        /// <remarks>
        /// The selection changing used to fade the box straight to full, writing over whatever the user had chosen, so
        /// the setting appeared to work only while the settings panel was open on top of an existing selection.
        /// </remarks>
        [Test]
        public void TestOpacitySurvivesSelectionChanging()
        {
            AddStep("set half opacity", () => config.SetValue(OsuSetting.EditorSelectionOpacity, 0.5f));

            AddStep("deselect", () => selectionBox.Shown = false);
            AddUntilStep("box is gone", () => Precision.AlmostEquals(selectionBox.Alpha, 0));

            AddStep("select again", () => selectionBox.Shown = true);
            AddUntilStep("box is still half solid", () => Precision.AlmostEquals(selectionBox.Alpha, 0.5f));
        }
    }
}
