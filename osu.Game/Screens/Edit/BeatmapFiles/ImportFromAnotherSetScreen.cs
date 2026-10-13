// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// The screen difficulties and sounds of another beatmap set are brought into the one being edited on.
    /// </summary>
    public partial class ImportFromAnotherSetScreen : BeatmapFilesScreen
    {
        public ImportFromAnotherSet Import { get; private set; } = null!;

        protected override LocalisableString Heading => LazerKaiBeatmapFilesStrings.ImportFromAnotherSet;

        protected override bool Changed => Import.Changed;

        protected override Drawable CreateContent() => Import = new ImportFromAnotherSet();
    }
}
