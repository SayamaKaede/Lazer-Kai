// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osu.Game.Database;
using osu.Game.Localisation;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// The screen the sounds of the beatmap set being edited are given the names they are played as hitsounds by on,
    /// and sound files are dropped on to be made sounds of the set.
    /// </summary>
    /// <remarks>
    /// A file of sound dropped on the editor is made the song of the beatmap, by the setup screen. What is dropped
    /// while this is open is a hitsound, and the setup screen is not to take it for a song: it only takes what is
    /// dropped while the editor itself is the screen shown (see <c>FormBeatmapFileSelector</c>).
    /// </remarks>
    public partial class SoundFileNamesScreen : BeatmapFilesScreen, ICanAcceptFiles
    {
        public SoundsOfTheSet Sounds { get; private set; } = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        protected override LocalisableString Heading => LazerKaiBeatmapFilesStrings.EditFileNames;

        protected override bool Changed => Sounds.Changed;

        protected override Drawable CreateContent() => Sounds = new SoundsOfTheSet();

        /// <summary>
        /// The kinds of file a hitsound is.
        /// </summary>
        public IEnumerable<string> HandledExtensions => SupportedExtensions.AUDIO_EXTENSIONS;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            game.RegisterImportHandler(this);
        }

        Task ICanAcceptFiles.Import(params string[] paths)
        {
            // what is dropped once this has been left is not this screen's, though it is not yet gone.
            if (this.IsCurrentScreen())
                Schedule(() => Sounds.AddFiles(paths));

            return Task.CompletedTask;
        }

        Task ICanAcceptFiles.Import(ImportTask[] tasks, ImportParameters parameters) => throw new NotImplementedException();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (game.IsNotNull())
                game.UnregisterImportHandler(this);
        }
    }
}
