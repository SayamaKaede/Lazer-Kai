// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game.Resources;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.Collab;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osu.Game.Screens.Edit.Discussion;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Reading a beatmap's discussion in the editor, and going to what each post is about (2026-10-18 request).
    /// </summary>
    public partial class TestSceneEditorDiscussion : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private DiscussionOverlay overlay => Editor.ChildrenOfType<DiscussionOverlay>().Single();

        private long[] shown() => overlay.ChildrenOfType<DiscussionRow>().Select(r => r.Discussion.Id).ToArray();

        private DiscussionRow row(long id) => overlay.ChildrenOfType<DiscussionRow>().Single(r => r.Discussion.Id == id);

        private readonly List<int> pagesAskedFor = new List<int>();
        private readonly List<long> repliesAskedFor = new List<long>();

        /// <summary>The object the first discussion names, by its time and its number in its combo.</summary>
        private HitObject named = null!;

        /// <summary>The object the second discussion is at.</summary>
        private HitObject earlier = null!;

        private string namedTime => named.StartTime.ToEditorFormattedString();

        private DiscussionImage[] images(long id) => row(id).ChildrenOfType<DiscussionImage>().ToArray();

        private DiscussionImageViewer viewer => overlay.ChildrenOfType<DiscussionImageViewer>().Single();

        private LocalImages localImages = null!;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            Dependencies.CacheAs<DiscussionImageStore>(localImages = new LocalImages(host.Renderer,
                () => host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(OsuResources.ResourceAssembly), @"Textures"))));
        }

        [SetUpSteps]
        public void SetUpDiscussion()
        {
            AddStep("answer for the discussion", () =>
            {
                pagesAskedFor.Clear();
                repliesAskedFor.Clear();
                localImages.Renew();

                earlier = EditorBeatmap.HitObjects[1];
                // not the first in its combo, so that the number in brackets is not the one every combo has.
                named = EditorBeatmap.HitObjects.Skip(4).First(h => ((IHasComboInformation)h).IndexInCurrentCombo > 0);

                dummyAPI.SetState(APIState.Online);
                dummyAPI.HandleRequest = answer;
            });
        }

        [Test]
        public void TestTheTimelineComesFirstInTimeOrder()
        {
            open();
            AddUntilStep("the timeline, then this difficulty, then the whole set", shown, () => Is.EqualTo(new long[] { 2, 1, 3, 4, 6 }));
        }

        /// <summary>
        /// What was said about another difficulty would be read against this one, and its times would go to the wrong
        /// objects.
        /// </summary>
        [Test]
        public void TestOtherDifficultiesAreLeftOut()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));
            AddAssert("nothing from another difficulty", shown, () => Does.Not.Contain(5));
        }

        [Test]
        public void TestEveryPageIsRead()
        {
            open();
            AddUntilStep("the second page is shown too", shown, () => Does.Contain(6));
            AddAssert("asked for both pages, in order", () => pagesAskedFor, () => Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void TestClickingTheTimeGoesThere()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddStep("click the second discussion's time", () => click(row(2).TimeLink,earlier.StartTime.ToEditorFormattedString()));
            AddUntilStep("at that time", () => EditorClock.CurrentTime, () => Is.EqualTo((int)earlier.StartTime).Within(1));
        }

        /// <summary>
        /// A time written in a post with combo numbers after it selects those objects, as it does from the website.
        /// </summary>
        [Test]
        public void TestClickingATimeInAPostSelectsWhatItNames()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddStep("click the time in the first post", () => click(row(1).Message, namedTime));
            AddUntilStep("the object it names is selected", () => EditorBeatmap.SelectedHitObjects, () => Is.EqualTo(new[] { named }));
        }

        /// <summary>
        /// Replies are asked for one thread at a time, so they are asked for only once a thread is opened.
        /// </summary>
        [Test]
        public void TestRepliesAreReadWhenAThreadIsOpened()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));
            AddAssert("no replies asked for yet", () => repliesAskedFor, () => Is.Empty);

            AddStep("show the first thread's replies", () =>
            {
                InputManager.MoveMouseTo(row(1).RepliesButton);
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("its reply is shown", () => row(1).ChildrenOfType<DiscussionReply>().Select(r => r.Post.Message), () => Is.EqualTo(new[] { "fixed" }));
            AddAssert("asked for that thread's only", () => repliesAskedFor, () => Is.EqualTo(new long[] { 1 }));
        }

        [Test]
        public void TestReopeningDoesNotAskAgain()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));
            AddStep("close it", () => overlay.Hide());
            open();
            AddAssert("both pages asked for once", () => pagesAskedFor, () => Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void TestABeatmapNotUploadedSaysSo()
        {
            AddStep("forget the beatmap's online ids", () =>
            {
                EditorBeatmap.BeatmapInfo.OnlineID = -1;
                EditorBeatmap.BeatmapInfo.BeatmapSet!.OnlineID = -1;
            });
            open();
            AddUntilStep("says it has not been uploaded", () => saying(DiscussionStrings.NotUploaded));
            AddAssert("asked for nothing", () => pagesAskedFor, () => Is.Empty);
        }

        [Test]
        public void TestLoggedOutSaysSo()
        {
            AddStep("log out", () => dummyAPI.SetState(APIState.Offline));
            open();
            AddUntilStep("says to log in", () => saying(DiscussionStrings.NotLoggedIn));
            AddAssert("asked for nothing", () => pagesAskedFor, () => Is.Empty);
        }

        [Test]
        public void TestTheKeyOpensAndClosesIt()
        {
            press(GlobalAction.EditorToggleDiscussion);
            AddAssert("open", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));

            press(GlobalAction.EditorToggleDiscussion);
            AddAssert("closed", () => overlay.State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        /// <summary>
        /// An image posted with markdown is shown under the post, at the height the website shows it, and its markdown
        /// is no longer in the text (2026-10-19 request).
        /// </summary>
        [Test]
        public void TestAnImageIsShownUnderItsPost()
        {
            open();
            AddUntilStep("the third post has its image", () => images(3).Select(i => i.Url), () => Is.EqualTo(new[] { "https://example.com/shot.png" }));
            AddAssert("its markdown is not in the text", () => row(3).Message.ChildrenOfType<SpriteText>().Any(t => t.Text.ToString().Contains("![")), () => Is.False);

            AddUntilStep("the image is loaded", () => images(3).Single().Texture, () => Is.Not.Null);
            AddUntilStep("as tall as on the website", () => images(3).Single().DrawHeight, () => Is.EqualTo(DiscussionImage.HEIGHT).Within(0.01));
            AddAssert("in its own proportions",
                () => images(3).Single().DrawWidth / images(3).Single().DrawHeight,
                () => Is.EqualTo(images(3).Single().Texture!.DisplayWidth / images(3).Single().Texture!.DisplayHeight).Within(0.01));
        }

        [Test]
        public void TestClickingAnImageShowsItWhole()
        {
            open();
            AddUntilStep("the image is loaded", () => images(3).SingleOrDefault()?.Texture, () => Is.Not.Null);

            AddStep("click it", () =>
            {
                InputManager.MoveMouseTo(images(3).Single());
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("shown large", () => viewer.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("the same picture", () => viewer.Url, () => Is.EqualTo(images(3).Single().Url));
            AddUntilStep("loaded", () => viewer.Texture, () => Is.Not.Null);

            AddStep("click again", () => InputManager.Click(MouseButton.Left));
            AddUntilStep("put away", () => viewer.State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the discussion stays open", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        /// <summary>
        /// A sprite disposes of the texture it is given when it is given another, so showing one image after another
        /// disposed of the first thumbnail's texture while the thumbnail was still drawing it, and the game crashed
        /// (2026-10-19, DOTNET-K).
        /// </summary>
        [Test]
        public void TestLookingAtAnotherImageLeavesTheFirstThumbnailAlone()
        {
            open();
            AddUntilStep("both images are loaded", () => images(3).SingleOrDefault()?.Texture != null && images(4).SingleOrDefault()?.Texture != null);

            showLarge(3);
            AddStep("put it away", () => InputManager.Click(MouseButton.Left));
            AddUntilStep("put away", () => viewer.State.Value, () => Is.EqualTo(Visibility.Hidden));
            showLarge(4);

            AddAssert("the first thumbnail's picture is still held", () => localImages.StillHeld(images(3).Single().Url));
        }

        private void showLarge(long id)
        {
            AddStep($"click post {id}'s image", () =>
            {
                InputManager.MoveMouseTo(images(id).Single());
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("shown large", () => viewer.Url, () => Is.EqualTo(images(id).Single().Url));
            AddUntilStep("loaded", () => viewer.Texture, () => Is.Not.Null);
        }

        [Test]
        public void TestEscapePutsTheImageAwayBeforeTheDiscussion()
        {
            open();
            AddUntilStep("the image is loaded", () => images(3).SingleOrDefault()?.Texture, () => Is.Not.Null);
            AddStep("click it", () =>
            {
                InputManager.MoveMouseTo(images(3).Single());
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("shown large", () => viewer.State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press escape", () => InputManager.Key(Key.Escape));
            AddUntilStep("put away", () => viewer.State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the discussion stays open", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        /// <summary>
        /// The website's tabs, each saying how many threads it lists (2026-10-20 request).
        /// </summary>
        [Test]
        public void TestTheTabsCountAndDivideTheList()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddAssert("counted as the website counts", () => Enum.GetValues<DiscussionFilter>().Select(f => tabs.CountOf(f)),
                () => Is.EqualTo(new[] { 0, 1, 1, 1, 2, 5 }));

            pickTab(DiscussionFilter.Pending);
            AddAssert("only the problem still open", shown, () => Is.EqualTo(new long[] { 1 }));

            pickTab(DiscussionFilter.Praises);
            AddAssert("the praise and the hype", shown, () => Is.EqualTo(new long[] { 3, 4 }));

            pickTab(DiscussionFilter.All);
            AddAssert("everything again", shown, () => Is.EqualTo(new long[] { 2, 1, 3, 4, 6 }));
        }

        /// <summary>
        /// A thread's replies stay open while the tabs take it out of the list and put it back.
        /// </summary>
        [Test]
        public void TestSwitchingTabsKeepsRepliesOpen()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddStep("show the first thread's replies", () =>
            {
                InputManager.MoveMouseTo(row(1).RepliesButton);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("its reply is shown", () => row(1).ChildrenOfType<DiscussionReply>().Count(), () => Is.EqualTo(1));

            pickTab(DiscussionFilter.Praises);
            pickTab(DiscussionFilter.All);

            AddAssert("still shown", () => row(1).ChildrenOfType<DiscussionReply>().Count(), () => Is.EqualTo(1));
            AddAssert("asked for once", () => repliesAskedFor, () => Is.EqualTo(new long[] { 1 }));
        }

        [Test]
        public void TestPickingAUserListsOnlyTheirs()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddAssert("everyone, then each by name", () => overlay.ChildrenOfType<UserDropdown>().Single().Items, () => Is.EqualTo(new[] { UserDropdown.EVERYONE, 3, 2 }));

            AddStep("pick the mapper", () => overlay.ChildrenOfType<UserDropdown>().Single().Current.Value = 3);
            AddAssert("only the mapper's", shown, () => Is.EqualTo(new long[] { 6 }));
            AddAssert("counted for the mapper alone", () => tabs.CountOf(DiscussionFilter.All), () => Is.EqualTo(1));
        }

        [Test]
        public void TestSortingByLastUpdated()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddStep("sort by last updated", () =>
            {
                InputManager.MoveMouseTo(overlay.ChildrenOfType<DiscussionSortSwitch>().Single().OptionFor(DiscussionSort.Updated));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("each part with the last changed first", shown, () => Is.EqualTo(new long[] { 1, 2, 3, 6, 4 }));
        }

        /// <summary>
        /// For putting a post into a translator (2026-10-20 request).
        /// </summary>
        [Test]
        public void TestCopyingAPostLeavesItsImagesOut()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddStep("copy the praise", () =>
            {
                InputManager.MoveMouseTo(row(3).CopyButton);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("its text alone", () => clipboard.GetText(), () => Is.EqualTo("nice"));
        }

        [Test]
        public void TestCopyingAReply()
        {
            open();
            AddUntilStep("the list is shown", () => shown().Length, () => Is.GreaterThan(0));

            AddStep("show the first thread's replies", () =>
            {
                InputManager.MoveMouseTo(row(1).RepliesButton);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("its reply is shown", () => row(1).ChildrenOfType<DiscussionReply>().Count(), () => Is.EqualTo(1));

            AddStep("copy the reply", () =>
            {
                InputManager.MoveMouseTo(row(1).ChildrenOfType<DiscussionReply>().Single().CopyButton);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("the reply's text", () => clipboard.GetText(), () => Is.EqualTo("fixed"));
        }

        /// <summary>
        /// Where the threads are, along the timeline at the bottom, once the panel has read them; and only those the
        /// tab lists (2026-10-20 request).
        /// </summary>
        [Test]
        public void TestTheTimelineMarksWhatTheTabLists()
        {
            AddAssert("nothing marked before the panel is opened", marked, () => Is.Empty);

            open();
            AddUntilStep("the threads at a time are marked", marked, () => Is.EquivalentTo(new long[] { 1, 2 }));

            pickTab(DiscussionFilter.Pending);
            AddAssert("only what is still open", marked, () => Is.EquivalentTo(new long[] { 1 }));

            AddStep("close the panel", () => overlay.Hide());
            AddAssert("still marked", marked, () => Is.EquivalentTo(new long[] { 1 }));
        }

        /// <summary>
        /// A resolved thread is marked the way the website marks it, with a green ring and tick in place of its kind
        /// (2026-10-20 request). Its row is darkened as well, so the ones still open stand out among all of them
        /// (2026-10-21, chosen by the user); its mark on the timeline is not.
        /// </summary>
        [Test]
        public void TestAResolvedThreadIsMarkedAsTheWebsiteMarksIt()
        {
            open();
            AddUntilStep("the threads at a time are marked", marked, () => Is.EquivalentTo(new long[] { 1, 2 }));

            AddAssert("its mark is a green ring and tick", () => iconOf(Editor.ChildrenOfType<DiscussionPart.Mark>().Single(m => m.Discussion.Id == 2)),
                () => Is.EqualTo(FontAwesome.Regular.CheckCircle));
            AddAssert("its mark is not faded", () => Editor.ChildrenOfType<DiscussionPart.Mark>().Single(m => m.Discussion.Id == 2).Alpha, () => Is.EqualTo(1));

            AddAssert("its row starts with the same", () => iconOf(row(2)), () => Is.EqualTo(FontAwesome.Regular.CheckCircle));
            // the body of the row; the images and replies under it are hidden while there are none.
            AddAssert("its row is darkened", () => row(2).ChildrenOfType<FillFlowContainer>().First().Alpha, () => Is.LessThan(1));

            AddAssert("an open one keeps its kind", () => iconOf(row(1)), () => Is.EqualTo(FontAwesome.Solid.ExclamationCircle));
            AddAssert("an open one is not darkened", () => row(1).ChildrenOfType<FillFlowContainer>().First().Alpha, () => Is.EqualTo(1));
        }

        private static IconUsage iconOf(Drawable drawable) => drawable.ChildrenOfType<SpriteIcon>().First().Icon;

        [Test]
        public void TestClickingAMarkGoesThereAndOpensTheThread()
        {
            open();
            AddUntilStep("the threads at a time are marked", marked, () => Is.EquivalentTo(new long[] { 1, 2 }));
            AddStep("close the panel", () => overlay.Hide());

            AddStep("click the mark of the second thread", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<DiscussionPart.Mark>().Single(m => m.Discussion.Id == 2));
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("at its time", () => EditorClock.CurrentTime, () => Is.EqualTo((int)earlier.StartTime).Within(1));
            AddAssert("the panel is open", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        [Test]
        public void TestTheOpacitySetsThePanel()
        {
            open();
            AddStep("set it to half", () => config.SetValue(OsuSetting.EditorDiscussionOpacity, 0.5f));
            AddAssert("the panel is half there", () => overlay.ChildrenOfType<CollabInputBlocker>().First().Alpha, () => Is.EqualTo(0.5f));
            AddStep("set it back", () => config.SetValue(OsuSetting.EditorDiscussionOpacity, 1f));
        }

        [Resolved]
        private Clipboard clipboard { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private DiscussionTabs tabs => overlay.ChildrenOfType<DiscussionTabs>().Single();

        private long[] marked() => Editor.ChildrenOfType<DiscussionPart.Mark>().Select(m => m.Discussion.Id).ToArray();

        private void pickTab(DiscussionFilter filter) => AddStep($"pick {filter}", () =>
        {
            InputManager.MoveMouseTo(tabs.TabFor(filter));
            InputManager.Click(MouseButton.Left);
        });

        private void open() => AddStep("open the discussion", () => overlay.Show());

        private void press(GlobalAction action) => AddStep($"press {action}",
            () => Editor.OnPressed(new KeyBindingPressEvent<GlobalAction>(new InputState(), action)));

        private bool saying(LocalisableString text) => overlay.ChildrenOfType<SpriteText>().Any(t => t.IsPresent && t.Text.Equals(text));

        /// <summary>
        /// Clicks a word of a text flow, which carries the space after it.
        /// </summary>
        private void click(Drawable flow, string text)
        {
            InputManager.MoveMouseTo(flow.ChildrenOfType<SpriteText>().First(t => t.Text.ToString().Trim() == text));
            InputManager.Click(MouseButton.Left);
        }

        private bool answer(APIRequest request)
        {
            switch (request)
            {
                case GetBeatmapsetDiscussionsRequest discussions:
                    pagesAskedFor.Add(discussions.Page);
                    discussions.TriggerSuccess(discussions.Page == 1 ? firstPage() : secondPage());
                    return true;

                case GetBeatmapsetDiscussionPostsRequest posts:
                    repliesAskedFor.Add(posts.DiscussionId);
                    posts.TriggerSuccess(new APIBeatmapsetDiscussionPosts
                    {
                        Posts = { new APIBeatmapsetDiscussionPost { Id = 101, UserId = 3, Message = "fixed" } },
                        Users = { new APIUser { Id = 3, Username = "the mapper" } },
                    });
                    return true;
            }

            return false;
        }

        private APIBeatmapsetDiscussions firstPage()
        {
            int here = EditorBeatmap.BeatmapInfo.OnlineID;

            return new APIBeatmapsetDiscussions
            {
                Discussions =
                {
                    discussion(1, here, (int)named.StartTime, "problem", $"{namedTime} ({((IHasComboInformation)named).IndexInCurrentCombo + 1}) - too far from the last one"),
                    discussion(2, here, (int)earlier.StartTime, "suggestion", "a whistle here", resolved: true),
                    discussion(3, here, null, "praise", "nice\n![](https://example.com/shot.png)"),
                    discussion(4, null, null, "hype", "hyped\n![](https://example.com/other.png)"),
                    discussion(5, here + 1, (int)named.StartTime, "problem", "about another difficulty"),
                },
                Users = { new APIUser { Id = 2, Username = "the modder" } },
                CursorString = "2",
            };
        }

        private static APIBeatmapsetDiscussions secondPage() => new APIBeatmapsetDiscussions
        {
            Discussions = { discussion(6, null, null, "mapper_note", "from the second page", user: 3) },
            Users = { new APIUser { Id = 3, Username = "the mapper" } },
        };

        /// <summary>
        /// Hands out a picture the game carries for every image asked for, so that no test reaches for the network.
        /// </summary>
        private class LocalImages : DiscussionImageStore
        {
            private readonly IRenderer renderer;
            private readonly Func<IResourceStore<TextureUpload>> createUploads;

            private CountingUploads uploads = null!;
            private LargeTextureStore textures = null!;

            public LocalImages(IRenderer renderer, Func<IResourceStore<TextureUpload>> createUploads)
            {
                this.renderer = renderer;
                this.createUploads = createUploads;
                Renew();
            }

            /// <summary>
            /// A store of its own for each test, so that the references it counts are that test's alone. The game's is
            /// shared with the editors of the tests before, whose thumbnails still hold references of their own.
            /// </summary>
            public void Renew()
            {
                uploads = new CountingUploads(createUploads());
                textures = new LargeTextureStore(renderer, uploads);
            }

            public override Texture? Get(string imageUrl) => textures.Get(pictureFor(imageUrl));

            /// <summary>
            /// Whether the picture an image is shown with is still held by something, rather than let go of and thrown out.
            /// </summary>
            /// <remarks>
            /// Asked by asking for it again: one still held is handed out as it is, and one thrown out is loaded again.
            /// The renderer the tests run on never lets go of a texture, so it cannot say whether one was.
            /// </remarks>
            public bool StillHeld(string imageUrl)
            {
                string picture = pictureFor(imageUrl);
                int loads = uploads.LoadsOf(picture);

                textures.Get(picture)?.Dispose();

                return uploads.LoadsOf(picture) == loads;
            }

            /// <remarks>
            /// A different picture for each image, as the website's are: one picture shared between them is one count of
            /// references between them, which hid a texture being disposed of under a thumbnail (DOTNET-K).
            /// </remarks>
            private static string pictureFor(string imageUrl) => imageUrl.Contains("other") ? @"Menu/menu-background-2" : @"Menu/menu-background-1";

            private class CountingUploads : IResourceStore<TextureUpload>
            {
                private readonly IResourceStore<TextureUpload> store;
                private readonly ConcurrentDictionary<string, int> loads = new ConcurrentDictionary<string, int>();

                public CountingUploads(IResourceStore<TextureUpload> store)
                {
                    this.store = store;
                }

                public int LoadsOf(string name) => loads.GetValueOrDefault(name);

                public TextureUpload Get(string name)
                {
                    loads.AddOrUpdate(name, 1, (_, n) => n + 1);
                    return store.Get(name);
                }

                public Task<TextureUpload> GetAsync(string name, CancellationToken cancellationToken = default)
                {
                    loads.AddOrUpdate(name, 1, (_, n) => n + 1);
                    return store.GetAsync(name, cancellationToken);
                }

                public Stream GetStream(string name) => store.GetStream(name);

                public IEnumerable<string> GetAvailableResources() => store.GetAvailableResources();

                public void Dispose() => store.Dispose();
            }
        }

        private static APIBeatmapsetDiscussion discussion(long id, int? beatmapId, int? timestamp, string type, string message, bool resolved = false, int user = 2) => new APIBeatmapsetDiscussion
        {
            Id = id,
            BeatmapId = beatmapId,
            Timestamp = timestamp,
            MessageType = type,
            Resolved = resolved,
            UserId = user,
            StartingPost = new APIBeatmapsetDiscussionPost { Id = id * 10, UserId = user, Message = message },
            // the first thread was replied to last, though it comes later in the song.
            UpdatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero).AddDays(id == 1 ? 30 : id),
        };
    }
}
