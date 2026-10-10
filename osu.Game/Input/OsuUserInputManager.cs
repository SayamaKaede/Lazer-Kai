// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        /// Whether a left-button drag begins as soon as the mouse moves at all.
        /// </summary>
        /// <remarks>
        /// The framework waits for the mouse to travel a set distance first, which makes nudging an object (or a
        /// slider control point) by a few pixels in the editor impossible, since the drag never starts.
        ///
        /// Applies only over the areas which ask for it by implementing <see cref="IFineDragArea"/> — the composer, the
        /// two timelines, and the slider bars a number is set with, wherever they are. Everything else, buttons
        /// included, keeps the normal threshold, so nothing else in the game has to work around a click that turned
        /// into a drag.
        ///
        /// This is a field initialiser rather than assigned in the constructor because <see cref="InputManager"/>'s
        /// constructor creates the button event managers before any constructor body here would run. It is bound by
        /// <see cref="OsuGameBase"/> rather than injected, since this input manager is part of the game's root
        /// hierarchy and loads before the config manager is available to it.
        /// </remarks>
        public readonly Bindable<bool> RemoveDragThreshold = new Bindable<bool>();

        /// <summary>
        /// Whether the right and middle mouse buttons trade places before anything sees them.
        /// </summary>
        public readonly Bindable<bool> SwapRightAndMiddleMouse = new Bindable<bool>();

        /// <summary>
        /// Whether the editor is the active screen. The button swap only applies there, so it cannot interfere with play.
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
                    return new LeftMouseManager(button, RemoveDragThreshold, () => PositionalInputQueue.OfType<IFineDragArea>().Any());
            }

            return base.CreateButtonEventManagerFor(button);
        }

        /// <summary>
        /// What the framework uses when nothing overrides it.
        /// </summary>
        public const float DEFAULT_DRAG_DISTANCE = 10;

        /// <summary>
        /// Small enough to feel immediate, but not zero: a click with an unsteady hand should still be a click.
        /// </summary>
        public const float MINIMAL_DRAG_DISTANCE = 1;

        /// <summary>
        /// How far the mouse must travel before a left-button drag begins.
        /// </summary>
        public static float ClickDragDistanceFor(bool removeDragThreshold, bool overFineDragArea)
            => removeDragThreshold && overFineDragArea ? MINIMAL_DRAG_DISTANCE : DEFAULT_DRAG_DISTANCE;

        private class LeftMouseManager : MouseButtonEventManager
        {
            private readonly Bindable<bool> removeDragThreshold;
            private readonly Func<bool> overFineDragArea;

            public LeftMouseManager(MouseButton button, Bindable<bool> removeDragThreshold, Func<bool> overFineDragArea)
                : base(button)
            {
                this.removeDragThreshold = removeDragThreshold;
                this.overFineDragArea = overFineDragArea;
            }

            public override bool EnableDrag => true;
            public override bool EnableClick => true;
            public override bool ChangeFocusOnClick => true;

            public override float ClickDragDistance => ClickDragDistanceFor(removeDragThreshold.Value, overFineDragArea());
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
