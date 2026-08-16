// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
using osu.Game.Screens.Play;
using osuTK.Input;

namespace osu.Game.Input
{
    public partial class OsuUserInputManager : UserInputManager
    {
        protected override bool AllowRightClickFromLongTouch => PlayingState.Value != LocalUserPlayingState.Playing;

        public readonly IBindable<LocalUserPlayingState> PlayingState = new Bindable<LocalUserPlayingState>();

        /// <summary>
        /// How far the mouse must travel before a left-button drag begins.
        /// </summary>
        /// <remarks>
        /// The framework's default makes nudging an object (or a slider control point) by a few pixels in the editor
        /// impossible, since the drag never starts. This is a field initialiser rather than assigned in the constructor
        /// because <see cref="InputManager"/>'s constructor creates the button event managers before any constructor
        /// body here would run.
        ///
        /// Bound by <see cref="OsuGameBase"/> rather than injected here: this input manager is part of the game's root
        /// hierarchy and loads before the config manager is available to it.
        /// </remarks>
        public readonly Bindable<float> DragStartDistance = new Bindable<float>(10);

        /// <summary>
        /// Whether the right and middle mouse buttons trade places before anything sees them.
        /// </summary>
        public readonly Bindable<bool> SwapRightAndMiddleMouse = new Bindable<bool>();

        /// <summary>
        /// Whether the editor is the active screen. The swap only applies there, so it cannot interfere with play.
        /// </summary>
        public readonly BindableBool EditorActive = new BindableBool();

        internal OsuUserInputManager()
        {
        }

        protected override List<IInput> GetPendingInputs()
        {
            var inputs = base.GetPendingInputs();

            if (!SwapRightAndMiddleMouse.Value || !EditorActive.Value)
                return inputs;

            // swapped at the source rather than per-drawable, so everything downstream (context menus, quick delete,
            // scroll containers) sees the button the user wants without knowing about the setting.
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i] is MouseButtonInput buttonInput)
                    inputs[i] = new MouseButtonInput(buttonInput.Entries.Select(e => new ButtonInputEntry<MouseButton>(swap(e.Button), e.IsPressed)));
            }

            return inputs;
        }

        private static MouseButton swap(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Right:
                    return MouseButton.Middle;

                case MouseButton.Middle:
                    return MouseButton.Right;

                default:
                    return button;
            }
        }

        protected override MouseButtonEventManager CreateButtonEventManagerFor(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Right:
                    return new RightMouseManager(button);

                case MouseButton.Left:
                    return new LeftMouseManager(button, DragStartDistance);
            }

            return base.CreateButtonEventManagerFor(button);
        }

        private class LeftMouseManager : MouseButtonEventManager
        {
            private readonly Bindable<float> dragStartDistance;

            public LeftMouseManager(MouseButton button, Bindable<float> dragStartDistance)
                : base(button)
            {
                this.dragStartDistance = dragStartDistance;
            }

            public override bool EnableDrag => true;
            public override bool EnableClick => true;
            public override bool ChangeFocusOnClick => true;

            public override float ClickDragDistance => dragStartDistance.Value;
        }

        private class RightMouseManager : MouseButtonEventManager
        {
            public RightMouseManager(MouseButton button)
                : base(button)
            {
            }

            public override bool EnableDrag => true; // allow right-mouse dragging for absolute scroll in scroll containers.
            public override bool EnableClick => false;
            public override bool ChangeFocusOnClick => false;
        }
    }
}
