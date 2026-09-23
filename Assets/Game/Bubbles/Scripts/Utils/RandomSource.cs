namespace SimulaAd.Bubbles
{
    /// <summary>
    /// Small pseudo-random generator (Park–Miller "minimal standard" LCG) done in double math,
    /// used instead of UnityEngine.Random, which is missing at runtime in Playworks builds.
    /// Fixed seeds, no clock/API dependency (System.DateTime.Millisecond doesn't exist in the
    /// Playworks runtime; seeding from it in field initializers made the web build load to black):
    /// the first run is always the same, and each replay continues the sequence, so boards vary
    /// between runs. Create one per consumer, lazily (not in field initializers); no static state.
    /// </summary>
    public class RandomSource
    {
        const double k_Modulus = 2147483647.0; // 2^31 - 1
        const double k_Multiplier = 16807.0;   // product stays < 2^53, exact in a double

        double m_State;

        public RandomSource(int seed)
        {
            double state = seed % (k_Modulus - 1.0);
            if (state < 0.0)
                state += k_Modulus - 1.0;
            m_State = state + 1.0; // must be in [1, modulus - 1]
        }

        /// <summary>Random float in [0, 1).</summary>
        public float Value()
        {
            m_State = (m_State * k_Multiplier) % k_Modulus;
            return (float)((m_State - 1.0) / (k_Modulus - 1.0));
        }

        /// <summary>Random float in [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        /// <summary>Random int in [min, maxExclusive), like UnityEngine.Random.Range(int, int).</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min)
                return min;

            // No float→int cast: (int)x compiles to Bridge.Int.clip32, missing in Playworks builds.
            // Walk the buckets comparing floats instead (spans here are tiny: colors, list sizes).
            float value = Value();
            float step = 1f / (maxExclusive - min);
            float threshold = step;
            int result = min;
            while (value >= threshold && result < maxExclusive - 1)
            {
                result++;
                threshold += step;
            }

            return result;
        }
    }
}
