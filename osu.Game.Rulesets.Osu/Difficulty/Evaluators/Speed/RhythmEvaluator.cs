// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators.Speed
{
    public static class RhythmEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current, OsuDifficultyConstants tuning)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var currObj = (OsuDifficultyHitObject)current;
            double surprise = currObj.CtwSurprise;

            // Slider transitions produce perceived rhythm changes that are easier to execute
            if (currObj.BaseObject is Slider || (current.Index > 0 && current.Previous(0).BaseObject is Slider))
                surprise *= tuning.RhythmSliderNerf;

            return 1.0 + tuning.RhythmOverallScale * surprise;
        }
    }
}
