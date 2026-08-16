// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Rulesets.Edit
{
    public partial class EditorToolboxGroup : SettingsToolboxGroup
    {
        /// <summary>
        /// Whether this group should be held at the top of its toolbox instead of scrolling with the rest.
        /// </summary>
        public BindableBool Pinned { get; } = new BindableBool();

        private IconButton pinButton = null!;

        public EditorToolboxGroup(string title)
            : base(title)
        {
            RelativeSizeAxes = Axes.X;
            Width = 1;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            HeaderContent.Add(pinButton = new IconButton
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.CentreRight,
                // sits immediately left of the expand button.
                Position = new Vector2(-40, 0),
                Icon = FontAwesome.Solid.Thumbtack,
                Scale = new Vector2(0.6f),
                TooltipText = "Keep this group pinned to the top",
                Action = () => Pinned.Toggle(),
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Pinned.BindValueChanged(pinned => pinButton.Alpha = pinned.NewValue ? 1 : 0.4f, true);
        }
    }
}
