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
    }
}
