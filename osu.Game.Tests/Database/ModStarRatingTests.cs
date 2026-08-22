// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Database
{
    [TestFixture]
    public class ModStarRatingTests : RealmTest
    {
        [Test]
        public void TestModStarRatingsSurviveBeatmapDetach()
        {
            RunTestWithRealm((realm, _) =>
            {
                addTestBeatmapSetWithModStarRating(realm);

                // Materialised before filtering as realm queries cannot express collection counts.
                var detached = realm.Run(r => r.All<BeatmapInfo>().AsEnumerable().Single(b => b.ModStarRatings.Count > 0).Detach());

                Assert.That(detached.IsManaged, Is.False);
                assertModStarRatingRetained(detached);
            });
        }

        [Test]
        public void TestModStarRatingsSurviveBeatmapSetDetach()
        {
            // The carousel receives beatmaps by detaching at the set level (a separate mapper configuration),
            // so this path must retain mod star ratings for filtering to work.
            RunTestWithRealm((realm, _) =>
            {
                addTestBeatmapSetWithModStarRating(realm);

                var detachedSet = realm.Run(r => r.All<BeatmapSetInfo>().Single().Detach());

                Assert.That(detachedSet.IsManaged, Is.False);
                assertModStarRatingRetained(detachedSet.Beatmaps.Single(b => b.ModStarRatings.Count > 0));
            });
        }

        private static void addTestBeatmapSetWithModStarRating(RealmAccess realm)
        {
            realm.Write(r =>
            {
                var beatmapSet = TestResources.CreateTestBeatmapSetInfo(2);
                beatmapSet.Beatmaps.First().ModStarRatings.Add(new ModStarRating
                {
                    Mods = "DTHD",
                    StarRating = 7.5,
                });
                r.Add(beatmapSet);
            });

            realm.Run(r => r.Refresh());

            // Guards the detach assertions against a silently failed write.
            Assert.That(realm.Run(r => r.All<BeatmapInfo>().AsEnumerable().Count(b => b.ModStarRatings.Count > 0)), Is.EqualTo(1));
        }

        private static void assertModStarRatingRetained(BeatmapInfo beatmap)
        {
            Assert.That(beatmap.ModStarRatings, Has.Count.EqualTo(1));
            Assert.That(beatmap.ModStarRatings.Single().Mods, Is.EqualTo("DTHD"));
            Assert.That(beatmap.ModStarRatings.Single().StarRating, Is.EqualTo(7.5));
        }
    }
}
