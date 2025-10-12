// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class MovementEvaluator
    {
        private const double eps = 1e-9;
        // TODO: Tune these params later, 4x nested integral is a bit expensive xd
        // Thinking that I'll build slow and accurate now and performance can happen from an estimator trained off the accurate model
        private const int start_pos_steps = 8;
        private const int walk_press_steps = 8;
        private const int dash_press_steps = 8;
        private const int dash_release_steps = 8;
        private const double catcher_radius = 1.0;

        private static IEnumerable<double> linspace(double lo, double hi, int samples)
        {
            double a = Math.Min(lo, hi);
            double b = Math.Max(lo, hi);

            if (samples <= 2) { yield return a; yield return b; yield break; }

            double step = (b - a) / (samples - 1);

            for (int i = 0; i < samples; i++)
                yield return a + i * step;
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

            double minTravelDistance = Math.Max(0.0, Math.Abs(current.NormalizedX - startPos) - catcher_radius);
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

            double lo = walkPressTime;
            double hi = Math.Min(current.StartTime - minDashTime, latestDashPressBeforeFarEdge);
            if (hi + eps < lo) throw new ArgumentException("dashPressHi should always be greater than or equal to dashPressLo");
            return (lo, hi);
        }

        // lo: if we're going to walk the rest of the way, what's the least dash time that still lets you walk within 1 radius of current
        // hi: reach the far edge
        private static (double lo, double hi) calcDashReleaseRange(CatchDifficultyHitObject current, double hyperMultiplier, double startPos, double walkPressTime, double dashPressTime)
        {
            double sign = Math.Sign(current.NormalizedX - startPos);
            double effectiveWalkSpeed = hyperMultiplier * current.NormalizedWalkSpeed;
            double effectiveDashSpeed = hyperMultiplier * current.NormalizedDashSpeed;

            double preDashDistanceWalked = effectiveWalkSpeed * (dashPressTime - walkPressTime);
            double currentPosAtDashPress = startPos + sign * preDashDistanceWalked;

            // Upper bound
            double maxDistanceTravel = sign * (current.NormalizedX - currentPosAtDashPress) + catcher_radius;
            double maxDashDuration = maxDistanceTravel / effectiveDashSpeed;
            double hi = Math.Min(current.StartTime, dashPressTime + maxDashDuration);

            // Lower bound
            double remainingDistanceFromDashPress = Math.Max(0.0, sign * (current.NormalizedX - currentPosAtDashPress) - catcher_radius);
            double walkCapacityFromDashPress = effectiveWalkSpeed * (current.StartTime - dashPressTime);
            double extraNeededAfterDashPress = Math.Max(0.0, remainingDistanceFromDashPress - walkCapacityFromDashPress);
            double minDashDuration = extraNeededAfterDashPress / (effectiveDashSpeed - effectiveWalkSpeed);

            double lo = Math.Max(dashPressTime, dashPressTime + minDashDuration);

            if (hi + eps < lo) throw new ArgumentException("dashReleaseHi should always be greater than or equal to dashReleaseLo");
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
            double minDistanceTravel = Math.Max(0.0, sign * (current.NormalizedX - currentPos) - catcher_radius);
            double maxDistanceTravel = sign * (current.NormalizedX - currentPos) + catcher_radius;
            double minTimeNeeded = minDistanceTravel / effectiveWalkSpeed;
            double maxTimeNeeded = maxDistanceTravel / effectiveWalkSpeed;
            double lo = Math.Max(dashReleaseTime, dashReleaseTime + minTimeNeeded);
            double hi = Math.Min(current.StartTime, dashReleaseTime + maxTimeNeeded);
            if (hi + eps < lo) throw new ArgumentException("walkReleaseHi should always be greater than or equal to walkReleaseLo");
            return (lo, hi);
        }


        public static double EvaluateDifficultyOf(DifficultyHitObject current, ref bool walk_passthrough, ref bool dash_passthrough)
        {
            var catchCurrent = (CatchDifficultyHitObject)current;
            var catchPrev = (CatchDifficultyHitObject)current.Previous(0);
            var catchNext = (CatchDifficultyHitObject)current.Next(0);
            if (catchPrev == null) return 0.0;
            if (catchNext == null) return 0.0;
            // TODO: Propagate start positions and key press passthrough across notes to minimize total difficulty
            // TODO: Non-uniform weighting across each distribution
            var (startPosLo, startPosHi) = (catchPrev.NormalizedX - catcher_radius, catchPrev.NormalizedX + catcher_radius);
            List<double> best = [double.NegativeInfinity];
            foreach (double startPos in linspace(startPosLo, startPosHi, start_pos_steps))
            {
                if (startPos >= catchCurrent.NormalizedX - catcher_radius && startPos <= catchCurrent.NormalizedX + catcher_radius)
                {
                    walk_passthrough = true;
                    dash_passthrough = true;
                    return 0.0;
                }
                double hyperMultiplier = calcHyperMult(catchCurrent, catchPrev, startPos);
                var (walkPressLo, walkPressHi) = calcWalkPressRange(catchCurrent, catchPrev, hyperMultiplier, startPos);
                double walkPressRange = walkPressHi - walkPressLo;
                if (walkPressRange <= 0) continue; // impossible starting position so we skip (need to verify this should actually happen)
                double wpLogP = Math.Log(1.0 - (1.0 / (eps + 1.0 + walkPressRange)));
                foreach (double walkPressTime in linspace(walkPressLo, walkPressHi, walk_press_steps))
                {
                    var (dashPressLo, dashPressHi) = calcDashPressRange(catchCurrent, hyperMultiplier, startPos, walkPressTime);
                    double dashPressRange = dashPressHi + eps - dashPressLo;
                    double dpLogP = Math.Log(1.0 - (1.0 / (eps + 1.0 + dashPressRange)));
                    foreach (double dashPressTime in linspace(dashPressLo, dashPressHi, dash_press_steps))
                    {
                        var (dashReleaseLo, dashReleaseHi) = calcDashReleaseRange(catchCurrent, hyperMultiplier, startPos, walkPressTime, dashPressTime);
                        double dashReleaseRange = dashReleaseHi + eps - dashReleaseLo;
                        // if you're holding dash through to the next note then there is no release timing
                        double drLogP = (dashReleaseHi + eps >= catchCurrent.StartTime && Math.Sign(catchCurrent.NormalizedX - startPos) == Math.Sign(catchNext.NormalizedX - catchCurrent.NormalizedX))
                            ? 0.0
                            : Math.Log(1.0 - (1.0 / (eps + 1.0 + dashReleaseRange)));
                        foreach (double dashReleaseTime in linspace(dashReleaseLo, dashReleaseHi, dash_release_steps))
                        {
                            var (walkReleaseLo, walkReleaseHi) = calcWalkReleaseRange(catchCurrent, hyperMultiplier, startPos, walkPressTime, dashPressTime, dashReleaseTime);
                            double walkReleaseRange = walkReleaseHi + eps - walkReleaseLo;
                            // if you're holding walk through to the next note then there is no release timing
                            double wrLogP = (walkReleaseHi + eps >= catchCurrent.StartTime && Math.Sign(catchCurrent.NormalizedX - startPos) == Math.Sign(catchNext.NormalizedX - catchCurrent.NormalizedX))
                                ? 0.0
                                : Math.Log(1.0 - (1.0 / (eps + 1.0 + walkReleaseRange)));
                            List<double> valid_input = [walk_passthrough ? 0.0 : wpLogP, dash_passthrough ? 0.0 : dpLogP, drLogP, wrLogP];
                            best = best.Sum() > valid_input.Sum() ? best : valid_input;
                        }
                    }
                }
            }
            walk_passthrough = best[2] >= 0.0 ? true : false;
            dash_passthrough = best[3] >= 0.0 ? true : false;
            // Using Max as a temporary fix for cheesable back and forths until I do backprop
            return Math.Log(0.914) / Math.Log(1.0 - Math.Exp(best.Sum()));
        }
    }
}
