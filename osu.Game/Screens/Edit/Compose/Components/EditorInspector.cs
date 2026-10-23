// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;

namespace osu.Game.Screens.Edit.Compose.Components
{
    public partial class EditorInspector : CompositeDrawable
    {
        protected OsuTextFlowContainer InspectorText = null!;

        [Resolved]
        protected EditorBeatmap EditorBeatmap { get; private set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            InternalChild = InspectorText = new OsuTextFlowContainer
            {
                // a strip takes its width from the text rather than the other way round: a line too long for the
                // space runs off the end, where the strip cuts it, instead of wrapping and pushing a line out of
                // sight. Which line a value is on is then fixed however long the values are.
                RelativeSizeAxes = Compact ? Axes.None : Axes.X,
                AutoSizeAxes = Compact ? Axes.Both : Axes.Y,
                // a strip is three lines in the height the row of ticks had, so the half-line between paragraphs is
                // more than it can spare.
                ParagraphSpacing = Compact ? 0 : 0.5f,
            };
        }

        /// <summary>
        /// Lays the same values out along a strip rather than down a panel: everything runs on as one line, and a
        /// heading is only written where it says which of two numbers is which.
        /// </summary>
        public bool Compact { get; init; }

        /// <summary>
        /// The size of the text, or null for the size the panel uses.
        /// </summary>
        public float? TextSize { get; init; }

        private float fontSize => TextSize ?? OsuFont.Style.Caption2.Size;

        /// <summary>
        /// Where a value goes when the inspector is laid out as a strip.
        /// </summary>
        protected enum CompactLine
        {
            /// <summary>Runs on after whatever came before it.</summary>
            SameLine,

            /// <summary>Starts a line of its own, so a long value cannot push the next one onto a line of its own.</summary>
            NewLine,

            /// <summary>Left out of the strip. The panel still has it.</summary>
            LeftOut,
        }

        private bool compactSkipping;
        private bool anyCompactText;

        /// <summary>
        /// Starts the inspector's text again from nothing.
        /// </summary>
        protected void BeginInspectorText()
        {
            InspectorText.Clear();
            compactSkipping = false;
            anyCompactText = false;
        }

        /// <param name="header">What the value below is, as the panel writes it.</param>
        /// <param name="compactName">
        /// The short form for a strip, where there is no room to name every value. Values whose heading is left out
        /// still run on in the order the panel puts them in.
        /// </param>
        /// <param name="line">Where the value belongs when this is a strip.</param>
        protected void AddHeader(string header, string? compactName = null, CompactLine line = CompactLine.SameLine)
        {
            if (Compact)
            {
                compactSkipping = line == CompactLine.LeftOut;

                if (compactSkipping)
                    return;

                if (line == CompactLine.NewLine && anyCompactText)
                    InspectorText.NewParagraph();

                if (compactName != null)
                    addRunOn($"{compactName} ", colourProvider.Content2, FontWeight.Regular);

                return;
            }

            InspectorText.AddParagraph($"{header}", s =>
            {
                s.Font = OsuFont.Style.Caption2;
                s.Colour = colourProvider.Content2;
            });
        }

        protected void AddValue(LocalisableString value)
        {
            if (Compact)
            {
                if (!compactSkipping)
                    addRunOn(LocalisableString.Interpolate($"{value} "), colourProvider.Content1, FontWeight.SemiBold);

                return;
            }

            InspectorText.NewLine();
            InspectorText.AddText(value, s =>
            {
                s.Font = OsuFont.Style.Caption2.With(weight: FontWeight.SemiBold);
                s.Colour = colourProvider.Content1;
            });
        }

        private void addRunOn(LocalisableString text, ColourInfo colour, FontWeight weight)
        {
            anyCompactText = true;

            InspectorText.AddText(text, s =>
            {
                s.Font = OsuFont.Style.Caption2.With(size: fontSize, weight: weight);
                s.Colour = colour;
            });
        }
    }
}
