// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Lists;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    public partial class OsuModRakeWhistle : Mod, IApplicableToDrawableRuleset<OsuHitObject>
    {
        public override string Name => @"Rake Whistle";
        public override string Acronym => @"RW";
        public override LocalisableString Description => @"Wouldn't want people to think you're a cheater.";
        public override double ScoreMultiplier => 1.0;
        public override ModType Type => ModType.Conversion;
        public override bool Ranked => true;
        public override Type[] IncompatibleMods => new[] { typeof(ModAutoplay), typeof(ModRelax), typeof(OsuModCinema) };

        private DrawableOsuRuleset ruleset = null!;

        private PeriodTracker nonGameplayPeriods = null!;

        private IFrameStableClock gameplayClock = null!;

        private readonly SortedList<PenalisedHitObject> penalisedHitObjects = new SortedList<PenalisedHitObject>((a, b) => a.HitWindow.End.CompareTo(b.HitWindow.End));

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            ruleset = (DrawableOsuRuleset)drawableRuleset;
            ruleset.Playfield.AttachInputInterceptor(new InputInterceptor(this));
            penalisedHitObjects.Clear();
            ruleset.RevertResult += result => penalisedHitObjects.RemoveAll(p => p.Result == result);

            var periods = new List<Period>();

            if (drawableRuleset.Objects.Any())
            {
                periods.Add(new Period(int.MinValue, getValidJudgementTime(ruleset.Objects.First()) - 1));

                foreach (BreakPeriod b in drawableRuleset.Beatmap.Breaks)
                    periods.Add(new Period(b.StartTime, getValidJudgementTime(ruleset.Objects.First(h => h.StartTime >= b.EndTime)) - 1));

                static double getValidJudgementTime(HitObject hitObject) => hitObject.StartTime - hitObject.HitWindows.WindowFor(HitResult.Meh);
            }

            nonGameplayPeriods = new PeriodTracker(periods);

            gameplayClock = drawableRuleset.FrameStableClock;
        }

        private DrawableHitCircle? getNextTappable()
        {
            foreach (var alive in ruleset.Playfield.HitObjectContainer.AliveObjects)
            {
                DrawableHitCircle? circle = alive switch
                {
                    DrawableSlider slider => slider.HeadCircle,
                    DrawableHitCircle hitCircle => hitCircle,
                    _ => null
                };

                if (circle != null && circle.Result?.HasResult != true)
                    return circle;
            }

            return null;
        }

        private bool isTapOnPenalisedHitObject(Vector2 screenSpacePosition)
        {
            double time = gameplayClock.CurrentTime;
            var playfieldPosition = ruleset.Playfield.HitObjectContainer.ToLocalSpace(screenSpacePosition);

            // Keep expired records for rewinds, but only examine windows which can still contain this press.
            for (int i = penalisedHitObjects.Count - 1; i >= 0; i--)
            {
                var penalised = penalisedHitObjects[i];

                if (time > penalised.HitWindow.End)
                    break;

                if (time < penalised.HitWindow.Start)
                    continue;

                var hitAreaPosition = Vector2Extensions.Transform(playfieldPosition, penalised.PlayfieldToHitArea);

                if (Vector2.DistanceSquared(hitAreaPosition, OsuHitObject.OBJECT_DIMENSIONS / 2) <= OsuHitObject.OBJECT_RADIUS * OsuHitObject.OBJECT_RADIUS)
                    return true;
            }

            return false;
        }

        private class PenalisedHitObject
        {
            public readonly JudgementResult Result;
            public readonly Period HitWindow;
            public readonly Matrix3 PlayfieldToHitArea;

            public PenalisedHitObject(DrawableHitCircle circle, OsuPlayfield playfield)
            {
                Result = circle.Result;

                double hitWindow = circle.HitObject.HitWindows.WindowFor(HitResult.Meh);
                HitWindow = new Period(circle.HitObject.StartTime - hitWindow, circle.HitObject.StartTime + hitWindow);

                // Capture the hit area before the miss animation or drawable pooling changes it.
                PlayfieldToHitArea = playfield.HitObjectContainer.DrawInfo.Matrix * circle.HitArea.DrawInfo.MatrixInverse;
            }
        }

        private partial class InputInterceptor : Component, IKeyBindingHandler<OsuAction>
        {
            private readonly OsuModRakeWhistle mod;

            public InputInterceptor(OsuModRakeWhistle mod)
            {
                this.mod = mod;
            }

            public bool OnPressed(KeyBindingPressEvent<OsuAction> e)
            {
                if (e.Action != OsuAction.LeftButton && e.Action != OsuAction.RightButton)
                    return false;

                if (mod.nonGameplayPeriods.IsInAny(mod.gameplayClock.CurrentTime))
                    return false;

                if (mod.gameplayClock.IsRewinding)
                    return false;

                var tappable = mod.getNextTappable();

                if (tappable == null)
                    return true;

                bool withinHitWindow = tappable.HitObject.HitWindows.ResultFor(mod.gameplayClock.CurrentTime - tappable.HitObject.StartTime).IsHit();

                if (withinHitWindow)
                {
                    tappable.HitArea.OnPressed(e);

                    if (tappable.Result?.HasResult == true)
                        return true;
                }

                if (mod.isTapOnPenalisedHitObject(e.ScreenSpaceMousePosition))
                    return true;

                var penalised = new PenalisedHitObject(tappable, mod.ruleset.Playfield);

                if (!withinHitWindow)
                    tappable.HitArea.OnPressed(e);

                if (tappable.Result?.HasResult != true)
                    tappable.MissForcefully();

                mod.penalisedHitObjects.Add(penalised);

                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<OsuAction> e)
            {
            }
        }
    }
}
