// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public record OsuDifficultyTuning
    {
        public static OsuDifficultyTuning Default { get; } = new OsuDifficultyTuning();

        /// <summary>
        /// Scales the aim skill difficulty output.
        /// </summary>
        public double AimSkillDifficultyScale { get; init; } = 1.0368075354588084;

        /// <summary>
        /// Scales the speed skill difficulty output.
        /// </summary>
        public double SpeedSkillDifficultyScale { get; init; } = 0.8431739571950971;

        /// <summary>
        /// Scales the flashlight skill difficulty output.
        /// </summary>
        public double FlashlightSkillDifficultyScale { get; init; } = 1.0;

        /// <summary>
        /// Scales the aim component of performance.
        /// </summary>
        public double AimPerformanceScale { get; init; } = 1.1002019795064326;

        /// <summary>
        /// Scales the speed component of performance.
        /// </summary>
        public double SpeedPerformanceScale { get; init; } = 0.8042306097762227;

        /// <summary>
        /// Scales the accuracy component of performance.
        /// </summary>
        public double AccuracyPerformanceScale { get; init; } = 0.8283444467023404;

        /// <summary>
        /// Scales the flashlight component of performance.
        /// </summary>
        public double FlashlightPerformanceScale { get; init; } = 1.0;

        /// <summary>
        /// Scales the combined performance value.
        /// </summary>
        public double TotalPerformanceScale { get; init; } = 0.9940206929812071;

        /// <summary>
        /// Scales the wide angle bonus applied during aim evaluation.
        /// </summary>
        public double AimWideAngleBonusScale { get; init; } = 1.4690309696747652;

        /// <summary>
        /// Scales the acute angle bonus applied during aim evaluation.
        /// </summary>
        public double AimAcuteAngleBonusScale { get; init; } = 2.71359679670386;

        /// <summary>
        /// Scales the slider bonus applied during aim evaluation when slider travel distance is considered.
        /// </summary>
        public double AimSliderBonusScale { get; init; } = 1.657583902536447;

        /// <summary>
        /// Scales the velocity change bonus applied during aim evaluation.
        /// </summary>
        public double AimVelocityChangeBonusScale { get; init; } = 0.7634828934553074;

        /// <summary>
        /// Scales the wiggle bonus applied during aim evaluation.
        /// </summary>
        public double AimWiggleBonusScale { get; init; } = 1.0112127447070165;

        /// <summary>
        /// Minimum BPM threshold before awarding additional speed bonus.
        /// </summary>
        public double SpeedMinSpeedBonusBpm { get; init; } = 203.65673621492996;

        /// <summary>
        /// Balancing factor applied when scaling speed bonus with BPM.
        /// </summary>
        public double SpeedBalancingFactor { get; init; } = 42.560990519318025;

        /// <summary>
        /// Maximum effective distance when computing speed bonus.
        /// </summary>
        public double SpeedSingleSpacingThreshold { get; init; } = OsuDifficultyHitObject.NORMALISED_DIAMETER * 1.28177429535715;

        /// <summary>
        /// Multiplier applied to the distance bonus component of speed evaluation.
        /// </summary>
        public double SpeedDistanceBonusScale { get; init; } = 0.8663783117125339;

        /// <summary>
        /// Maximum time range in milliseconds for rhythm history consideration.
        /// </summary>
        public double RhythmHistoryTimeMax { get; init; } = 4.75 * 1000;

        /// <summary>
        /// Maximum number of historical objects considered for rhythm calculation.
        /// </summary>
        public int RhythmHistoryObjectsMax { get; init; } = 32;

        /// <summary>
        /// Multiplier applied to the final rhythm difficulty value.
        /// </summary>
        public double RhythmOverallScale { get; init; } = 0.9612125144117571;

        /// <summary>
        /// Multiplier applied to rhythm ratio calculations.
        /// </summary>
        public double RhythmRatioMultiplier { get; init; } = 14.872228691100279;
    }
}
