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
        private readonly struct RhythmEvent
        {
            public readonly double Time;
            public readonly double Delta;
            public readonly double HitWindow;
            public readonly OsuDifficultyHitObject Source;

            public RhythmEvent(double time, double delta, double hitWindow, OsuDifficultyHitObject source)
            {
                Time = time;
                Delta = delta;
                HitWindow = hitWindow;
                Source = source;
            }
        }

        public static void ProcessAndAssign(List<DifficultyHitObject> objects, OsuDifficultyConstants tuning)
        {
            var events = collectEvents(objects);

            if (events.Count == 0)
                return;

            var clusters = buildClusters(events, tuning);

            double[] paritySurprises = scoreParity(clusters, tuning);
            double[] gapSurprises = scoreGapRatio(clusters, tuning);
            double[] internalSurprises = scoreInternalRatio(clusters, tuning);

            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];

                cluster[0].Source.CtwSurprise = paritySurprises[i] + gapSurprises[i] + internalSurprises[i];
                cluster[0].Source.CtwParitySurprise = paritySurprises[i];
                cluster[0].Source.CtwGapSurprise = gapSurprises[i];
                cluster[0].Source.CtwInternalSurprise = internalSurprises[i];
                cluster[0].Source.ClusterSize = cluster.Count;

                for (int j = 1; j < cluster.Count; j++)
                    cluster[j].Source.CtwSurprise = 0;
            }

            for (int i = 0; i < clusters.Count; i++)
            {
                foreach (var evt in clusters[i])
                    evt.Source.ClusterIndices.Add(i);
            }
        }

        private static double[] scoreParity(List<List<RhythmEvent>> clusters, OsuDifficultyConstants tuning)
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

        private static double[] scoreGapRatio(List<List<RhythmEvent>> clusters, OsuDifficultyConstants tuning)
        {
            var ctw = new ContextTreeWeighting(tuning.CtwMaxDepth, RhythmSymbolQuantizer.RATIO_BIN_COUNT);
            var surprises = new double[clusters.Count];
            double prevGap = 0;

            for (int i = 0; i < clusters.Count; i++)
            {
                double gap = Math.Max(clusters[i][0].Source.LastObjectEndDeltaTime, 1e-7);
                double epsilon = clusters[i][0].HitWindow * tuning.CtwEpsilonFactor;

                int sym = RhythmSymbolQuantizer.QuantizeRatio(gap, prevGap > 0 ? prevGap : gap, epsilon);
                surprises[i] = ctw.Update(sym);

                prevGap = gap;
            }

            return surprises;
        }

        private static double[] scoreInternalRatio(List<List<RhythmEvent>> clusters, OsuDifficultyConstants tuning)
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
                    double epsilon = cluster[0].HitWindow * tuning.CtwEpsilonFactor;
                    sym = RhythmSymbolQuantizer.QuantizeRatio(internalDelta, prevInternalDelta, epsilon);
                }

                surprises[i] = ctw.Update(sym);

                if (cluster.Count > 1)
                    prevInternalDelta = internalDelta;
            }

            return surprises;
        }

        private static double averageInternalDelta(List<RhythmEvent> cluster)
        {
            double sum = 0;

            for (int i = 1; i < cluster.Count; i++)
                sum += Math.Max(cluster[i].Delta, 1e-7);

            return sum / (cluster.Count - 1);
        }

        private static List<RhythmEvent> collectEvents(List<DifficultyHitObject> objects)
        {
            var events = new List<RhythmEvent>();

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = (OsuDifficultyHitObject)objects[i];

                if (obj.BaseObject is Spinner)
                    continue;

                double hitWindow = obj.HitWindow(HitResult.Great);
                events.Add(new RhythmEvent(obj.StartTime, obj.DeltaTime, hitWindow, obj));
            }

            return events;
        }

        private static List<List<RhythmEvent>> buildClusters(List<RhythmEvent> events, OsuDifficultyConstants tuning)
        {
            var clusters = new List<List<RhythmEvent>>();

            if (events.Count == 0)
                return clusters;

            int lastCovered = -1;

            for (int i = 1; i < events.Count;)
            {
                double delta = Math.Max(events[i].Delta, 1e-7);
                double epsilon = events[i].HitWindow * tuning.CtwEpsilonFactor;

                int end = i;

                while (end + 1 < events.Count && Math.Abs(Math.Max(events[end + 1].Delta, 1e-7) - delta) < epsilon)
                    end++;

                if (end > i)
                {
                    for (int k = Math.Max(lastCovered + 1, 0); k < i - 1; k++)
                        clusters.Add(new List<RhythmEvent> { events[k] });

                    var cluster = new List<RhythmEvent>();

                    for (int j = i - 1; j <= end; j++)
                        cluster.Add(events[j]);

                    clusters.Add(cluster);
                    lastCovered = end;
                }

                i = end + 1;
            }

            for (int k = Math.Max(lastCovered + 1, 0); k < events.Count; k++)
                clusters.Add(new List<RhythmEvent> { events[k] });

            return clusters;
        }
    }
}
