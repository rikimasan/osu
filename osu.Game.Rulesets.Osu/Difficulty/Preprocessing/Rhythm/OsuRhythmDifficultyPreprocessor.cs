// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing.Rhythm
{
    public static class OsuRhythmDifficultyPreprocessor
    {
        public static void ProcessAndAssign(List<DifficultyHitObject> objects, OsuDifficultyConstants tuning)
        {
            var notes = collectNotes(objects);

            if (notes.Count == 0)
                return;

            var clusters = buildClusters(notes, tuning);

            double[] paritySurprises = scoreParity(clusters, tuning);
            double[] gapSurprises = scoreGapRatio(clusters, tuning);
            double[] internalSurprises = scoreInternalRatio(clusters, tuning);

            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];

                cluster[0].CtwSurprise = paritySurprises[i] + gapSurprises[i] + internalSurprises[i];
                cluster[0].CtwParitySurprise = paritySurprises[i];
                cluster[0].CtwGapSurprise = gapSurprises[i];
                cluster[0].CtwInternalSurprise = internalSurprises[i];
                cluster[0].ClusterSize = cluster.Count;

                for (int j = 1; j < cluster.Count; j++)
                    cluster[j].CtwSurprise = 0;
            }

            for (int i = 0; i < clusters.Count; i++)
            {
                foreach (var note in clusters[i])
                    note.ClusterIndices.Add(i);
            }
        }

        private static double[] scoreParity(List<List<OsuDifficultyHitObject>> clusters, OsuDifficultyConstants tuning)
        {
            var ctw = new ContextTreeWeighting(tuning.CtwMaxDepth, 2);
            var surprises = new double[clusters.Count];

            for (int i = 0; i < clusters.Count; i++)
            {
                int sym = clusters[i].Count % 2;
                surprises[i] = ctw.Update(sym);
            }

            return surprises;
        }

        private static double[] scoreGapRatio(List<List<OsuDifficultyHitObject>> clusters, OsuDifficultyConstants tuning)
        {
            var ctw = new ContextTreeWeighting(tuning.CtwMaxDepth, RhythmSymbolQuantizer.RATIO_BIN_COUNT);
            var surprises = new double[clusters.Count];
            double prevGap = 0;

            for (int i = 0; i < clusters.Count; i++)
            {
                double gap = Math.Max(clusters[i][0].LastObjectEndDeltaTime, 1e-7);
                double epsilon = clusters[i][0].HitWindow(HitResult.Great) * tuning.CtwEpsilonFactor;

                int sym = RhythmSymbolQuantizer.QuantizeRatio(gap, prevGap > 0 ? prevGap : gap, epsilon);
                surprises[i] = ctw.Update(sym);

                prevGap = gap;
            }

            return surprises;
        }

        private static double[] scoreInternalRatio(List<List<OsuDifficultyHitObject>> clusters, OsuDifficultyConstants tuning)
        {
            var ctw = new ContextTreeWeighting(tuning.CtwMaxDepth, RhythmSymbolQuantizer.RATIO_BIN_COUNT);
            var surprises = new double[clusters.Count];
            double prevInternalDelta = 0;

            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];
                double internalDelta = cluster.Count > 1 ? averageInternalDelta(cluster) : 0;

                int sym;

                if (cluster.Count <= 1 || prevInternalDelta <= 0)
                    sym = RhythmSymbolQuantizer.RATIO_BIN_COUNT / 2;
                else
                {
                    double epsilon = cluster[0].HitWindow(HitResult.Great) * tuning.CtwEpsilonFactor;
                    sym = RhythmSymbolQuantizer.QuantizeRatio(internalDelta, prevInternalDelta, epsilon);
                }

                surprises[i] = ctw.Update(sym);

                if (cluster.Count > 1)
                    prevInternalDelta = internalDelta;
            }

            return surprises;
        }

        private static double averageInternalDelta(List<OsuDifficultyHitObject> cluster)
        {
            double sum = 0;

            for (int i = 1; i < cluster.Count; i++)
                sum += Math.Max(cluster[i].DeltaTime, 1e-7);

            return sum / (cluster.Count - 1);
        }

        private static List<OsuDifficultyHitObject> collectNotes(List<DifficultyHitObject> objects)
        {
            var notes = new List<OsuDifficultyHitObject>();

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = (OsuDifficultyHitObject)objects[i];

                if (obj.BaseObject is not Spinner)
                    notes.Add(obj);
            }

            return notes;
        }

        private static List<List<OsuDifficultyHitObject>> buildClusters(List<OsuDifficultyHitObject> notes, OsuDifficultyConstants tuning)
        {
            var clusters = new List<List<OsuDifficultyHitObject>>();

            if (notes.Count == 0)
                return clusters;

            int lastCovered = -1;

            for (int i = 1; i < notes.Count;)
            {
                double delta = Math.Max(notes[i].DeltaTime, 1e-7);
                double epsilon = notes[i].HitWindow(HitResult.Great) * tuning.CtwEpsilonFactor;

                int end = i;

                while (end + 1 < notes.Count && Math.Abs(Math.Max(notes[end + 1].DeltaTime, 1e-7) - delta) < epsilon)
                    end++;

                if (end > i)
                {
                    // Emit singlets for uncovered notes before this cluster.
                    for (int k = Math.Max(lastCovered + 1, 0); k < i - 1; k++)
                        clusters.Add(new List<OsuDifficultyHitObject> { notes[k] });

                    var cluster = new List<OsuDifficultyHitObject>();

                    for (int j = i - 1; j <= end; j++)
                        cluster.Add(notes[j]);

                    clusters.Add(cluster);
                    lastCovered = end;
                }

                i = end + 1;
            }

            // Emit singlets for remaining uncovered notes.
            for (int k = Math.Max(lastCovered + 1, 0); k < notes.Count; k++)
                clusters.Add(new List<OsuDifficultyHitObject> { notes[k] });

            return clusters;
        }
    }
}
