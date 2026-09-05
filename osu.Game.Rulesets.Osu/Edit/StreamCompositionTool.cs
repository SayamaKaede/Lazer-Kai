// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Localisation.Osu;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Streams;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Places a run of circles along a shape drawn the way a slider is drawn.
    /// </summary>
    /// <remarks>
    /// Streams were already reachable by placing a slider and converting it, which is two steps and leaves a
    /// slider in the beatmap in between. The shape is the only thing the two have in common, and drawing it is
    /// what the slider tool is for -- so the tool borrows that whole gesture and hands what it drew straight to
    /// the conversion panel.
    /// </remarks>
    public class StreamCompositionTool : CompositionTool<OsuAction>
    {
        public StreamCompositionTool()
            : base(OsuEditorStrings.StreamTool)
        {
            TooltipText = """
                Left click for new point.
                Left click twice or S key for new segment.
                Tab, Shift-Tab, or Alt-1~4 to change current segment type.
                Right click to finish, which opens the stream settings.
                Click and drag for drawing mode.
                """;
            Action = OsuAction.EditorStreamTool;
        }

        public override Drawable CreateIcon() => new SpriteIcon { Icon = OsuIcon.EditorDistanceSnap };

        public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new StreamPlacementBlueprint();
    }
}
