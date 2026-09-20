// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Realms;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// A star rating calculated for a beatmap with a specific combination of mods applied,
    /// persisted for a small fixed set of tracked mod combinations.
    /// </summary>
    public class ModStarRating : EmbeddedObject
    {
        /// <summary>
        /// The mod combination this rating was calculated for, encoded as mod acronyms
        /// sorted alphabetically and concatenated (e.g. "DTHD" for DoubleTime + Hidden).
        /// Only mods in their default configuration are tracked.
        /// </summary>
        public string Mods { get; set; } = string.Empty;

        /// <summary>
        /// The star rating of the beatmap with <see cref="Mods"/> applied, calculated for the beatmap's own ruleset.
        /// </summary>
        public double StarRating { get; set; }
    }
}
