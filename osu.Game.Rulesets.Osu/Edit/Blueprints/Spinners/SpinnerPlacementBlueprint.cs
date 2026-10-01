// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners
{
    public partial class SpinnerPlacementBlueprint : HitObjectPlacementBlueprint
    {
        public new Spinner HitObject => (Spinner)base.HitObject;

        private readonly SpinnerPiece piece;

        private bool isPlacingEnd;

        [Resolved]
        private IBeatSnapProvider? beatSnapProvider { get; set; }

        private Bindable<bool> swapRightAndMiddleMouse { get; set; } = null!;

        /// <summary>
        /// The button which finishes the spinner, matching how a slider path is finished.
        /// </summary>
        /// <remarks>
        /// Deliberately tied to the physical right button rather than to <see cref="MouseButton.Right"/>: finishing a
        /// placement is muscle memory, so it stays on the same physical button even when the two are swapped. The swap
        /// happens at the input source, so that button arrives here as <see cref="MouseButton.Middle"/>.
        /// </remarks>
        private MouseButton finishButton => swapRightAndMiddleMouse.Value ? MouseButton.Middle : MouseButton.Right;

        public SpinnerPlacementBlueprint()
            : base(new Spinner { Position = OsuPlayfield.BASE_SIZE / 2 })
        {
            Child = piece = new SpinnerPiece { Alpha = 0.5f };
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            swapRightAndMiddleMouse = config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse);
        }

        protected override void Update()
        {
            base.Update();

            if (isPlacingEnd)
                updateEndTimeFromCurrent();

            piece.UpdateFrom(HitObject);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (isPlacingEnd)
            {
                // even if the mouse button isn't the correct button to terminate placement,
                // clicks must not be allowed to fall through to underlying drawables.
                // if they were allowed to fall through, they may trigger selection of some other object.
                // selection of some other object will cause the tool to revert to "select",
                // which will cause this placement to automatically commit,
                // which *may* also cause the removal of the object whose selection was attempted
                // (happens when the start time of this spinner is the same as the start time of clicked object),
                // and that last possibility *will* result in a hard crash.
                if (e.Button != finishButton)
                    return true;

                updateEndTimeFromCurrent();
                EndPlacement(true);
            }
            else
            {
                if (e.Button != MouseButton.Left)
                    return false;

                BeginPlacement(commitStart: true);
                piece.FadeTo(1f, 150, Easing.OutQuint);

                isPlacingEnd = true;
            }

            return true;
        }

        private void updateEndTimeFromCurrent()
        {
            HitObject.EndTime = beatSnapProvider == null
                ? Math.Max(HitObject.StartTime, EditorClock.CurrentTime)
                : Math.Max(HitObject.StartTime + beatSnapProvider.GetBeatLengthAtTime(HitObject.StartTime), beatSnapProvider.SnapTime(EditorClock.CurrentTime));
        }
    }
}
