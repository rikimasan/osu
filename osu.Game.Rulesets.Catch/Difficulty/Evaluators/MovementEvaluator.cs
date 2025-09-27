// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class MovementEvaluator
    {
        // TODO: Tune these params later
        private const int start_pos_steps = 10;
        private const int walk_press_steps = 10;
        private const int dash_press_steps = 10;
        private const int dash_release_steps = 10;


        private const double v_walk = Catcher.BASE_WALK_SPEED;
        private const double v_dash = Catcher.BASE_DASH_SPEED;
        private static double calcHyperMult(CatchDifficultyHitObject current, CatchDifficultyHitObject prev)
        {

            if (prev.BaseObject.HyperDash)
            {
                double dx = Math.Abs(current.BaseObject.EffectiveX - prev.BaseObject.EffectiveX);
                double dt = Math.Max(1.0, current.DeltaTime - 1000.0 / 60.0);
                return dx / dt;
            }
            return 1.0;
        }

        private static (double lo, double hi) calcWalkPressRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier)
        {
            return (0.0, 0.0);
        }
        private static (double lo, double hi) calcDashPressRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double walkPressTime)
        {
            return (0.0, 0.0);
        }
        private static (double lo, double hi) calcDashReleaseRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double walkPressTime, double dashPressTime)
        {
            return (0.0, 0.0);
        }
        private static (double lo, double hi) calcWalkReleaseRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double walkPressTime, double dashPressTime, double dashReleaseTime)
        {
            return (0.0, 0.0);
        }
        public static double EvaluateDifficultyOf(DifficultyHitObject current, double catcherSpeedMultiplier)
        {
            var catchCurrent = (CatchDifficultyHitObject)current;
            var catchPrev = (CatchDifficultyHitObject)current.Previous(0);

            double hyperMultiplier = calcHyperMult(catchCurrent, catchPrev);

            // TODO: Start and end position will need to be weighted non-uniformly later through backprop
            var (startPosLo, startPosHi) = (catchPrev.NormalizedX - 1.0, catchPrev.NormalizedX + 1.0);
            double deltaStartPos = 2.0 / start_pos_steps;
            double difficulty = 0.0;
            for (double startPos = startPosLo; startPos <= startPosHi; startPos += deltaStartPos)
            {
                var (walkPressLo, walkPressHi) = calcWalkPressRange(catchCurrent, catchPrev, hyperMultiplier);
                double deltaWalkPress = (walkPressHi - walkPressLo) / walk_press_steps;
                double difficultyWP = 0.0;
                for (double walkPressTime = walkPressLo; walkPressTime <= walkPressHi; walkPressTime += deltaWalkPress)
                {
                    var (dashPressLo, dashPressHi) = calcDashPressRange(catchCurrent, catchPrev, hyperMultiplier, walkPressTime);
                    double deltaDashPress = (dashPressHi - dashPressLo) / dash_press_steps;
                    double difficultyDP = 0.0;
                    for (double dashPressTime = dashPressLo; dashPressTime <= dashPressHi; dashPressTime += deltaDashPress)
                    {
                        var (dashReleaseLo, dashReleaseHi) = calcDashReleaseRange(catchCurrent, catchPrev, hyperMultiplier, walkPressTime, dashPressTime);
                        double deltaDashRelease = (dashReleaseHi - dashReleaseLo) / dash_release_steps;
                        double difficultyDR = 0.0;
                        for (double dashReleaseTime = dashPressLo; dashReleaseTime <= dashReleaseHi; dashReleaseTime += deltaDashRelease)
                        {
                            var (walkReleaseLo, walkReleaseHi) = calcWalkReleaseRange(catchCurrent, catchPrev, hyperMultiplier, walkPressTime, dashPressTime, dashReleaseTime);
                            double difficultyWR = walkReleaseHi - walkReleaseLo;
                            difficultyDR += difficultyWR / deltaDashRelease;
                        }
                        difficultyDP += difficultyDR / deltaDashPress;
                    }
                    difficultyWP += difficultyDP / deltaWalkPress;
                }
                difficulty += difficultyWP / deltaStartPos;
            }

            return difficulty;
        }
    }
}
