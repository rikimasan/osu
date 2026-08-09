// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Data.Sqlite;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// A persistent store of star ratings calculated for tracked mod combinations (see <see cref="ModStarRatingCombinations"/>),
    /// keyed by beatmap content hash rather than database identity.
    /// </summary>
    /// <remarks>
    /// Because keys are content-based, cached values survive realm resets or replacement wholesale, allowing
    /// <see cref="Database.BackgroundDataStoreProcessor"/> to backfill <see cref="BeatmapInfo.ModStarRatings"/>
    /// without recalculating difficulty.
    /// </remarks>
    public class ModStarRatingCache
    {
        private const string database_name = @"mod_star_ratings.db";

        private readonly Storage storage;

        public ModStarRatingCache(Storage storage)
        {
            this.storage = storage;

            try
            {
                prepareSchema();
            }
            catch (SqliteException)
            {
                purge();
                prepareSchema();
            }
        }

        /// <summary>
        /// Attempts to retrieve a cached star rating. A cached value only matches if it was calculated
        /// by the same difficulty calculator version.
        /// </summary>
        public bool TryGet(string beatmapMD5Hash, string rulesetShortName, string modsKey, int calculatorVersion, out double starRating)
        {
            starRating = default;

            try
            {
                using (var connection = getConnection())
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            @"SELECT `star_rating` FROM `ratings` WHERE `beatmap_md5` = $md5 AND `ruleset` = $ruleset AND `mods` = $mods AND `calculator_version` = $version";
                        command.Parameters.AddWithValue(@"$md5", beatmapMD5Hash);
                        command.Parameters.AddWithValue(@"$ruleset", rulesetShortName);
                        command.Parameters.AddWithValue(@"$mods", modsKey);
                        command.Parameters.AddWithValue(@"$version", calculatorVersion);

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                                return false;

                            starRating = reader.GetDouble(0);
                            return true;
                        }
                    }
                }
            }
            catch (SqliteException ex)
            {
                Logger.Log($@"Mod star rating cache lookup failed, purging cache: {ex}");
                purge();
                prepareSchema();
                return false;
            }
        }

        /// <summary>
        /// Stores a calculated star rating, replacing any value previously cached for the same
        /// beatmap / ruleset / mod combination (including values from older calculator versions).
        /// </summary>
        public void Store(string beatmapMD5Hash, string rulesetShortName, string modsKey, int calculatorVersion, double starRating)
        {
            try
            {
                using (var connection = getConnection())
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            @"INSERT OR REPLACE INTO `ratings` (`beatmap_md5`, `ruleset`, `mods`, `calculator_version`, `star_rating`) VALUES ($md5, $ruleset, $mods, $version, $star_rating)";
                        command.Parameters.AddWithValue(@"$md5", beatmapMD5Hash);
                        command.Parameters.AddWithValue(@"$ruleset", rulesetShortName);
                        command.Parameters.AddWithValue(@"$mods", modsKey);
                        command.Parameters.AddWithValue(@"$version", calculatorVersion);
                        command.Parameters.AddWithValue(@"$star_rating", starRating);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqliteException ex)
            {
                Logger.Log($@"Mod star rating cache write failed: {ex}");
            }
        }

        private void prepareSchema()
        {
            using (var connection = getConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"CREATE TABLE IF NOT EXISTS `ratings` (
                            `beatmap_md5` TEXT NOT NULL,
                            `ruleset` TEXT NOT NULL,
                            `mods` TEXT NOT NULL,
                            `calculator_version` INTEGER NOT NULL,
                            `star_rating` REAL NOT NULL,
                            PRIMARY KEY (`beatmap_md5`, `ruleset`, `mods`)
                        )";
                    command.ExecuteNonQuery();
                }
            }
        }

        private void purge()
        {
            try
            {
                // `SqliteConnection` pools connections per database file; the pools must be cleared
                // before the file can be deleted (see LocalCachedBeatmapMetadataSource for details).
                SqliteConnection.ClearAllPools();
                storage.Delete(database_name);
            }
            catch (Exception ex)
            {
                Logger.Log($@"Failed to purge mod star rating cache: {ex}");
            }
        }

        private SqliteConnection getConnection() =>
            new SqliteConnection(string.Concat(@"Data Source=", storage.GetFullPath(database_name, true)));
    }
}
