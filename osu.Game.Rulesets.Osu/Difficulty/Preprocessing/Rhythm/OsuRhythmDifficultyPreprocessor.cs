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
            var ctw = new ContextTreeWeighting(tuning.CtwMaxDepth, RhythmSymbolQuantizer.ALPHABET_SIZE);

            for (int i = 0; i < objects.Count; i++)
            {
                var currObj = (OsuDifficultyHitObject)objects[i];

                if (currObj.BaseObject is Spinner)
                    continue;

                double currDelta = Math.Max(currObj.DeltaTime, 1e-7);
                double prevDelta = i > 0 ? Math.Max(objects[i - 1].DeltaTime, 1e-7) : currDelta;

                double epsilon = currObj.HitWindow(HitResult.Great) * tuning.CtwEpsilonFactor;

                var nextObj = i + 1 < objects.Count ? (OsuDifficultyHitObject)objects[i + 1] : null;
                double doubletapness = currObj.GetDoubletapness(nextObj);

                int symbol = RhythmSymbolQuantizer.Quantize(currDelta, prevDelta, epsilon, doubletapness, tuning.CtwDoubletapThreshold);

                double surprise = ctw.Update(symbol);

                // Normalize raw surprise into [0, 1] — ln(K) is maximum surprise for a uniform distribution
                double logK = Math.Log(RhythmSymbolQuantizer.ALPHABET_SIZE);
                currObj.CtwSurprise = DifficultyCalculationUtils.Smoothstep(surprise / logK, 0.05, 0.8);
            }
        }
    }
}
