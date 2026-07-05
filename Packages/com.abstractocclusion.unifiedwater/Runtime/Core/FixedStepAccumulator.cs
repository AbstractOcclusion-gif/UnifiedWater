using System;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Turns variable frame time into a whole number of fixed-size simulation steps, so the sim
    /// advances identically regardless of render framerate. Real delta time is accumulated; each
    /// <see cref="Advance"/> returns how many fixed steps are now due and keeps the sub-step
    /// remainder for next time. The step count is capped so one very long frame cannot schedule an
    /// unbounded catch-up; when the cap is hit the backlog is dropped rather than carried, which
    /// avoids a spiral where the sim never catches up and every frame stays pinned at the cap.
    /// </summary>
    internal sealed class FixedStepAccumulator
    {
        private readonly float _fixedDeltaTime;
        private readonly int _maxStepsPerAdvance;
        private float _accumulated;

        internal FixedStepAccumulator(float fixedDeltaTime, int maxStepsPerAdvance)
        {
            if (fixedDeltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fixedDeltaTime), fixedDeltaTime, "Fixed timestep must be positive.");
            }

            if (maxStepsPerAdvance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxStepsPerAdvance), maxStepsPerAdvance, "Max steps must be positive.");
            }

            _fixedDeltaTime = fixedDeltaTime;
            _maxStepsPerAdvance = maxStepsPerAdvance;
        }

        internal int Advance(float realDeltaTime)
        {
            if (realDeltaTime <= 0f)
            {
                return 0;
            }

            _accumulated += realDeltaTime;

            int dueSteps = (int)(_accumulated / _fixedDeltaTime);
            if (dueSteps <= 0)
            {
                return 0;
            }

            if (dueSteps >= _maxStepsPerAdvance)
            {
                // Cap and drop the backlog so a long stall does not pin every future frame at the cap.
                _accumulated = 0f;
                return _maxStepsPerAdvance;
            }

            _accumulated -= dueSteps * _fixedDeltaTime;
            return dueSteps;
        }
    }
}
