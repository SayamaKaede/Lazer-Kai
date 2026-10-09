// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Changelog
{
    /// <summary>
    /// osu!'s changelog header, without the update streams: the fork has the one.
    /// </summary>
    public partial class LazerKaiChangelogHeader : OverlayHeader
    {
        protected override Drawable CreateBackground() => new OverlayHeaderBackground(@"Headers/changelog");

        protected override OverlayTitle CreateTitle() => new LazerKaiChangelogTitle();

        private partial class LazerKaiChangelogTitle : OverlayTitle
        {
            public LazerKaiChangelogTitle()
            {
                Title = LazerKaiSettingsStrings.ChangelogTitle;
                Icon = OsuIcon.ChangelogB;
            }
        }
    }
}
