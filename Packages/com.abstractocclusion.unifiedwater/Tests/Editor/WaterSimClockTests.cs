using System;
using NUnit.Framework;

namespace AbstractOcclusion.UnifiedWater.Tests
{
    /// <summary>
    /// Pure-logic coverage of the fixed-timestep accumulator: it must convert variable frame time
    /// into whole fixed steps, bank the remainder, and cap a long frame without spiralling.
    /// </summary>
    public sealed class WaterSimClockTests
    {
        private const float FixedStep = 0.1f;
        private const int MaxSteps = 8;

        private static FixedStepAccumulator NewAccumulator() => new FixedStepAccumulator(FixedStep, MaxSteps);

        [Test]
        public void Advance_OneFixedStep_YieldsOneStep()
        {
            Assert.AreEqual(1, NewAccumulator().Advance(FixedStep));
        }

        [Test]
        public void Advance_FloorsToWholeSteps()
        {
            Assert.AreEqual(2, NewAccumulator().Advance(2.5f * FixedStep));
        }

        [Test]
        public void Advance_BanksRemainderAcrossCalls()
        {
            var accumulator = NewAccumulator();

            Assert.AreEqual(0, accumulator.Advance(0.4f * FixedStep));
            Assert.AreEqual(1, accumulator.Advance(0.7f * FixedStep)); // 0.4 + 0.7 = 1.1 steps
        }

        [Test]
        public void Advance_NonPositiveDelta_YieldsZero()
        {
            Assert.AreEqual(0, NewAccumulator().Advance(0f));
            Assert.AreEqual(0, NewAccumulator().Advance(-FixedStep));
        }

        [Test]
        public void Advance_ClampsToMax_AndDropsBacklog()
        {
            var accumulator = NewAccumulator();

            Assert.AreEqual(MaxSteps, accumulator.Advance(100f * FixedStep));
            // Backlog dropped: the next ordinary frame yields one step, not a pinned catch-up.
            Assert.AreEqual(1, accumulator.Advance(FixedStep));
        }

        [Test]
        public void Constructor_Rejects_NonPositiveArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStepAccumulator(0f, MaxSteps));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStepAccumulator(FixedStep, 0));
        }
    }
}
