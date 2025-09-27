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
        // TODO: Tune these params later, 5x nested integral is a bit expensive xd
        // Thinking that I'll build slow and accurate now and performance can happen from an estimator trained off the accurate model
        private const int start_pos_steps = 6;
        private const int walk_press_steps = 6;
        private const int dash_press_steps = 6;
        private const int dash_release_steps = 6;
        private const int walk_release_steps = 6;

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

        // lo: previous start time
        // hi: if you dash the whole way, what's the latest time you can leave
        private static (double lo, double hi) calcWalkPressRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double startPos)
        {
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double minTravelDistance = Math.Abs(current.NormalizedX - prev.NormalizedX) - 1.0;
            return (prev.StartTime, current.StartTime - (minTravelDistance / effectiveDashSpeed));
        }

        // lo: you can't dash before you've started walking
        // hi: what is minimum amount of time spent dashing needed, subtract from current.StartTime
        private static (double lo, double hi) calcDashPressRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double startPos, double walkPressTime)
        {
            double effectiveWalkSpeed = hyperMultiplier * current.NormalizedWalkSpeed;
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double minDistanceTravel = Math.Abs(current.NormalizedX - prev.NormalizedX) - 1.0;
            double maxWalkDistance = effectiveWalkSpeed * (current.StartTime - walkPressTime);
            double extraDistance = Math.Max(0.0, minDistanceTravel - maxWalkDistance);
            double minDashTime = extraDistance / (effectiveDashSpeed - effectiveWalkSpeed);
            // bound above to not walk past far edge
            double maxDistanceTravel = Math.Abs(current.NormalizedX - startPos) + 1.0;
            double latestDashPressBeforeFarEdge = walkPressTime + maxDistanceTravel / effectiveWalkSpeed;

            return (walkPressTime, Math.Min(Math.Min(current.StartTime, current.StartTime - minDashTime), latestDashPressBeforeFarEdge));
        }

        // lo: if we're going to walk the rest of the way, what's the least dash time that still lets you walk within 1 radius of current
        // hi: reach the far edge
        private static (double lo, double hi) calcDashReleaseRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double startPos, double walkPressTime, double dashPressTime)
        {
            double sign = Math.Sign(current.NormalizedX - prev.NormalizedX);
            double effectiveWalkSpeed = hyperMultiplier * current.NormalizedWalkSpeed;
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double preDashDistanceWalked = effectiveWalkSpeed * (dashPressTime - walkPressTime);
            double currentPos = startPos + sign * preDashDistanceWalked;

            // Upper bound
            double maxDistanceTravel = Math.Abs(current.NormalizedX - currentPos) + 1.0;
            double maxDashDuration = maxDistanceTravel / effectiveDashSpeed;
            double hi = Math.Min(current.StartTime, dashPressTime + maxDashDuration);

            // Lower bound
            double closeDistanceFromStart = Math.Abs(current.NormalizedX - startPos) - 1.0;
            double walkOnlyDistance = effectiveWalkSpeed * (current.StartTime - walkPressTime);
            double extraNeeded = Math.Max(0.0, closeDistanceFromStart - walkOnlyDistance);
            double minDashDuration = extraNeeded / (effectiveDashSpeed - effectiveWalkSpeed);

            double loCandidate = dashPressTime + minDashDuration;
            // Ensure ordering; allow degenerate range if infeasible.
            double lo = Math.Max(dashPressTime, Math.Min(hi, loCandidate));

            return (lo, hi);
        }

        // lo: minimum to walk rest to the close edge
        // hi: reach the far edge and ensure excess time >= 0
        private static (double lo, double hi) calcWalkReleaseRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double startPos, double walkPressTime, double dashPressTime, double dashReleaseTime)
        {
            double sign = Math.Sign(current.NormalizedX - prev.NormalizedX);
            double effectiveWalkSpeed = hyperMultiplier * current.NormalizedWalkSpeed;
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double preDashDistanceWalked = effectiveWalkSpeed * (dashPressTime - walkPressTime);
            double dashedDistance = effectiveDashSpeed * (dashReleaseTime - dashPressTime);
            double currentPos = startPos + sign * (preDashDistanceWalked + dashedDistance);
            double minDistanceTravel = Math.Abs(current.NormalizedX - currentPos) - 1.0;
            double maxDistanceTravel = Math.Abs(current.NormalizedX - currentPos) + 1.0;
            double minTimeNeeded = minDistanceTravel / effectiveWalkSpeed;
            double maxTimeNeeded = maxDistanceTravel / effectiveWalkSpeed;

            return (dashReleaseTime + minTimeNeeded, Math.Min(current.StartTime, dashReleaseTime + maxTimeNeeded));
        }


        // TODO: Make not needing to take an action easier
        // Examples:
        // standing still should be no difficulty since it involves no inputs)
        // not needing to dash should reduce difficulty because it removes two of the measured inputs (hopefully accounted for automatically when summation isn't uniformly weighted)
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            var catchCurrent = (CatchDifficultyHitObject)current;
            var catchPrev = (CatchDifficultyHitObject)current.Previous(0);

            double hyperMultiplier = calcHyperMult(catchCurrent, catchPrev);

            // TODO: Start and end position will need to be weighted non-uniformly later through backprop
            var (startPosLo, startPosHi) = (catchPrev.NormalizedX - 1.0, catchPrev.NormalizedX + 1.0);
            double deltaStartPos = 2.0 / start_pos_steps;
            double difficulty = 0.0;
            for (double startPos = startPosLo; startPos < startPosHi; startPos += deltaStartPos)
            {
                var (walkPressLo, walkPressHi) = calcWalkPressRange(catchCurrent, catchPrev, hyperMultiplier, startPos);
                double deltaWalkPress = (walkPressHi - walkPressLo) / walk_press_steps;
                double difficultyWP = 0.0;
                for (double walkPressTime = walkPressLo; walkPressTime < walkPressHi; walkPressTime += deltaWalkPress)
                {
                    var (dashPressLo, dashPressHi) = calcDashPressRange(catchCurrent, catchPrev, hyperMultiplier, startPos, walkPressTime);
                    double deltaDashPress = (dashPressHi - dashPressLo) / dash_press_steps;
                    double difficultyDP = 0.0;
                    for (double dashPressTime = dashPressLo; dashPressTime < dashPressHi; dashPressTime += deltaDashPress)
                    {
                        var (dashReleaseLo, dashReleaseHi) = calcDashReleaseRange(catchCurrent, catchPrev, hyperMultiplier, startPos, walkPressTime, dashPressTime);
                        double deltaDashRelease = (dashReleaseHi - dashReleaseLo) / dash_release_steps;
                        double difficultyDR = 0.0;
                        for (double dashReleaseTime = dashReleaseLo; dashReleaseTime < dashReleaseHi; dashReleaseTime += deltaDashRelease)
                        {
                            var (walkReleaseLo, walkReleaseHi) = calcWalkReleaseRange(catchCurrent, catchPrev, hyperMultiplier, startPos, walkPressTime, dashPressTime, dashReleaseTime);
                            double deltaWalkRelease = (walkReleaseHi - walkReleaseLo) / walk_release_steps;
                            double difficultyWR = 0.0;
                            for (double walkReleaseTime = walkReleaseLo; walkReleaseTime < walkReleaseHi; walkReleaseTime += deltaWalkRelease)
                            {
                                difficultyWR += (current.StartTime - walkReleaseTime) * deltaWalkRelease;
                            }
                            difficultyDR += difficultyWR * deltaDashRelease;
                        }
                        difficultyDP += difficultyDR * deltaDashPress;
                    }
                    difficultyWP += walkPressTime * difficultyDP * deltaWalkPress;
                }
                difficulty += difficultyWP / deltaStartPos;
            }
            // TODO: Choose a better function here
            // Epistemic check, ask players if they feel per strain sorting is correct but exacerbated scaling
            return 1 / Math.Log(1.0 + difficulty);
        }
    }
}
