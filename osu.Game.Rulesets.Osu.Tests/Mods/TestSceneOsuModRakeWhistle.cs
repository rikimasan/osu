// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    public partial class TestSceneOsuModRakeWhistle : OsuModTestScene
    {
        [Test]
        public void TestTapOnPenalisedObjectDoesNotMissNextObject() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRakeWhistle(),
            Autoplay = false,
            CreateBeatmap = () => createCircleBeatmap(
                (500, new Vector2(100)),
                (800, new Vector2(100))),
            ReplayFrames = createTapReplay(
                (400, new Vector2(300, 300), OsuAction.LeftButton),
                (500, new Vector2(100), OsuAction.LeftButton),
                (520, new Vector2(100), OsuAction.RightButton),
                (800, new Vector2(100), OsuAction.LeftButton)),
            PassCondition = () => hasCompletedWithResults(1, 1),
        });

        [TestCase(1000, 100, 100, 1)]
        [TestCase(1000, 130, 130, 2)]
        [TestCase(850, 100, 100, 2)]
        [TestCase(1150, 100, 100, 2)]
        public void TestTapOnPenalisedObjectRequiresOriginalHitAreaAndWindow(double tapTime, float x, float y, int expectedMisses) => CreateModTest(new ModTestData
        {
            Mod = new OsuModRakeWhistle(),
            Autoplay = false,
            CreateBeatmap = () => createCircleBeatmap(
                (500, new Vector2(400, 100)),
                (1000, new Vector2(100)),
                (1500, new Vector2(200, 100))),
            ReplayFrames = createTapReplay(
                (500, new Vector2(400, 100), OsuAction.LeftButton),
                (700, new Vector2(100), OsuAction.LeftButton),
                (tapTime, new Vector2(x, y), OsuAction.LeftButton),
                (1500, new Vector2(200, 100), OsuAction.LeftButton)),
            PassCondition = () => hasCompletedWithResults(expectedMisses, 3 - expectedMisses),
        });

        [Test]
        public void TestValidHitTakesPriorityOverTapOnPenalisedObject() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRakeWhistle(),
            Autoplay = false,
            CreateBeatmap = () => createCircleBeatmap(
                (500, new Vector2(100)),
                (600, new Vector2(100)),
                (1000, new Vector2(300, 100))),
            ReplayFrames = createTapReplay(
                (400, new Vector2(300, 300), OsuAction.LeftButton),
                (600, new Vector2(100), OsuAction.LeftButton),
                (620, new Vector2(100), OsuAction.RightButton),
                (1000, new Vector2(300, 100), OsuAction.LeftButton)),
            PassCondition = () => hasCompletedWithResults(1, 2),
        });

        [Test]
        public void TestTapsOnMultiplePenalisedObjectsDoNotMissNextObject() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRakeWhistle(),
            Autoplay = false,
            CreateBeatmap = () => createCircleBeatmap(
                (500, new Vector2(100)),
                (600, new Vector2(200, 100)),
                (1000, new Vector2(300, 100))),
            ReplayFrames = createTapReplay(
                (400, new Vector2(300, 300), OsuAction.LeftButton),
                (420, new Vector2(300, 300), OsuAction.LeftButton),
                (500, new Vector2(100), OsuAction.LeftButton),
                (600, new Vector2(200, 100), OsuAction.LeftButton),
                (1000, new Vector2(300, 100), OsuAction.LeftButton)),
            PassCondition = () => hasCompletedWithResults(2, 1),
        });

        [TestCase(350)]
        [TestCase(450)]
        public void TestRewind(double seekTime)
        {
            bool replayed = false;

            CreateModTest(new ModTestData
            {
                Mod = new OsuModRakeWhistle(),
                Autoplay = false,
                CreateBeatmap = () => createCircleBeatmap(
                    (500, new Vector2(100)),
                    (1000, new Vector2(200, 100))),
                ReplayFrames = createTapReplay(
                    (400, new Vector2(300, 300), OsuAction.LeftButton),
                    (500, new Vector2(100), OsuAction.LeftButton),
                    (1000, new Vector2(200, 100), OsuAction.LeftButton)),
                PassCondition = () => replayed && hasCompletedWithResults(1, 1),
            });

            AddUntilStep("first play completed", () => hasCompletedWithResults(1, 1));
            AddStep("rewind", () =>
            {
                Player.GameplayClockContainer.Stop();
                Player.Seek(seekTime);
            });
            AddUntilStep("rewind completed", () => Player.DrawableRuleset.FrameStableClock.CurrentTime == seekTime);
            AddAssert("penalty follows rewind", () => Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Miss), () => Is.EqualTo(seekTime < 400 ? 0 : 1));
            AddStep("replay", () =>
            {
                replayed = true;
                Player.GameplayClockContainer.Start();
            });
        }

        [Test]
        public void TestExtraPressDuringSliderMissesNextObject() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRakeWhistle(),
            Autoplay = false,
            CreateBeatmap = createSliderBeatmap,
            ReplayFrames = new List<ReplayFrame>
            {
                new OsuReplayFrame(500, new Vector2(100), OsuAction.LeftButton),
                new OsuReplayFrame(700, new Vector2(150, 100), OsuAction.LeftButton, OsuAction.RightButton),
                new OsuReplayFrame(701, new Vector2(150, 100), OsuAction.LeftButton),
                new OsuReplayFrame(900, new Vector2(200, 100)),
                new OsuReplayFrame(1500, new Vector2(300, 100), OsuAction.LeftButton),
                new OsuReplayFrame(1501, new Vector2(300, 100)),
            },
            PassCondition = () => hasCompletedWithResults(1, 1),
        });

        [Test]
        public void TestTapOnPenalisedSliderHeadDoesNotMissNextObject() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRakeWhistle(),
            Autoplay = false,
            CreateBeatmap = createSliderBeatmap,
            ReplayFrames = new List<ReplayFrame>
            {
                new OsuReplayFrame(400, new Vector2(300, 300), OsuAction.LeftButton),
                new OsuReplayFrame(401, new Vector2(300, 300)),
                new OsuReplayFrame(500, new Vector2(100), OsuAction.LeftButton),
                new OsuReplayFrame(900, new Vector2(200, 100)),
                new OsuReplayFrame(1500, new Vector2(300, 100), OsuAction.LeftButton),
                new OsuReplayFrame(1501, new Vector2(300, 100)),
            },
            PassCondition = () => hasCompletedWithResults(1, 1),
        });

        [Test]
        public void TestMisaimOnLaterObjectDoesNotRegisterHit() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRakeWhistle(),
            Autoplay = false,
            CreateBeatmap = () => createCircleBeatmap(
                (500, new Vector2(100)),
                (700, new Vector2(200, 100))),
            ReplayFrames = createTapReplay(
                (595, new Vector2(200, 100), OsuAction.LeftButton),
                (700, new Vector2(200, 100), OsuAction.LeftButton)),
            PassCondition = () => hasCompletedWithResults(1, 1),
        });

        [Test]
        public void TestPressBlockedByAlternateIsNotCountedAsExtra() => CreateModTest(new ModTestData
        {
            Mods = new Mod[] { new OsuModAlternate(), new OsuModRakeWhistle() },
            Autoplay = false,
            CreateBeatmap = () => createCircleBeatmap(
                (500, new Vector2(100)),
                (1000, new Vector2(200, 100))),
            ReplayFrames = createTapReplay(
                (500, new Vector2(100), OsuAction.LeftButton),
                (700, new Vector2(150, 100), OsuAction.LeftButton),
                (1000, new Vector2(200, 100), OsuAction.RightButton)),
            PassCondition = () => hasCompletedWithResults(0, 2),
        });

        private static Beatmap createCircleBeatmap(params (double startTime, Vector2 position)[] circles) => new Beatmap
        {
            Difficulty = new BeatmapDifficulty { OverallDifficulty = 5, CircleSize = 5 },
            HitObjects = circles.Select(circle => (HitObject)new HitCircle
            {
                StartTime = circle.startTime,
                Position = circle.position,
            }).ToList(),
        };

        private static Beatmap createSliderBeatmap() => new Beatmap
        {
            HitObjects = new List<HitObject>
            {
                new Slider
                {
                    StartTime = 500,
                    Position = new Vector2(100),
                    Path = new SliderPath(PathType.LINEAR, new[]
                    {
                        Vector2.Zero,
                        new Vector2(100, 0),
                    }),
                },
                new HitCircle
                {
                    StartTime = 1500,
                    Position = new Vector2(300, 100),
                },
            },
        };

        private static List<ReplayFrame> createTapReplay(params (double time, Vector2 position, OsuAction action)[] taps) => taps.SelectMany(tap => new ReplayFrame[]
        {
            new OsuReplayFrame(tap.time, tap.position, tap.action),
            new OsuReplayFrame(tap.time + 1, tap.position),
        }).ToList();

        private bool hasCompletedWithResults(int misses, int greats) => Player.ScoreProcessor.HasCompleted.Value
                                                                      && Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Miss) == misses
                                                                      && Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Great) == greats;
    }
}
