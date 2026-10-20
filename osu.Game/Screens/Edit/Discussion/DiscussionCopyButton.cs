// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osuTK;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// Copies a post's text as it reads in the panel, to be put into a translator (2026-10-20 request).
    /// </summary>
    /// <remarks>
    /// The images under the post are left out: their links are in the way of what was written, and they are not
    /// what is being translated.
    /// </remarks>
    public partial class DiscussionCopyButton : IconButton
    {
        private const double confirm_duration = 1500;

        public readonly string Text;

        [Resolved]
        private Clipboard clipboard { get; set; } = null!;

        private ScheduledDelegate? restore;

        public DiscussionCopyButton(string post)
        {
            Text = DiscussionImageLinks.Extract(post).Text.Trim();

            Anchor = Anchor.TopRight;
            Origin = Anchor.TopRight;
            Scale = new Vector2(0.5f);
            Icon = FontAwesome.Regular.Copy;
            TooltipText = DiscussionStrings.Copy;
            Action = copy;
        }

        private void copy()
        {
            clipboard.SetText(Text);

            // said back on the button itself, where the eye already is.
            Icon = FontAwesome.Solid.Check;
            TooltipText = DiscussionStrings.Copied;

            restore?.Cancel();
            restore = Scheduler.AddDelayed(() =>
            {
                Icon = FontAwesome.Regular.Copy;
                TooltipText = DiscussionStrings.Copy;
            }, confirm_duration);
        }
    }
}
