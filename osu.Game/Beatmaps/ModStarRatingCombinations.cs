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
        /// Persisted ratings are calculated with each mod in its default configuration (ie. DoubleTime at its default 1.5x rate).
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
        /// Computes the canonical key for a mod selection, or <c>null</c> if it contains no tracked mods.
        /// Only the presence of tracked mods is considered; all other mods and all mod settings are ignored,
        /// on the basis that the tracked rating is a closer approximation than the unmodded one.
        /// </summary>
        public static string? GetKey(IEnumerable<Mod> mods)
        {
            string[] acronyms = mods.Select(m => m.Acronym)
                                    .Where(a => TRACKED_ACRONYMS.Contains(a))
                                    .Distinct()
                                    .OrderBy(a => a, StringComparer.Ordinal)
                                    .ToArray();

            return acronyms.Length == 0 ? null : string.Concat(acronyms);
        }
    }
}
