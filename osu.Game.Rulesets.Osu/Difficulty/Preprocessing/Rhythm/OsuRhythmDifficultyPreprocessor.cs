// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
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
            public readonly OsuDifficultyHitObject? Source;

            public RhythmEvent(double time, double delta, double hitWindow, OsuDifficultyHitObject? source)
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

            buildAndScoreClusters(events, tuning);
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
            double prevTime = 0;

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = (OsuDifficultyHitObject)objects[i];

                if (obj.BaseObject is Spinner)
                    continue;

                double hitTime = obj.StartTime;
                double hitWindow = obj.HitWindow(HitResult.Great);

                events.Add(new RhythmEvent(hitTime, hitTime - prevTime, hitWindow, obj));
                prevTime = hitTime;

                if (obj.BaseObject is Slider slider)
                {
                    double releaseTime = Math.Max(
                        slider.StartTime + slider.Duration + SliderEventGenerator.TAIL_LENIENCY,
                        slider.StartTime + slider.Duration / 2);

                    if (releaseTime > hitTime)
                    {
                        double tailTime = obj.EndTime;
                        events.Add(new RhythmEvent(tailTime, tailTime - prevTime, hitWindow, null));
                        prevTime = tailTime;
                    }
                }
            }

            return events;
        }

        private static void buildAndScoreClusters(List<RhythmEvent> events, OsuDifficultyConstants tuning)
        {
            var clusters = new List<List<RhythmEvent>>();

            if (events.Count == 0)
                return;

            int lastCovered = -1;

            for (int i = 1; i < events.Count;)
            {
                if (events[i].Source == null)
                {
                    i++;
                    continue;
                }

                double delta = Math.Max(events[i].Delta, 1e-7);
                double epsilon = events[i].HitWindow * tuning.CtwEpsilonFactor;

                int end = i;

                while (end + 1 < events.Count && events[end + 1].Source != null && Math.Abs(Math.Max(events[end + 1].Delta, 1e-7) - delta) < epsilon)
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

            mergeDoubles(clusters);

            scoreClusters(clusters, tuning);
        }

        private static void scoreClusters(List<List<RhythmEvent>> clusters, OsuDifficultyConstants tuning)
        {
            var parityCTW = new ContextTreeWeighting(tuning.CtwMaxDepth, 2);
            var gapCTW = new ContextTreeWeighting(tuning.CtwMaxDepth, RhythmSymbolQuantizer.RATIO_BIN_COUNT);
            var internalCTW = new ContextTreeWeighting(tuning.CtwMaxDepth, RhythmSymbolQuantizer.RATIO_BIN_COUNT);

            double prevGap = 0;
            double prevInternalDelta = 0;

            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];

                double paritySurprise = parityCTW.Update(cluster.Count % 2);

                double gap = Math.Max(cluster[0].Delta, 1e-7);
                double epsilon = cluster[0].HitWindow * tuning.CtwEpsilonFactor;
                double gapSurprise = gapCTW.Update(RhythmSymbolQuantizer.QuantizeRatio(gap, prevGap > 0 ? prevGap : gap, epsilon));
                prevGap = gap;

                double internalDelta = cluster.Count > 1 ? averageInternalDelta(cluster) : 0;
                int internalSym = cluster.Count <= 1 || prevInternalDelta <= 0
                    ? RhythmSymbolQuantizer.RATIO_BIN_COUNT / 2
                    : RhythmSymbolQuantizer.QuantizeRatio(internalDelta, prevInternalDelta, epsilon);
                double internalSurprise = internalCTW.Update(internalSym);

                if (cluster.Count > 1)
                    prevInternalDelta = internalDelta;

                var data = new RhythmClusterData(i, cluster.Count, cluster[0].Time, cluster[^1].Time, paritySurprise, gapSurprise, internalSurprise);

                foreach (var evt in cluster)
                    evt.Source?.RhythmClusters.Add(data);
            }
        }
        private static void mergeDoubles(List<List<RhythmEvent>> clusters)
        {
            for (int i = 1; i < clusters.Count - 1; i++)
            {
                if (clusters[i].Count != 1)
                    continue;

                double epsilon = clusters[i][0].HitWindow;
                double prev = clusters[i - 1][^1].Delta;
                double curr = clusters[i][0].Delta;
                double next = clusters[i + 1][0].Delta;

                if (prev > curr + epsilon && next > curr + epsilon)
                {
                    clusters[i - 1].Add(clusters[i][0]);
                    clusters.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
