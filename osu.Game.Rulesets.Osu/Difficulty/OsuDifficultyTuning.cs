// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public record OsuDifficultyTuning
    {
        public static OsuDifficultyTuning Default { get; } = new OsuDifficultyTuning();
        /// <summary>
        /// Scales the aim component of performance.
        /// </summary>
        public double AimPerformanceScale { get; init; } = 0.9268927097441173;

        /// <summary>
        /// Scales the speed component of performance.
        /// </summary>
        public double SpeedPerformanceScale { get; init; } = 0.9539584851372671;

        /// <summary>
        /// Scales the accuracy component of performance.
        /// </summary>
        public double AccuracyPerformanceScale { get; init; } = 1.011660095315058;

        /// <summary>
        /// Scales the flashlight component of performance.
        /// </summary>
        public double FlashlightPerformanceScale { get; init; } = 1.0;

        /// <summary>
        /// Scales the combined performance value.
        /// </summary>
        public double TotalPerformanceScale { get; init; } = 1.0441587573630162;

        public double AimSkillStrainScale { get; init; } = 0.966573979661007;
        public double SpeedSkillStrainScale { get; init; } = 0.9304808611199941;
        public double FlashlightSkillStrainScale { get; init; } = 1.0;

        public double AimWideAngleBonusScale { get; init; } = 2.229709962334549;
        public double AimAcuteAngleScale { get; init; } = 3.5116654707807626;
        public double AimSliderBonusScale { get; init; } = 1.2898769151968734;
        public double AimVelocityChangeBonusScale { get; init; } = 0.9777109855860147;
        public double AimWiggleBonusScale { get; init; } = 1.2335368034077572;

        public double FlashlightMaxOpacityBonusScale { get; init; } = 0.4;
        public double FlashlightHiddenBonusScale { get; init; } = 0.2;
        public double FlashlightMinVelocityScale { get; init; } = 0.5;
        public double FlashlightSliderBonusScale { get; init; } = 1.3;
        public double FlashlightMinAngleScale { get; init; } = 0.2;

        public int RhythmHistoryTimeMax { get; init; } = 5000; // 5 seconds
        public int RhythmHistoryObjectsMax { get; init; } = 32;
        public double RhythmOverallScale { get; init; } = 0.9668403981522815;
        public double RhythmRatioScale { get; init; } = 14.729845315920402;

        public double SpeedSingleSpacingThreshold { get; init; } = 127.15501783414545;
        public double SpeedMinBonusBpm { get; init; } = 201.94457874367282;
        public double SpeedBalancingFactor { get; init; } = 42.35321843996763;
        public double SpeedDistanceScale { get; init; } = 0.6585168776992504;
    }
}
