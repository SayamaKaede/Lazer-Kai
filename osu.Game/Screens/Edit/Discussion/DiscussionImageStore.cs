// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// Fetches the images posted in the discussion: most through the website's media proxy as its comments are, imgur's
    /// from imgur itself, and the website's own as they are.
    /// </summary>
    /// <remarks>
    /// The game only fetches from the website's own hosts, and the proxy is one of them, so the image host is asked by
    /// the website rather than by the player. The proxy answers 404 for every imgur image, though, which imgur itself
    /// serves (2026-10-19, read in the game's network log and asked of both by hand), so imgur alone is asked
    /// directly, as the player chose. Cached on its own so that a test can hand out images without the network.
    /// </remarks>
    public class DiscussionImageStore : IDisposable
    {
        private const string imgur = @"i.imgur.com";

        private readonly LargeTextureStore? website;
        private readonly LargeTextureStore? imageHosts;
        private readonly string websiteUrl = string.Empty;

        /// <param name="website">The game's own store, which fetches from the website's hosts only.</param>
        /// <param name="imageHosts">One that fetches from anywhere, for the hosts the proxy cannot reach. Owned by this.</param>
        /// <param name="websiteUrl">Where the proxy is.</param>
        public DiscussionImageStore(LargeTextureStore website, LargeTextureStore imageHosts, string websiteUrl)
        {
            this.website = website;
            this.imageHosts = imageHosts;
            this.websiteUrl = websiteUrl;
        }

        protected DiscussionImageStore()
        {
        }

        /// <summary>
        /// The image at <paramref name="imageUrl"/>, or null where there is none to be had.
        /// </summary>
        /// <remarks>Waits for the download, so is only for a load thread.</remarks>
        public virtual Texture? Get(string imageUrl)
        {
            var source = SourceOf(imageUrl, websiteUrl);
            return (source.FromImageHost ? imageHosts : website)?.Get(source.Url);
        }

        /// <summary>
        /// Where the image at <paramref name="imageUrl"/> is asked for.
        /// </summary>
        public static ImageSource SourceOf(string imageUrl, string websiteUrl)
        {
            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
            {
                if (uri.Host.Equals(imgur, StringComparison.OrdinalIgnoreCase))
                    return new ImageSource(imageUrl, true);

                // the website's own, such as a screenshot shared with Shift+F12, which the game fetches as it is.
                if (uri.Host.EndsWith(@".ppy.sh", StringComparison.OrdinalIgnoreCase))
                    return new ImageSource(imageUrl, false);
            }

            return new ImageSource($@"{websiteUrl}/media-url?url={Uri.EscapeDataString(imageUrl)}", false);
        }

        public void Dispose()
        {
            imageHosts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <param name="Url">What is asked for.</param>
    /// <param name="FromImageHost">Whether it is asked of the image's own host rather than of the website.</param>
    public readonly record struct ImageSource(string Url, bool FromImageHost);
}
