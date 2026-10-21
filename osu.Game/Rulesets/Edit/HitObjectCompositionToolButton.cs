// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.RadioButtons;

namespace osu.Game.Rulesets.Edit
{
    public abstract partial class HitObjectCompositionToolButton : EditorRadioButton
    {
        public CompositionTool Tool { get; }

        /// <summary>
        /// Whether this answers its shortcut key at the moment.
        /// </summary>
        /// <remarks>
        /// Separate from the button being enabled, which greys it out and stops it being clicked. The tools are
        /// reached by the number row, and while something else on screen has been given the number row the button is
        /// still there to be clicked -- it just is not what a digit means any more.
        /// </remarks>
        public readonly BindableBool ShortcutEnabled = new BindableBool(true);

        protected HitObjectCompositionToolButton(CompositionTool tool, Action<CompositionTool>? action)
            : base(tool.Name, () => action?.Invoke(tool), tool.CreateIcon)
        {
            Tool = tool;

            Selected.BindDisabledChanged(isDisabled =>
            {
                TooltipText = isDisabled ? "Add at least one timing point first!" : Tool.TooltipText;
            }, true);
        }
    }

    public partial class HitObjectCompositionToolButton<TAction> : HitObjectCompositionToolButton, IKeyBindingHandler<TAction>
        where TAction : struct, Enum
    {
        public new CompositionTool<TAction> Tool => (CompositionTool<TAction>)base.Tool;

        [Resolved(CanBeNull = true)]
        private EditorAsmr? asmr { get; set; }

        public HitObjectCompositionToolButton(CompositionTool<TAction> tool, Action<CompositionTool>? action)
            : base(tool, action)
        {
        }

        public bool OnPressed(KeyBindingPressEvent<TAction> e)
        {
            if (Nullable.Equals(e.Action, Tool.Action) && Enabled.Value && ShortcutEnabled.Value)
            {
                TriggerClick();
                // a key makes no click sound of its own; the ASMR mode wants one either way (2026-10-21).
                asmr?.PlayInterface(AsmrSound.UiClick, this);
                return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<TAction> e)
        {
        }
    }
}
