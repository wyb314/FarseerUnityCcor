using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FarseerUnity.Base.FarseerPhysics
{
    public static class Numeric
    {
        public static bool AreEqual(float value1, float value2, float epsilon)
        {
            if (epsilon <= 0.0f)
                throw new ArgumentOutOfRangeException("epsilon", "Epsilon value must be greater than 0.");

            // Infinity values have to be handled carefully because the check with the epsilon tolerance
            // does not work there. Check for equality in case they are infinite:
            if (value1 == value2)
                return true;

            float delta = value1 - value2;
            return (-epsilon < delta) && (delta < epsilon);
        }
    }
}
