// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input;
using osu.Game.Configuration;
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
        /// </remarks>
        private readonly Bindable<float> leftDragStartDistance = new Bindable<float>(10);

        internal OsuUserInputManager()
        {
        }

        // config is not necessarily available: this input manager is constructed as part of the game's root hierarchy,
        // and test scenes set it up without one.
        [BackgroundDependencyLoader(true)]
        private void load(OsuConfigManager? config)
        {
            config?.BindWith(OsuSetting.DragStartDistance, leftDragStartDistance);
        }

        protected override MouseButtonEventManager CreateButtonEventManagerFor(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Right:
                    return new RightMouseManager(button);

                case MouseButton.Left:
                    return new LeftMouseManager(button, leftDragStartDistance);
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
