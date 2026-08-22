// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using System.Linq;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Components.TernaryButtons
{
    public partial class DrawableTernaryButton : OsuButton, IHasTooltip, IHasCurrentValue<TernaryState>
    {
        public Bindable<TernaryState> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly BindableWithCurrent<TernaryState> current = new BindableWithCurrent<TernaryState>();

        public required LocalisableString Description
        {
            get => Text;
            set => Text = value;
        }

        public LocalisableString TooltipText { get; set; }

        /// <summary>
        /// A function which creates a drawable icon to represent this item. If null, a sane default should be used.
        /// </summary>
        public Func<Drawable>? CreateIcon { get; init; }

        private Color4 defaultBackgroundColour;
        private Color4 defaultIconColour;
        private Color4 selectedBackgroundColour;
        private Color4 selectedIconColour;

        public Drawable Icon { get; private set; } = null!;
        protected HotkeyDisplay HotkeyDisplay { get; private set; } = null!;
        public Hotkey? Hotkey { get; init; }

        public DrawableTernaryButton(HoverSampleSet? hoverSampleSet = HoverSampleSet.Button)
            : base(hoverSampleSet)
        {
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            AddInternal(new EditorButtonSquareness(this, OsuSetting.EditorToggleButtonSquareness));


            defaultBackgroundColour = colourProvider.Background3;
            selectedBackgroundColour = colourProvider.Background1;

            defaultIconColour = defaultBackgroundColour.Darken(0.5f);
            selectedIconColour = selectedBackgroundColour.Lighten(0.5f);

            Add(Icon = (CreateIcon?.Invoke() ?? new Circle()).With(b =>
            {
                b.Blending = BlendingParameters.Additive;
                b.Anchor = Anchor.CentreLeft;
                b.Origin = Anchor.CentreLeft;
                b.Size = new Vector2(20);
                b.X = 10;
            }));

            if (Hotkey != null)
            {
                SpriteText.Origin = Anchor.BottomLeft;
                SpriteText.Y = -1;

                Add(HotkeyDisplay = new HotkeyDisplay
                {
                    Hotkey = Hotkey.Value,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.TopLeft,
                    X = 40,
                    Y = 1,
                });
            }
        }

        /// <summary>
        /// Claims drags starting on the button so the toolbox behind it does not scroll instead.
        /// </summary>
        /// <remarks>
        /// A press that wanders by a pixel was being read as a drag of the scrolling toolbox, and the click never
        /// arrived. The toolbox still scrolls from the space around the buttons, and from the wheel.
        /// </remarks>
        private bool draggedSincePress;

        protected override bool OnDragStart(DragStartEvent e)
        {
            draggedSincePress = true;
            return true;
        }

        /// <summary>
        /// Treats a press and release within the button as a press, however much the mouse wandered between them.
        /// </summary>
        /// <remarks>
        /// The framework drops a click once the mouse moves further than its drag distance. That distance is small,
        /// and smaller still when the editor's drag threshold is removed for nudging objects, so an ordinary press
        /// on a toolbox button was landing nowhere.
        /// </remarks>
        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (draggedSincePress && e.Button == MouseButton.Left && ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
            {
                // the sounds are a sibling listening for the same click, so they miss it for the same reason.
                this.ChildrenOfType<HoverClickSounds>().FirstOrDefault()?.PlayClickSample();
                TriggerClick();
            }

            draggedSincePress = false;

            base.OnMouseUp(e);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            current.BindValueChanged(_ => updateSelectionState(), true);

            Action = onAction;
        }

        private void onAction()
        {
            if (!Enabled.Value)
                return;

            Toggle();
        }

        /// <summary>
        /// Briefly highlights this button, reusing the hover treatment so the flash reads the same way.
        /// Used to show which hit sounds are playing as the editor plays through the beatmap.
        /// </summary>
        public void Flash()
        {
            if (IsHovered)
                return;

            Hover.FadeTo(0.2f, 40, Easing.OutQuint)
                 .Then()
                 .FadeOut(400, Easing.OutQuint);
        }

        public void Toggle()
        {
            switch (Current.Value)
            {
                case TernaryState.False:
                case TernaryState.Indeterminate:
                    Current.Value = TernaryState.True;
                    break;

                case TernaryState.True:
                    Current.Value = TernaryState.False;
                    break;
            }
        }

        private void updateSelectionState()
        {
            if (!IsLoaded)
                return;

            switch (Current.Value)
            {
                case TernaryState.Indeterminate:
                    Icon.Colour = selectedIconColour.Darken(0.5f);
                    BackgroundColour = selectedBackgroundColour.Darken(0.5f);
                    break;

                case TernaryState.False:
                    Icon.Colour = defaultIconColour;
                    BackgroundColour = defaultBackgroundColour;
                    break;

                case TernaryState.True:
                    Icon.Colour = selectedIconColour;
                    BackgroundColour = selectedBackgroundColour;
                    break;
            }
        }

        protected override SpriteText CreateText() => new OsuSpriteText
        {
            Depth = -1,
            Origin = Anchor.CentreLeft,
            Anchor = Anchor.CentreLeft,
            X = 40f
        };
    }

    public partial class DrawableTernaryButton<TAction> : DrawableTernaryButton, IKeyBindingHandler<TAction>
        where TAction : struct, Enum
    {
        public new TAction? Action { get; init; }

        public DrawableTernaryButton(HoverSampleSet? hoverSampleSet = HoverSampleSet.Button)
            : base(hoverSampleSet)
        {
        }

        public bool OnPressed(KeyBindingPressEvent<TAction> e)
        {
            if (e.Repeat || !Nullable.Equals(Action, e.Action))
                return false;

            TriggerClick();
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<TAction> e)
        {
        }
    }
}
