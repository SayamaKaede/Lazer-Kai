// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// Finds the images in a discussion post, to show under it (2026-10-19 request).
    /// </summary>
    /// <remarks>
    /// An image is one put in with markdown, a link to an image file, a link to the page imgur or Gyazo gives a
    /// single screenshot, or a screenshot shared from the game. The markdown is taken out of the text, since read as text it is only brackets around a link;
    /// the links are left, where they can still be opened in the browser.
    /// </remarks>
    public static class DiscussionImageLinks
    {
        private static readonly Regex markdown_image = new Regex(@"!\[[^\]]*\]\(\s*<?(?<url>[^\s)>]+)>?(?:\s+""[^""]*"")?\s*\)", RegexOptions.Compiled);

        private static readonly Regex link = new Regex(@"https?://[^\s<>()\[\]""]+", RegexOptions.Compiled);

        private static readonly Regex imgur_page = new Regex(@"^/(?<id>[A-Za-z0-9]{5,10})$", RegexOptions.Compiled);

        private static readonly Regex gyazo_page = new Regex(@"^/(?<id>[0-9a-f]{32})$", RegexOptions.Compiled);

        private static readonly Regex screenshot = new Regex(@"^/ss/(?<id>\d+)(?:/(?<hash>[0-9a-f]+))?/?$", RegexOptions.Compiled);

        private static readonly string[] image_extensions = { @".png", @".jpg", @".jpeg", @".gif", @".webp" };

        public static (string Text, IReadOnlyList<string> Images) Extract(string text)
        {
            var found = new List<(int Index, string Url)>();
            var markdown = markdown_image.Matches(text);

            foreach (Match m in markdown)
            {
                string url = m.Groups[@"url"].Value;
                // put in as an image by whoever wrote it, so taken at their word unless it is a page with one in it.
                found.Add((m.Index, imageOfPage(url) ?? url));
            }

            foreach (Match m in link.Matches(text))
            {
                if (markdown.Any(md => m.Index >= md.Index && m.Index < md.Index + md.Length))
                    continue;

                // the end of the sentence the link ends, rather than part of it.
                string url = m.Value.TrimEnd('.', ',', '!', '?', ';', ':');

                if (imageAt(url) is string image)
                    found.Add((m.Index, image));
            }

            string remaining = Regex.Replace(markdown_image.Replace(text, string.Empty), @"\n{3,}", "\n\n").Trim();

            return (remaining, found.OrderBy(f => f.Index).Select(f => f.Url).Distinct().ToArray());
        }

        /// <summary>
        /// Where the image a link shows can be had from, or null where it is not a link to one.
        /// </summary>
        private static string? imageAt(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return null;

            if (image_extensions.Any(e => uri.AbsolutePath.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                return url;

            return imageOfPage(url);
        }

        /// <summary>
        /// The image on a page that holds just the one, or null where the page is not one of those.
        /// </summary>
        /// <remarks>
        /// Both sites serve a single screenshot's page at the image's id and the image itself at that id on another
        /// host. An imgur album or gallery holds several, which its address does not tell apart.
        /// </remarks>
        private static string? imageOfPage(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return null;

            switch (uri.Host.ToLowerInvariant())
            {
                case @"imgur.com":
                case @"www.imgur.com":
                case @"m.imgur.com":
                {
                    var page = imgur_page.Match(uri.AbsolutePath);
                    return page.Success ? $@"https://i.imgur.com/{page.Groups[@"id"].Value}.png" : null;
                }

                case @"gyazo.com":
                case @"www.gyazo.com":
                {
                    var page = gyazo_page.Match(uri.AbsolutePath);
                    return page.Success ? $@"https://i.gyazo.com/{page.Groups[@"id"].Value}.png" : null;
                }

                // a screenshot shared from the game with Shift+F12, which is the image itself rather than a page. Stable
                // still hands these out over http.
                case @"osu.ppy.sh":
                {
                    var shot = screenshot.Match(uri.AbsolutePath);

                    if (!shot.Success)
                        return null;

                    string hash = shot.Groups[@"hash"].Value;
                    return $@"https://osu.ppy.sh/ss/{shot.Groups[@"id"].Value}" + (hash.Length > 0 ? $@"/{hash}" : string.Empty);
                }

                default:
                    return null;
            }
        }
    }
}
