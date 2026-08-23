// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.Edit;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The editor's sidebars let a group be held at the top. Its own fixture, because the pinned groups live in the
    /// config and would otherwise carry from one test into the next.
    /// </summary>
    [TestFixture]
    public partial class TestSceneEditorToolboxPinning : OsuManualInputManagerTestScene
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private ExpandingToolboxContainer toolbox = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("start with nothing pinned", () => config.SetValue(OsuSetting.EditorPinnedToolboxGroups, string.Empty));
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("restore pinned groups", () => config.GetBindable<string>(OsuSetting.EditorPinnedToolboxGroups).SetDefault());
        }

        /// <remarks>
        /// One of the three sits inside a container of its own, the way the composer's velocity group does. That group
        /// was left out of the persisted state entirely, because only direct children were being looked at.
        /// </remarks>
        private void createToolbox() => AddStep("create toolbox", () => Child = toolbox = new ExpandingToolboxContainer(50, 200)
        {
            Children = new Drawable[]
            {
                new EditorToolboxGroup("first"),
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Child = new EditorToolboxGroup("wrapped"),
                },
                new EditorToolboxGroup("third"),
            }
        });

        private EditorToolboxGroup group(string title) => toolbox.ChildrenOfType<EditorToolboxGroup>().Single(g => g.Title == title);

        private float topOf(string title) => group(title).ScreenSpaceDrawQuad.TopLeft.Y;

        [Test]
        public void TestPinIsRemembered()
        {
            createToolbox();

            AddStep("pin wrapped group", () => group("wrapped").Pinned.Value = true);
            AddAssert("pin was written", () => config.Get<string>(OsuSetting.EditorPinnedToolboxGroups), () => Is.EqualTo("wrapped"));

            AddStep("pin first group", () => group("first").Pinned.Value = true);
            AddAssert("both were written", () => config.Get<string>(OsuSetting.EditorPinnedToolboxGroups), () => Is.EqualTo("first|wrapped"));

            AddStep("unpin wrapped group", () => group("wrapped").Pinned.Value = false);
            AddAssert("only one is left", () => config.Get<string>(OsuSetting.EditorPinnedToolboxGroups), () => Is.EqualTo("first"));
        }

        [Test]
        public void TestPinIsRestored()
        {
            AddStep("store a pin", () => config.SetValue(OsuSetting.EditorPinnedToolboxGroups, "wrapped"));

            createToolbox();

            AddAssert("group came back pinned", () => group("wrapped").Pinned.Value);
            AddAssert("it is at the top", () => topOf("wrapped"), () => Is.LessThan(topOf("first")));
        }

        /// <remarks>
        /// A flow container falls back to the order it first saw its children in, so a group taken out and put back
        /// used to land at the bottom of the sidebar rather than where it came from.
        /// </remarks>
        [Test]
        public void TestUnpinningReturnsGroupToItsPlace()
        {
            createToolbox();

            AddStep("pin first group", () => group("first").Pinned.Value = true);
            AddStep("unpin first group", () => group("first").Pinned.Value = false);

            AddAssert("it is back on top", () => topOf("first"), () => Is.LessThan(topOf("wrapped")));
            AddAssert("the rest did not move", () => topOf("wrapped"), () => Is.LessThan(topOf("third")));
        }

        /// <remarks>
        /// A group scrolled up behind the pinned ones has nothing on screen to say it is there, so it can stay lost
        /// indefinitely. Putting the scroll back every time the cursor arrives caps how long that can last.
        /// </remarks>
        [Test]
        public void TestScrollReturnsToTheTopWhenTheCursorComesBack()
        {
            OsuScrollContainer scroll = null!;

            AddStep("create a toolbox too short for its groups", () => Child = toolbox = new ExpandingToolboxContainer(50, 200)
            {
                RelativeSizeAxes = Axes.None,
                Size = new Vector2(200),
                Children = Enumerable.Range(0, 5)
                                     .Select(i => (Drawable)new EditorToolboxGroup($"group {i}") { Child = new Box { RelativeSizeAxes = Axes.X, Height = 100 } })
                                     .ToArray(),
            });

            AddStep("point away", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddStep("find the scroll", () => scroll = toolbox.ChildrenOfType<OsuScrollContainer>().Single());

            AddStep("scroll to the bottom", () => scroll.ScrollToEnd(false));
            AddUntilStep("it did scroll", () => scroll.Current, () => Is.GreaterThan(0));

            AddStep("point at the toolbox", () => InputManager.MoveMouseTo(toolbox));
            AddUntilStep("it went back to the top", () => scroll.Current, () => Is.EqualTo(0).Within(0.5));
        }

        [Test]
        public void TestPinnedGroupsKeepSidebarOrder()
        {
            createToolbox();

            AddStep("pin third group", () => group("third").Pinned.Value = true);
            AddStep("pin first group", () => group("first").Pinned.Value = true);

            AddAssert("sidebar order is kept", () => topOf("first"), () => Is.LessThan(topOf("third")));
        }

    }
}
