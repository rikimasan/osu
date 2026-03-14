// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Measures rhythmic complexity as total information content with a soft cap on length.
    /// Each object's difficulty is the local entropy rate (mean CTW surprise over context window).
    /// Final difficulty is the soft-capped sum: (Σ localEntropyRate)^γ.
    /// </summary>
    /// <remarks>
    /// If isolated rhythm spikes are underweighted, strain accumulation with decay could be layered
    /// on top of the local entropy rate to model cognitive load dissipating between rhythm-intensive sections.
    /// </remarks>
    public class Rhythm : Skill
    {
        private readonly OsuDifficultyConstants tuning;

        public Rhythm(Mod[] mods, OsuDifficultyConstants tuning)
            : base(mods)
        {
            this.tuning = tuning;
        }

        protected override double ProcessInternal(DifficultyHitObject current)
            => RhythmEvaluator.EvaluateDifficultyOf(current, tuning);

        public override double DifficultyValue()
            => Math.Pow(ObjectDifficulties.Where(d => d > 0).Sum(), tuning.RhythmLengthExponent);

        public static double DifficultyToPerformance(double difficulty) => 4.0 * Math.Pow(difficulty, 3.0);
    }
}
