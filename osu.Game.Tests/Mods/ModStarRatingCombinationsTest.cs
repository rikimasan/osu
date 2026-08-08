// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Tests.Mods
{
    [TestFixture]
    public class ModStarRatingCombinationsTest
    {
        [Test]
        public void TestAllKeysAreTheSevenTrackedCombinations()
        {
            Assert.That(ModStarRatingCombinations.ALL_KEYS, Is.EquivalentTo(new[]
            {
                "DT", "HD", "HR", "DTHD", "DTHR", "HDHR", "DTHDHR",
            }));
        }

        [Test]
        public void TestEverySubsetOfTrackedModsProducesAKey()
        {
            Mod[] trackedMods = { new OsuModDoubleTime(), new OsuModHidden(), new OsuModHardRock() };

            for (int bits = 1; bits < 1 << trackedMods.Length; bits++)
            {
                var subset = trackedMods.Where((_, i) => (bits & (1 << i)) != 0).ToArray();

                string? key = ModStarRatingCombinations.GetKey(subset);

                Assert.That(key, Is.Not.Null);
                Assert.That(ModStarRatingCombinations.ALL_KEYS, Does.Contain(key));
            }
        }

        [Test]
        public void TestKeyIsSelectionOrderIndependent()
        {
            string? forward = ModStarRatingCombinations.GetKey(new Mod[] { new OsuModDoubleTime(), new OsuModHidden(), new OsuModHardRock() });
            string? reverse = ModStarRatingCombinations.GetKey(new Mod[] { new OsuModHardRock(), new OsuModHidden(), new OsuModDoubleTime() });

            Assert.That(forward, Is.EqualTo("DTHDHR"));
            Assert.That(reverse, Is.EqualTo(forward));
        }

        [Test]
        public void TestEmptySelectionHasNoKey()
        {
            Assert.That(ModStarRatingCombinations.GetKey(Enumerable.Empty<Mod>()), Is.Null);
        }

        [Test]
        public void TestNonDefaultSettingsHaveNoKey()
        {
            var doubleTime = new OsuModDoubleTime();
            doubleTime.SpeedChange.Value = 1.4;

            Assert.That(ModStarRatingCombinations.GetKey(new Mod[] { doubleTime }), Is.Null);
        }

        [Test]
        public void TestUntrackedModInSelectionHasNoKey()
        {
            Assert.That(ModStarRatingCombinations.GetKey(new Mod[] { new OsuModHidden(), new OsuModNoFail() }), Is.Null);
        }
    }
}
