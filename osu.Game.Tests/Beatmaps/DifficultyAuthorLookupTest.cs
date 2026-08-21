// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Models;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Tests.Visual;

namespace osu.Game.Tests.Beatmaps
{
    /// <summary>
    /// Correcting a guest difficulty's mapper name, which the beatmap file records as the set owner's.
    /// </summary>
    public partial class DifficultyAuthorLookupTest : OsuTestScene
    {
        private DifficultyAuthorLookup lookup = null!;
        private readonly List<int> requested = new List<int>();

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set up the lookup", () =>
            {
                requested.Clear();

                ((DummyAPIAccess)API).HandleRequest = request =>
                {
                    if (request is not LookupUsersRequest users)
                        return false;

                    requested.AddRange(users.UserIds);
                    users.TriggerSuccess(new GetUsersResponse
                    {
                        Users = users.UserIds.Select(id => new APIUser { Id = id, Username = $"guest{id}" }).ToList()
                    });
                    return true;
                };

                // the lookup cache has to be this scene's, so that it talks to this scene's api rather than the game's.
                var userCache = new UserLookupCache();

                Child = new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies = new (System.Type, object)[] { (typeof(UserLookupCache), userCache) },
                    Children = new Drawable[]
                    {
                        userCache,
                        lookup = new DifficultyAuthorLookup(),
                    }
                };
            });
        }

        [Test]
        public void TestNameFromTheFileIsCorrected()
        {
            string? corrected = null;

            AddStep("look up a difficulty credited to the set owner", () =>
                lookup.GetAuthorName(beatmapAuthoredBy(1234, @"sethost"), name => corrected = name, CancellationToken.None));

            AddUntilStep("the guest's name came back", () => corrected, () => Is.EqualTo(@"guest1234"));
        }

        /// <summary>
        /// The recorded id is the author whether or not a guest made the difficulty, so the set owner's own
        /// difficulties resolve to the name already on screen and must not disturb it.
        /// </summary>
        [Test]
        public void TestNothingHappensWhenTheNameAlreadyMatches()
        {
            bool called = false;

            AddStep("look up a difficulty the owner made", () =>
                lookup.GetAuthorName(beatmapAuthoredBy(1234, @"guest1234"), _ => called = true, CancellationToken.None));

            AddUntilStep("the lookup happened", () => requested, () => Does.Contain(1234));
            AddAssert("but nothing was changed", () => called, () => Is.False);
        }

        /// <summary>
        /// A beatmap which has never been looked up online carries no author id, and there is nothing to ask about.
        /// </summary>
        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-1)]
        public void TestNoLookupWithoutARealAuthorId(int id)
        {
            AddStep("look up a difficulty with no author id", () =>
                lookup.GetAuthorName(beatmapAuthoredBy(id, @"someone"), _ => { }, CancellationToken.None));

            AddWaitStep("give it a chance", 5);
            AddAssert("nothing was asked", () => requested, () => Is.Empty);
        }

        private static BeatmapInfo beatmapAuthoredBy(int onlineID, string username) => new BeatmapInfo
        {
            Metadata = new BeatmapMetadata { Author = new RealmUser { OnlineID = onlineID, Username = username } }
        };
    }
}
