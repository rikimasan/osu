// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    /// <summary>
    /// Computes the local entropy rate: the mean CTW surprise over the model's context window.
    /// This measures sustained rhythmic complexity rather than single-note surprise.
    /// </summary>
    public static class RhythmEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current, OsuDifficultyConstants tuning)
        {
            if (current.BaseObject is Spinner)
                return 0;

            int windowSize = tuning.CtwMaxDepth;
            double sum = 0;
            int count = 0;

            for (int i = 0; i < windowSize; i++)
            {
                DifficultyHitObject? obj = i == 0 ? current : (current.Index >= i ? current.Previous(i - 1) : null);

                if (obj == null)
                    break;

                if (obj.BaseObject is Spinner)
                    continue;

                sum += ((OsuDifficultyHitObject)obj).CtwSurprise;
                count++;
            }

            if (count == 0)
                return 0;

            return sum / count;
        }
    }
}
