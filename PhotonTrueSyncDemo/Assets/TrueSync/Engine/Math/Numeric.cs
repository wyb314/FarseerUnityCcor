using System;
using System.Collections.Generic;

namespace TrueSync
{
    public static class Numeric
    {
        public static bool AreEqual(FP value1, FP value2, FP epsilon)
        {
            if (epsilon <= FP.Zero)
                throw new ArgumentOutOfRangeException("epsilon", "Epsilon value must be greater than 0.");

            // Infinity values have to be handled carefully because the check with the epsilon tolerance
            // does not work there. Check for equality in case they are infinite:
            if (value1 == value2)
                return true;

            FP delta = value1 - value2;
            return (-epsilon < delta) && (delta < epsilon);
        }

        public static bool IsZero(FP value, FP epsilon)
        {
            if (epsilon <= FP.Zero)
                throw new ArgumentOutOfRangeException("epsilon", "Epsilon value must be greater than 0.");

            return (-epsilon < value) && (value < epsilon);
        }

        private static FP _epsilonF = new FP(1)/new FP(100000);
        private static FP _epsilonFSquared = new FP(1) / new FP(1000000000);
        public static FP EpsilonF
        {
            get { return _epsilonF; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "The tolerance value must be greater than 0.");

                _epsilonF = value;
                _epsilonFSquared = value * value;
            }
        }

        public static FP EpsilonFSquared
        {
            get { return _epsilonFSquared; }
        }
    }
}
