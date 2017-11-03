using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Xna.Framework;

public struct Line : IEquatable<Line>
{

    public float D;

    public FVector2 Normal;

    public Line(FVector2 normal, float d)
    {
        this.Normal = normal;
        this.D = d;
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
        return (line1.Normal == line2.Normal) && line1.D == line2.D;
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
