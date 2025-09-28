// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class MovementEvaluator
    {
        // TODO: Tune these params later, 5x nested integral is a bit expensive xd
        // Thinking that I'll build slow and accurate now and performance can happen from an estimator trained off the accurate model
        private const int start_pos_steps = 10;
        private const int walk_press_steps = 5;
        private const int dash_press_steps = 5;
        private const int dash_release_steps = 5;
        private const int walk_release_steps = 5;
        private const double catcher_radius = 1.0;

        private static IEnumerable<double> linspace(double lo, double hi, int samples)
        {
            if (!double.IsFinite(lo) || !double.IsFinite(hi) || samples <= 0) yield break;

            double span = hi - lo;
            if (span <= 0) yield break;

            double inv = 1.0 / samples;
            for (int i = 0; i < samples; i++)
                yield return lo + (i + 0.5) * span * inv;
        }

        private static double calcHyperMult(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double startPos)
        {
            if (!prev.BaseObject.HyperDash) return 1.0;
            double dx = Math.Abs(current.NormalizedX - startPos);
            double dt = Math.Max(1.0, current.DeltaTime - 1000.0 / 60.0);
            double vReq = dx / dt;
            return Math.Max(1.0, vReq / current.NormalizedDashSpeed);
        }

        // lo: previous start time
        // hi: if you dash the whole way, what's the latest time you can leave
        private static (double lo, double hi) calcWalkPressRange(CatchDifficultyHitObject current, CatchDifficultyHitObject prev, double hyperMultiplier, double startPos)
        {
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double minTravelDistance = Math.Abs(current.NormalizedX - startPos) - catcher_radius;
            return (prev.StartTime, current.StartTime - (minTravelDistance / effectiveDashSpeed));
        }

        // lo: you can't dash before you've started walking
        // hi: what is minimum amount of time spent dashing needed, subtract from current.StartTime
        private static (double lo, double hi) calcDashPressRange(CatchDifficultyHitObject current, double hyperMultiplier, double startPos, double walkPressTime)
        {
            double effectiveWalkSpeed = hyperMultiplier * current.NormalizedWalkSpeed;
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double minTravelDistance = Math.Abs(current.NormalizedX - startPos) - catcher_radius;
            double maxWalkDistance = effectiveWalkSpeed * (current.StartTime - walkPressTime);
            double extraDistance = Math.Max(0.0, minTravelDistance - maxWalkDistance);
            double minDashTime = extraDistance / (effectiveDashSpeed - effectiveWalkSpeed);
            // bound above to not walk past far edge
            double maxDistanceTravel = Math.Abs(current.NormalizedX - startPos) + catcher_radius;
            double latestDashPressBeforeFarEdge = walkPressTime + maxDistanceTravel / effectiveWalkSpeed;

            return (walkPressTime, Math.Min(current.StartTime - minDashTime, latestDashPressBeforeFarEdge));
        }

        // lo: if we're going to walk the rest of the way, what's the least dash time that still lets you walk within 1 radius of current
        // hi: reach the far edge
        private static (double lo, double hi) calcDashReleaseRange(CatchDifficultyHitObject current, double hyperMultiplier, double startPos, double walkPressTime, double dashPressTime)
        {
            double sign = Math.Sign(current.NormalizedX - startPos);
            double effectiveWalkSpeed = hyperMultiplier * current.NormalizedWalkSpeed;
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double preDashDistanceWalked = effectiveWalkSpeed * (dashPressTime - walkPressTime);
            double currentPos = startPos + sign * preDashDistanceWalked;

            // Upper bound
            double maxDistanceTravel = Math.Abs(current.NormalizedX - currentPos) + catcher_radius;
            double maxDashDuration = maxDistanceTravel / effectiveDashSpeed;
            double hi = Math.Min(current.StartTime, dashPressTime + maxDashDuration);

            // Lower bound
            double closeDistanceFromStart = Math.Abs(current.NormalizedX - startPos) - catcher_radius;
            double walkOnlyDistance = effectiveWalkSpeed * (current.StartTime - walkPressTime);
            double extraNeeded = Math.Max(0.0, closeDistanceFromStart - walkOnlyDistance);
            double minDashDuration = extraNeeded / (effectiveDashSpeed - effectiveWalkSpeed);

            double loCandidate = dashPressTime + minDashDuration;
            double lo = Math.Max(dashPressTime, Math.Min(hi, loCandidate));

            return (lo, hi);
        }

        // lo: minimum to walk rest to the close edge
        // hi: reach the far edge and ensure excess time >= 0
        private static (double lo, double hi) calcWalkReleaseRange(CatchDifficultyHitObject current, double hyperMultiplier, double startPos, double walkPressTime, double dashPressTime, double dashReleaseTime)
        {
            double sign = Math.Sign(current.NormalizedX - startPos);
            double effectiveWalkSpeed = hyperMultiplier * current.NormalizedWalkSpeed;
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double preDashDistanceWalked = effectiveWalkSpeed * (dashPressTime - walkPressTime);
            double dashedDistance = effectiveDashSpeed * (dashReleaseTime - dashPressTime);
            double currentPos = startPos + sign * (preDashDistanceWalked + dashedDistance);
            double minDistanceTravel = Math.Abs(current.NormalizedX - currentPos) - catcher_radius;
            double maxDistanceTravel = Math.Abs(current.NormalizedX - currentPos) + catcher_radius;
            double minTimeNeeded = minDistanceTravel / effectiveWalkSpeed;
            double maxTimeNeeded = maxDistanceTravel / effectiveWalkSpeed;

            return (dashReleaseTime + minTimeNeeded, Math.Min(current.StartTime, dashReleaseTime + maxTimeNeeded));
        }


        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            var catchCurrent = (CatchDifficultyHitObject)current;
            var catchPrev = (CatchDifficultyHitObject)current.Previous(0);
            if (catchPrev == null) return 0.0;

            // TODO: Start and end position will need to be weighted non-uniformly later through backprop
            var (startPosLo, startPosHi) = (catchPrev.NormalizedX - catcher_radius, catchPrev.NormalizedX + catcher_radius);
            double deltaStartPos = 2.0 / start_pos_steps;
            List<double> difficulty = new List<double>();
            for (double startPos = startPosLo; startPos < startPosHi; startPos += deltaStartPos)
            {
                if (startPos >= catchCurrent.NormalizedX - catcher_radius && startPos <= catchCurrent.NormalizedX + catcher_radius) continue;
                double hyperMultiplier = calcHyperMult(catchCurrent, catchPrev, startPos);

                var (walkPressLo, walkPressHi) = calcWalkPressRange(catchCurrent, catchPrev, hyperMultiplier, startPos);
                double deltaWalkPress = (walkPressHi - walkPressLo) / walk_press_steps;
                double inputVolumeWP = 0.0;
                foreach (double walkPressTime in linspace(walkPressLo, walkPressHi, walk_press_steps))
                {
                    var (dashPressLo, dashPressHi) = calcDashPressRange(catchCurrent, hyperMultiplier, startPos, walkPressTime);
                    double deltaDashPress = (dashPressHi - dashPressLo) / dash_press_steps;
                    double inputVolumeDP = 0.0;
                    foreach (double dashPressTime in linspace(dashPressLo, dashPressHi, dash_press_steps))
                    {
                        // TODO: make it so if you don't have to release it doesn't add difficulty for the release timing
                        var (dashReleaseLo, dashReleaseHi) = calcDashReleaseRange(catchCurrent, hyperMultiplier, startPos, walkPressTime, dashPressTime);
                        double deltaDashRelease = (dashReleaseHi - dashReleaseLo) / dash_release_steps;
                        double inputVolumeDR = 0.0;
                        foreach (double dashReleaseTime in linspace(dashReleaseLo, dashReleaseHi, dash_release_steps))
                        {
                            var (walkReleaseLo, walkReleaseHi) = calcWalkReleaseRange(catchCurrent, hyperMultiplier, startPos, walkPressTime, dashPressTime, dashReleaseTime);
                            double deltaWalkRelease = (walkReleaseHi - walkReleaseLo) / walk_release_steps;
                            double inputVolumeWR = 0.0;
                            foreach (double walkReleaseTime in linspace(walkReleaseLo, walkReleaseHi, walk_release_steps))
                            {
                                inputVolumeWR += (catchCurrent.StartTime - walkReleaseTime) * deltaWalkRelease;
                            }
                            inputVolumeDR += inputVolumeWR * deltaDashRelease;
                        }
                        inputVolumeDP += inputVolumeDR * deltaDashPress;
                    }
                    inputVolumeWP += (walkPressTime - catchPrev.StartTime) * inputVolumeDP * deltaWalkPress;
                }
                // TODO: Choose a better function here
                // Epistemic check, ask players if they feel per strain sorting is correct but exacerbated scaling
                difficulty.Add(1.0 / Math.Log(1e-6 + 1.0 + inputVolumeWP * deltaStartPos));
            }
            return difficulty.Count > 0 ? difficulty.Min() : 0.0;
        }
    }
}
