// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Defines the fixed set of mod combinations for which star ratings are persisted (see <see cref="ModStarRating"/>),
    /// and the canonical encoding of a mod selection into a <see cref="ModStarRating.Mods"/> key.
    /// </summary>
    public static class ModStarRatingCombinations
    {
        /// <summary>
        /// The acronyms of mods tracked for persisted star ratings, in sorted order.
        /// Only mods in their default configuration are tracked (ie. DoubleTime only at its default 1.5x rate).
        /// </summary>
        public static readonly string[] TRACKED_ACRONYMS = { @"DT", @"HD", @"HR" };

        private static readonly string[][] tracked_combinations =
            Enumerable.Range(1, (1 << TRACKED_ACRONYMS.Length) - 1)
                      .Select(bits => TRACKED_ACRONYMS.Where((_, i) => (bits & (1 << i)) != 0).ToArray())
                      .ToArray();

        /// <summary>
        /// The keys of all tracked combinations: every non-empty subset of <see cref="TRACKED_ACRONYMS"/>,
        /// each encoded as its acronyms concatenated in sorted order.
        /// </summary>
        public static readonly string[] ALL_KEYS = tracked_combinations.Select(c => string.Concat(c)).ToArray();

        /// <summary>
        /// Computes the canonical key for a mod selection, or <c>null</c> if no star rating is tracked for it.
        /// A selection is tracked when it is exactly one of the tracked combinations with every mod in its default configuration.
        /// An empty selection returns <c>null</c>; its rating is stored as <see cref="BeatmapInfo.StarRating"/> instead.
        /// </summary>
        public static string? GetKey(IEnumerable<Mod> mods)
        {
            var selection = mods.ToArray();

            if (selection.Any(m => !m.UsesDefaultConfiguration))
                return null;

            string[] acronyms = selection.Select(m => m.Acronym).OrderBy(a => a, StringComparer.Ordinal).ToArray();

            string[]? match = tracked_combinations.FirstOrDefault(c => c.SequenceEqual(acronyms));
            return match == null ? null : string.Concat(match);
        }
    }
}
