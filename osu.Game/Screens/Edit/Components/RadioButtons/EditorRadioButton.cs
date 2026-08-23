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

namespace osu.Game.Screens.Edit.Components.RadioButtons
{
    public partial class EditorRadioButton : OsuButton, IHasTooltip
    {
        /// <summary>
        /// Whether this <see cref="EditorRadioButton"/> is selected.
        /// Disable this bindable to disable the button.
        /// </summary>
        public readonly BindableBool Selected = new BindableBool();

        /// <summary>
        /// A function which creates a drawable icon to represent this item. If null, a sane default should be used.
        /// </summary>
        public readonly Func<Drawable?>? CreateIcon;

        public Hotkey? Hotkey { get; init; }

        private readonly Action? action;

        private Color4 defaultBackgroundColour;
        private Color4 defaultIconColour;
        private Color4 selectedBackgroundColour;
        private Color4 selectedIconColour;

        private Drawable icon = null!;

        public EditorRadioButton(LocalisableString label, Action? action, Func<Drawable?>? createIcon = null)
        {
            Text = label;
            CreateIcon = createIcon;
            this.action = action;
            Action = Select;

            RelativeSizeAxes = Axes.X;
        }

        /// <summary>
        /// Which setting decides this button's height. Radio buttons are used by more than one toolbox group, and
        /// the groups are sized apart from each other.
        /// </summary>
        public OsuSetting SquarenessSetting { get; init; } = OsuSetting.EditorToolButtonSquareness;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            AddInternal(new EditorButtonSquareness(this, SquarenessSetting));


            defaultBackgroundColour = colourProvider.Background3;
            selectedBackgroundColour = colourProvider.Background1;

            defaultIconColour = defaultBackgroundColour.Darken(0.5f);
            selectedIconColour = selectedBackgroundColour.Lighten(0.5f);

            Add(icon = (CreateIcon?.Invoke() ?? new Circle()).With(b =>
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

                Add(new HotkeyDisplay
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

            Selected.BindValueChanged(selected =>
            {
                updateSelectionState();
                if (selected.NewValue)
                    action?.Invoke();
            }, true);

            Selected.BindDisabledChanged(disabled => Enabled.Value = !disabled, true);
            updateSelectionState();
        }

        /// <summary>
        /// Selects this <see cref="EditorRadioButton"/>.
        /// </summary>
        public void Select() => Selected.Value = true;

        /// <summary>
        /// Deselects this <see cref="EditorRadioButton"/>.
        /// </summary>
        public void Deselect() => Selected.Value = false;

        private void updateSelectionState()
        {
            if (!IsLoaded)
                return;

            BackgroundColour = Selected.Value ? selectedBackgroundColour : defaultBackgroundColour;
            icon.Colour = Selected.Value ? selectedIconColour : defaultIconColour;
        }

        protected override SpriteText CreateText() => new OsuSpriteText
        {
            Depth = -1,
            Origin = Anchor.CentreLeft,
            Anchor = Anchor.CentreLeft,
            X = 40f
        };

        public LocalisableString TooltipText { get; set; }
    }
}
