// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Game.Audio;
using osu.Game.Skinning;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Has the notes of a difficulty made to hitsound in find their sounds in the library: its lanes are files of the
    /// mapper's own folder of sounds, which neither the beatmap nor any skin has.
    /// </summary>
    /// <remarks>
    /// Put around what plays the notes, so that the library is asked before the beatmap, which may have a file of its
    /// own under the name of one in the library.
    /// </remarks>
    public partial class LaneSoundSkinProvidingContainer : SkinProvidingContainer
    {
        private readonly LaneSounds laneSounds;

        public LaneSoundSkinProvidingContainer(LaneSounds laneSounds)
            : base(new LibrarySounds(laneSounds))
        {
            this.laneSounds = laneSounds;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // what was found before the library was read again is not what is there now.
            laneSounds.LibraryReloaded += TriggerSourceChanged;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (laneSounds.IsNotNull())
                laneSounds.LibraryReloaded -= TriggerSourceChanged;
        }

        /// <summary>
        /// The sounds of the library, as a skin that has nothing else.
        /// </summary>
        private class LibrarySounds : ISkin
        {
            private readonly LaneSounds laneSounds;

            public LibrarySounds(LaneSounds laneSounds)
            {
                this.laneSounds = laneSounds;
            }

            public ISample? GetSample(ISampleInfo sampleInfo) => laneSounds.SampleFor(sampleInfo);

            public Drawable? GetDrawableComponent(ISkinComponentLookup lookup) => null;

            public Texture? GetTexture(string componentName, WrapMode wrapModeS, WrapMode wrapModeT) => null;

            public IBindable<TValue>? GetConfig<TLookup, TValue>(TLookup lookup)
                where TLookup : notnull
                where TValue : notnull
                => null;
        }
    }
}
