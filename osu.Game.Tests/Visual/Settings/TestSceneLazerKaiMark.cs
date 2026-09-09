// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Overlays.Settings.Sections.LazerKai;
using osuTK;

namespace osu.Game.Tests.Visual.Settings
{
    /// <summary>
    /// That the fork's mark finds both halves of itself, through the same lookup the settings groups use.
    /// </summary>
    /// <remarks>
    /// <see cref="NonVisual.LazerKaiResourcesTest"/> checks the files are embedded; this checks the name the mark
    /// asks for reaches them once the texture store is in the way, extensions and all. Neither half says anything
    /// when it is missing -- it simply draws nothing.
    /// </remarks>
    public partial class TestSceneLazerKaiMark : OsuTestScene
    {
        private LazerKaiMark mark = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("draw the mark", () => Child = mark = new LazerKaiMark
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
            });
        }

        [Test]
        public void TestBothHalvesAreFound()
        {
            AddUntilStep("the cog and the character are both there", () => mark.IsComplete);
        }
    }
}
