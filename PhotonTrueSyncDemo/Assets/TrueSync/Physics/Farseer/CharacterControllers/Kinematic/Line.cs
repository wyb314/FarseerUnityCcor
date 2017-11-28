using System;
using System.Collections.Generic;

namespace TrueSync.Physics2D.Specialized
{
    public struct Line : IEquatable<Line>
    {

        public TSVector2 positoin;

        public TSVector2 Normal;

        public Line(TSVector2 normal, TSVector2 positoin)
        {
            this.Normal = normal;
            this.positoin = positoin;
        }

        public bool Equals(Line other)
        {
            return Equals(ref this, ref other);
        }

        public override bool Equals(object obj)
        {
            return obj is Line && Equals((Line)obj);
        }
        public static bool Equals(Line line1, Line line2)
        {
            return Equals(ref line1, ref line2);
        }

        public static bool Equals(ref Line line1, ref Line line2)
        {
            return (line1.Normal == line2.Normal) && line1.positoin == line2.positoin;
        }

        public static bool operator ==(Line line1, Line line2)
        {
            return Equals(ref line1, ref line2);
        }
        public static bool operator !=(Line line1, Line line2)
        {
            return !Equals(ref line1, ref line2);
        }
    }

}
