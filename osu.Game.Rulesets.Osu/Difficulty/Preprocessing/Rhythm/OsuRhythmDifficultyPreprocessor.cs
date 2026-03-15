// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
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

            var ctw = new ContextTreeWeighting(tuning.CtwMaxDepth, RhythmSymbolQuantizer.ALPHABET_SIZE);
            double logK = Math.Log(RhythmSymbolQuantizer.ALPHABET_SIZE);

            for (int i = 0; i < notes.Count; i++)
            {
                var currObj = notes[i];

                double currDelta = Math.Max(currObj.DeltaTime, 1e-7);
                double prevDelta = i > 0 ? Math.Max(notes[i - 1].DeltaTime, 1e-7) : currDelta;

                double epsilon = currObj.HitWindow(HitResult.Great) * tuning.CtwEpsilonFactor;

                var nextObj = i + 1 < notes.Count ? notes[i + 1] : null;
                double doubletapness = currObj.GetDoubletapness(nextObj);

                int symbol = RhythmSymbolQuantizer.Quantize(currDelta, prevDelta, epsilon, doubletapness, tuning.CtwDoubletapThreshold);

                double surprise = ctw.Update(symbol);

                currObj.CtwSurprise = DifficultyCalculationUtils.Smoothstep(surprise / logK, 0.05, 0.8);
            }
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
            var cluster = new List<OsuDifficultyHitObject> { notes[0] };
            double firstInternalDelta = 0;

            for (int i = 1; i < notes.Count; i++)
            {
                double delta = Math.Max(notes[i].DeltaTime, 1e-7);
                double epsilon = cluster[0].HitWindow(HitResult.Great) * tuning.CtwEpsilonFactor;

                bool joinCluster;

                if (cluster.Count == 1)
                    joinCluster = delta < Math.Max(cluster[0].DeltaTime, 1e-7) - epsilon;
                else
                    joinCluster = Math.Abs(delta - firstInternalDelta) < epsilon;

                if (joinCluster)
                {
                    cluster.Add(notes[i]);

                    if (cluster.Count == 2)
                        firstInternalDelta = delta;
                }
                else
                {
                    clusters.Add(cluster);
                    cluster = new List<OsuDifficultyHitObject> { notes[i] };
                    firstInternalDelta = 0;
                }
            }

            clusters.Add(cluster);
            return clusters;
        }
    }
}
