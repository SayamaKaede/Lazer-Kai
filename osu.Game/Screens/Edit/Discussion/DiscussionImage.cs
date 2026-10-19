// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// An image posted in the discussion, small under its post, shown whole when clicked.
    /// </summary>
    public partial class DiscussionImage : OsuClickableContainer
    {
        /// <summary>
        /// The height the website shows images in posts at.
        /// </summary>
        /// <remarks>The same as the comments' (<c>CommentMarkdownContainer</c>), which takes it from the website.</remarks>
        public const float HEIGHT = 84;

        /// <summary>
        /// How wide a very wide image may get before it is made shorter instead, to stay inside the panel.
        /// </summary>
        private const float max_width = 400;

        public readonly string Url;

        /// <summary>
        /// The image, once it has been fetched. Null before then, and where there was none to be had.
        /// </summary>
        public Texture? Texture { get; private set; }

        [Resolved]
        private DiscussionImageStore store { get; set; } = null!;

        [Resolved]
        private DiscussionImageViewer viewer { get; set; } = null!;

        private Drawable placeholder = null!;

        public DiscussionImage(string url)
        {
            Url = url;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Size = new Vector2(HEIGHT);
            Masking = true;
            CornerRadius = 4;
            TooltipText = Url;

            DelayedLoadWrapper wrapper;

            Children = new Drawable[]
            {
                placeholder = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background5,
                        },
                        new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Icon = FontAwesome.Regular.Image,
                            Size = new Vector2(20),
                            Colour = colourProvider.Foreground1,
                        },
                    }
                },
                // only fetched once scrolled to, since a long discussion can carry a great many.
                wrapper = new DelayedLoadWrapper(() => new FetchedImage(Url, store) { RelativeSizeAxes = Axes.Both }, 0)
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };

            wrapper.DelayedLoadComplete += loaded =>
            {
                // nothing to be had: the placeholder stays, with the address on it to say what was meant to be there.
                if (((FetchedImage)loaded).Texture is not Texture texture)
                    return;

                Texture = texture;

                float scale = Math.Min(HEIGHT / texture.DisplayHeight, max_width / texture.DisplayWidth);
                Size = new Vector2(texture.DisplayWidth, texture.DisplayHeight) * scale;

                placeholder.FadeOut(200, Easing.OutQuint);
                loaded.FadeInFromZero(200, Easing.OutQuint);
            };

            Action = () =>
            {
                if (Texture != null)
                    viewer.Show(Url);
            };
        }
    }

    /// <summary>
    /// An image from the discussion, fetched while it loads.
    /// </summary>
    /// <remarks>
    /// Each one asks for its own texture, and so holds a reference of its own: a sprite disposes of the texture it is
    /// given when it is given another or is itself disposed, so a texture handed from one sprite to another is disposed
    /// of under the first. Showing one image large after another did exactly that to a thumbnail still on screen, and
    /// the game crashed drawing it (2026-10-19, DOTNET-K). A second reference to one already fetched is no second download.
    /// </remarks>
    internal partial class FetchedImage : Sprite
    {
        private readonly string url;
        private readonly DiscussionImageStore store;

        public FetchedImage(string url, DiscussionImageStore store)
        {
            this.url = url;
            this.store = store;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Texture = store.Get(url);
        }
    }

    /// <summary>
    /// An image from the discussion shown whole over the editor, as large as fits without being blown up past its own
    /// size. Any click or escape puts it away.
    /// </summary>
    public partial class DiscussionImageViewer : VisibilityContainer
    {
        /// <summary>
        /// How much of the screen the image may cover, leaving the editor in sight around it.
        /// </summary>
        private const float max_coverage = 0.9f;

        [Resolved]
        private DiscussionImageStore store { get; set; } = null!;

        private FetchedImage? image;

        /// <summary>
        /// The image being shown, which has been asked for and may still be loading.
        /// </summary>
        public string? Url { get; private set; }

        /// <summary>
        /// The texture shown, once it has loaded.
        /// </summary>
        public Texture? Texture => image?.Texture;

        public DiscussionImageViewer()
        {
            RelativeSizeAxes = Axes.Both;

            Child = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.Black,
                Alpha = 0.8f,
            };
        }

        /// <summary>
        /// Shows the image at <paramref name="url"/>, by a reference of its own to it rather than the thumbnail's.
        /// </summary>
        public void Show(string url)
        {
            Url = url;

            // let go of now rather than left up while the next one loads, where it would be taken for that one.
            image?.Expire();
            image = null;

            Show();

            LoadComponentAsync(new FetchedImage(url, store)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            }, loaded =>
            {
                // another one was asked for while this one loaded.
                if (Url != url)
                {
                    loaded.Dispose();
                    return;
                }

                Add(image = loaded);
            });
        }

        protected override void Update()
        {
            base.Update();

            if (image?.Texture is not Texture texture)
                return;

            float scale = Math.Min(1, Math.Min(DrawWidth * max_coverage / texture.DisplayWidth, DrawHeight * max_coverage / texture.DisplayHeight));
            image.Size = new Vector2(texture.DisplayWidth, texture.DisplayHeight) * scale;
        }

        protected override void PopIn() => this.FadeIn(200, Easing.OutQuint);

        protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);

        // over the whole editor, so nothing behind it is reached while it is up.
        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e)
        {
            Hide();
            return true;
        }

        protected override bool OnScroll(ScrollEvent e) => true;

        protected override bool OnHover(HoverEvent e) => true;
    }
}
