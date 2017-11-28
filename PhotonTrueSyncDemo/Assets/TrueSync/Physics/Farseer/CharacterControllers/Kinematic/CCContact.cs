using System;
using System.Collections.Generic;
namespace TrueSync.Physics2D.Specialized
{
    public struct CCContact
    {
        public TSVector2 Position;       // Local position on the capsule.
        public TSVector2 Normal;         // Normal pointing to capsule.
        public FP PenetrationDepth;
    }
}
