// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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

            Assert.That(cache.TryGet("abc123", "osu", "DTHD", 20260706, out double rating), Is.True);
            Assert.That(rating, Is.EqualTo(7.5));
        }

        [Test]
        public void TestMissOnAbsentEntry()
        {
            Assert.That(cache.TryGet("unknown", "osu", "DT", 20260706, out _), Is.False);
        }

        [Test]
        public void TestMissOnDifferentKeyComponents()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);

            Assert.That(cache.TryGet("abc123", "osu", "DT", 20260706, out _), Is.False);
            Assert.That(cache.TryGet("abc123", "taiko", "DTHD", 20260706, out _), Is.False);
            Assert.That(cache.TryGet("other", "osu", "DTHD", 20260706, out _), Is.False);
        }

        [Test]
        public void TestMissOnCalculatorVersionMismatch()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);

            Assert.That(cache.TryGet("abc123", "osu", "DTHD", 20270101, out _), Is.False);
        }

        [Test]
        public void TestStoreReplacesExistingEntry()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);
            cache.Store("abc123", "osu", "DTHD", 20270101, 7.8);

            Assert.That(cache.TryGet("abc123", "osu", "DTHD", 20260706, out _), Is.False);
            Assert.That(cache.TryGet("abc123", "osu", "DTHD", 20270101, out double rating), Is.True);
            Assert.That(rating, Is.EqualTo(7.8));
        }

        [Test]
        public void TestValuesSurviveReinstantiation()
        {
            cache.Store("abc123", "osu", "DTHD", 20260706, 7.5);

            var secondInstance = new ModStarRatingCache(storage);

            Assert.That(secondInstance.TryGet("abc123", "osu", "DTHD", 20260706, out double rating), Is.True);
            Assert.That(rating, Is.EqualTo(7.5));
        }
    }
}
