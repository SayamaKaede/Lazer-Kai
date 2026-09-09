// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// The gameplay cursor, drawn at the size it would actually be played at.
    /// </summary>
    /// <remarks>
    /// The cursor works its own size out from the cursor size setting and, when the size is set to follow circle
    /// size, from the beatmap being played. There is no beatmap being played here -- the editor has no gameplay
    /// screen for it to read one off -- so the second half of that quietly did nothing and the cursor stayed at
    /// whatever the first difficulty opened happened to want. The difficulty being edited is what it should follow,
    /// and unlike a beatmap in play, that one's circle size can change while the cursor is on screen.
    /// </remarks>
    public partial class EditorAutoCursor : OsuCursor
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private float lastCircleSize;

        protected override float CalculateCursorScale()
        {
            float scale = base.CalculateCursorScale();

            // read straight out of configuration rather than through a bindable of this class's own. The base class
            // works the scale out again the moment its own copy of this setting changes, and its copy was bound
            // first, so a copy here would still be holding the old value when that happens.
            if (config.Get<bool>(OsuSetting.AutoCursorSize))
                scale *= GetScaleForCircleSize(editorBeatmap.Difficulty.CircleSize);

            return scale;
        }

        protected override void Update()
        {
            base.Update();

            if (lastCircleSize == editorBeatmap.Difficulty.CircleSize)
                return;

            lastCircleSize = editorBeatmap.Difficulty.CircleSize;
            UpdateCursorScale();
        }
    }
}
