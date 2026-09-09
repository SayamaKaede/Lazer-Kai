// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Game.IO
{
    /// <summary>
    /// This fork's own drawings, and where they come from.
    /// </summary>
    /// <remarks>
    /// Each one has a copy built into the game, and each one can be replaced by dropping a file of the same name
    /// into <see cref="DIRECTORY"/> beside the game's data. That folder is made on startup so it can be found
    /// without being told where to look, and a file in it wins over the copy inside the build -- which means a
    /// revised drawing does not need a new build, and does not need anything measured off it by hand either: see
    /// <see cref="Overlays.Settings.Sections.LazerKai.LazerKaiMark"/>, which reads what it needs off the picture.
    ///
    /// Kept out of the game's own resource store on purpose. Adding a folder anyone can write to in front of the
    /// game's textures would let a stray file stand in for any of them.
    /// </remarks>
    public class LazerKaiArtwork
    {
        /// <summary>The folder beside the game's data that a replacement drawing goes in.</summary>
        public const string DIRECTORY = @"marks";

        private readonly ResourceStore<byte[]> files;
        private readonly TextureStore textures;

        public LazerKaiArtwork(GameHost host, Storage storage)
        {
            files = CreateStore(storage);
            textures = new TextureStore(host.Renderer, host.CreateTextureLoaderStore(files));
        }

        /// <summary>
        /// Where the drawings are read from, replacements first.
        /// </summary>
        /// <param name="storage">
        /// The game's data folder, or null to read only what is built in -- which is what the tests do.
        /// </param>
        public static ResourceStore<byte[]> CreateStore(Storage? storage)
        {
            var store = new ResourceStore<byte[]>();

            // added first, because the first store to answer wins and a replacement is meant to.
            if (storage != null)
                store.AddStore(new StorageBackedResourceStore(storage.GetStorageForDirectory(DIRECTORY)));

            // the files sit under `Resources/` in the project and that folder ends up in their embedded name, which
            // nothing else would account for.
            store.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(LazerKaiArtwork).Assembly),
                @"Resources/Textures/LazerKai"));

            return store;
        }

        /// <summary>The drawing to draw, or null when there is none by that name.</summary>
        public Texture? GetTexture(string name) => textures.Get(name);

        /// <summary>The same drawing as bytes, for reading something off the picture itself.</summary>
        public byte[]? GetBytes(string name) => files.Get($@"{name}.png");
    }
}
