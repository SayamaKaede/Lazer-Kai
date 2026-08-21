// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Tests.Visual;
using osu.Game.Users;

namespace osu.Game.Tests.Beatmaps
{
    /// <summary>
    /// Crediting a difficulty to whoever actually made it, which for a collaboration is several people.
    /// </summary>
    public partial class DifficultyAuthorLookupTest : OsuTestScene
    {
        private DifficultyAuthorLookup lookup = null!;
        private readonly BeatmapLookupCache beatmapCache = new BeatmapLookupCache();

        // the cache has to be this scene's, so that it talks to this scene's api rather than the game's.
        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs(beatmapCache);
            return dependencies;
        }
        private readonly List<int> requested = new List<int>();
        private APIBeatmap.BeatmapOwner[] answer = [];

        [BackgroundDependencyLoader]
        private void load()
        {
            // built once: the steps below run again for every test in this fixture, and a drawable belongs to one parent.
            Children = new Drawable[]
            {
                beatmapCache,
                lookup = new DifficultyAuthorLookup(),
            };
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("answer lookups from this scene", () =>
            {
                requested.Clear();
                answer = [];

                ((DummyAPIAccess)API).HandleRequest = request =>
                {
                    if (request is not GetBeatmapsRequest beatmaps)
                        return false;

                    requested.AddRange(beatmaps.BeatmapIds);
                    beatmaps.TriggerSuccess(new GetBeatmapsResponse
                    {
                        Beatmaps = beatmaps.BeatmapIds.Select(id => new APIBeatmap { OnlineID = id, BeatmapOwners = answer }).ToList()
                    });
                    return true;
                };
            });
        }

        [Test]
        public void TestEveryoneCreditedComesBack()
        {
            APIBeatmap.BeatmapOwner[]? found = null;

            AddStep("a collaboration between two people", () =>
            {
                answer =
                [
                    new APIBeatmap.BeatmapOwner { Id = 11, Username = @"first" },
                    new APIBeatmap.BeatmapOwner { Id = 22, Username = @"second" },
                ];

                lookup.GetAuthors(new BeatmapInfo { OnlineID = 555 }, a => found = a, CancellationToken.None);
            });

            AddUntilStep("both came back", () => found?.Select(a => a.Username), () => Is.EqualTo(new[] { @"first", @"second" }));
            AddAssert("looked up the beatmap itself", () => requested, () => Does.Contain(555));
        }

        /// <summary>
        /// Opening a profile casts its argument, so handing it an id compiles and then throws when clicked.
        /// </summary>
        [Test]
        public void TestAnOwnerConvertsToSomethingAProfileLinkAccepts()
        {
            var user = DifficultyAuthorLookup.AsUser(new APIBeatmap.BeatmapOwner { Id = 42, Username = @"someone" });

            Assert.That(user, Is.InstanceOf<IUser>());
            Assert.That(user.OnlineID, Is.EqualTo(42));
            Assert.That(user.Username, Is.EqualTo(@"someone"));
        }

        /// <summary>
        /// A beatmap which has never been looked up online has no id to ask about, and asking anyway would be a
        /// request per beatmap browsed for nothing.
        /// </summary>
        [TestCase(0)]
        [TestCase(-1)]
        public void TestNoLookupWithoutAnOnlineBeatmap(int id)
        {
            AddStep("look up a local-only beatmap", () => lookup.GetAuthors(new BeatmapInfo { OnlineID = id }, _ => { }, CancellationToken.None));

            AddWaitStep("give it a chance", 5);
            AddAssert("nothing was asked", () => requested, () => Is.Empty);
        }

        /// <summary>
        /// Nothing is known about who made it, so whatever the beatmap file said has to stand.
        /// </summary>
        [Test]
        public void TestNothingHappensWhenNobodyIsCredited()
        {
            bool called = false;

            // a different beatmap from the other tests': the cache answers a repeat without asking again.
            AddStep("nobody is credited", () =>
            {
                answer = [];
                lookup.GetAuthors(new BeatmapInfo { OnlineID = 777 }, _ => called = true, CancellationToken.None);
            });

            AddUntilStep("the lookup happened", () => requested, () => Does.Contain(777));
            AddAssert("but nothing was offered", () => called, () => Is.False);
        }
    }
}
