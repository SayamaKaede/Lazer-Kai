// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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

        /// <summary>
        /// Asked to swap places with the group above (-1) or below (+1) it. Set by the toolbox, which is the only
        /// thing that knows where this group sits among the others.
        /// </summary>
        public Action<int>? RequestReorder { get; set; }

        /// <summary>
        /// Whether there is a group to swap with in each direction.
        /// </summary>
        public BindableBool CanMoveUp { get; } = new BindableBool();

        public BindableBool CanMoveDown { get; } = new BindableBool();

        /// <summary>
        /// Identifies this group when persisting its pinned state.
        /// </summary>
        public string Title { get; }

        private IconButton pinButton = null!;
        private IconButton moveUpButton = null!;
        private IconButton moveDownButton = null!;

        // a mapper folds a group away to get the room back, and a sidebar the cursor has to cross to reach anything
        // else would keep handing it straight back.
        protected override bool PeekOnHover => false;

        // nothing else says the group is folded rather than empty, and a button that only appears once the cursor is
        // already there cannot be found by looking.
        protected override bool AlwaysShowExpandButton => true;

        public EditorToolboxGroup(string title)
            : base(title)
        {
            Title = title;
            RelativeSizeAxes = Axes.X;
            Width = 1;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            HeaderContent.Add(moveUpButton = new IconButton
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.CentreRight,
                // the two of them sit left of the pin, in the order they move things.
                Position = new Vector2(-78, 0),
                Icon = FontAwesome.Solid.ChevronUp,
                Scale = new Vector2(0.5f),
                TooltipText = "Move this group up",
                Action = () => RequestReorder?.Invoke(-1),
            });

            HeaderContent.Add(moveDownButton = new IconButton
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.CentreRight,
                Position = new Vector2(-59, 0),
                Icon = FontAwesome.Solid.ChevronDown,
                Scale = new Vector2(0.5f),
                TooltipText = "Move this group down",
                Action = () => RequestReorder?.Invoke(1),
            });

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

        private void updateMoveButtons()
        {
            const float fade_duration = 500;

            moveUpButton.FadeTo(GroupHovered.Value ? (CanMoveUp.Value ? 0.7f : 0.2f) : 0, fade_duration, Easing.OutQuint);
            moveDownButton.FadeTo(GroupHovered.Value ? (CanMoveDown.Value ? 0.7f : 0.2f) : 0, fade_duration, Easing.OutQuint);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Pinned.BindValueChanged(pinned => pinButton.Alpha = pinned.NewValue ? 1 : 0.4f, true);

            // reordering is a rare, deliberate thing, so the arrows stay out of the way until the cursor is on the
            // group. dimmed rather than removed at the ends of the list, so the header keeps its shape as groups move.
            CanMoveUp.BindValueChanged(_ => updateMoveButtons());
            CanMoveDown.BindValueChanged(_ => updateMoveButtons());
            GroupHovered.BindValueChanged(_ => updateMoveButtons(), true);
        }
    }
}
