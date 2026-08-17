// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;

namespace osu.Game.Tests.Database
{
    [TestFixture]
    public class ModStarRatingCacheTest
    {
        private TemporaryNativeStorage storage = null!;
        private ModStarRatingCache cache = null!;

        [SetUp]
        public void SetUp()
        {
            storage = new TemporaryNativeStorage(Guid.NewGuid().ToString());
            cache = new ModStarRatingCache(storage);
        }

        [TearDown]
        public void TearDown()
        {
            storage.Dispose();
        }

        [Test]
        public void TestRoundTrip()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);
            cache.Store("abc123", "osu", "HR", 20260706, 8.25);

            Assert.That(cache.GetRatings("abc123", "osu", 20260706), Is.EquivalentTo(new Dictionary<string, double>
            {
                ["DTHD"] = 7.5,
                ["HR"] = 8.25,
            }));
        }

        [Test]
        public void TestMissOnAbsentEntry()
        {
            Assert.That(cache.GetRatings("unknown", "osu", 20260706), Is.Empty);
        }

        [Test]
        public void TestMissOnDifferentKeyComponents()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);

            Assert.That(cache.GetRatings("abc123", "taiko", 20260706), Is.Empty);
            Assert.That(cache.GetRatings("other", "osu", 20260706), Is.Empty);
        }

        [Test]
        public void TestMissOnCalculatorVersionMismatch()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);

            Assert.That(cache.GetRatings("abc123", "osu", 20270101), Is.Empty);
        }

        [Test]
        public void TestStoreReplacesExistingEntry()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);
            cache.Store("abc123", "osu", "DTHD", 20270101, 7.8);

            Assert.That(cache.GetRatings("abc123", "osu", 20260706), Is.Empty);
            Assert.That(cache.GetRatings("abc123", "osu", 20270101)["DTHD"], Is.EqualTo(7.8));
        }

        [Test]
        public void TestValuesSurviveReinstantiation()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);

            var secondInstance = new ModStarRatingCache(storage);

            Assert.That(secondInstance.GetRatings("abc123", "osu", 20260706)["DTHD"], Is.EqualTo(7.5));
        }
    }
}
