using System;
using System.Collections.Generic;

namespace TrueSync.Physics2D.Specialized
{
    public static class ContactExtend
    {
        internal static void Update1(this Contact contact)
        {
            Body bodyA = contact.FixtureA.Body;
            Body bodyB = contact.FixtureB.Body;

            if (contact.FixtureA == null || contact.FixtureB == null)
                return;

            if (!ContactManager.CheckCollisionConditions(contact.FixtureA, contact.FixtureB))
            {
                contact.Enabled = false;
                return;
            }

            Manifold oldManifold = contact.Manifold;

            // Re-enable this contact.
            contact.Enabled = true;

            bool touching;
            bool wasTouching = contact.IsTouching;

            bool sensor = contact.FixtureA.IsSensor || contact.FixtureB.IsSensor;

            
            // Is this contact a sensor?
            if (sensor)
            {
                Shape shapeA = contact.FixtureA.Shape;
                Shape shapeB = contact.FixtureB.Shape;
                touching = AABB.TestOverlap(shapeA, contact.ChildIndexA, shapeB, contact.ChildIndexB, ref bodyA._xf, ref bodyB._xf);

                // Sensors don't generate manifolds.
                contact.Manifold.PointCount = 0;
            }
            else
            {

                contact.Evaluate(ref contact.Manifold, ref bodyA._xf, ref bodyB._xf);
                touching = contact.Manifold.PointCount > 0;

                // Match old contact ids to new contact ids and copy the
                // stored impulses to warm start the solver.
                for (int i = 0; i < contact.Manifold.PointCount; ++i)
                {
                    ManifoldPoint mp2 = contact.Manifold.Points[i];
                    mp2.NormalImpulse = 0.0f;
                    mp2.TangentImpulse = 0.0f;
                    ContactID id2 = mp2.Id;

                    for (int j = 0; j < oldManifold.PointCount; ++j)
                    {
                        ManifoldPoint mp1 = oldManifold.Points[j];

                        if (mp1.Id.Key == id2.Key)
                        {
                            mp2.NormalImpulse = mp1.NormalImpulse;
                            mp2.TangentImpulse = mp1.TangentImpulse;
                            break;
                        }
                    }
                    contact.Manifold.Points[i] = mp2;
                }

                if (touching != wasTouching)
                {
                    bodyA.Awake = true;
                    bodyB.Awake = true;
                }
            }

            //if (touching)
            //{
            //    contact.Flags |= ContactFlags.Touching;
            //}
            //else
            //{
            //    contact.Flags &= ~ContactFlags.Touching;
            //}

            if (wasTouching == false)
            {
                if (touching)
                {

                    bool enabledA, enabledB;


                    enabledA = true;


                    enabledB = true;

                    contact.Enabled = enabledA && enabledB;

                    //// BeginContact can also return false and disable the contact
                    //if (enabledA && enabledB && contactManager.BeginContact != null)
                    //{
                    //        contact.Enabled = contactManager.BeginContact(this);
                    //}

                    // If the user disabled the contact (needed to exclude it in TOI solver) at any point by
                    // any of the callbacks, we need to mark it as not touching and call any separation
                    // callbacks for fixtures that didn't explicitly disable the collision.
                    //if (!contact.Enabled)
                    //{
                    //    contact.Flags &= ~ContactFlags.Touching;
                    //}


                }
            }
            else
            {
                //if (touching == false)
                //{
                //    //Report the separation to both participants:
                //    if (FixtureA != null && FixtureA.OnSeparation != null)
                //        FixtureA.OnSeparation(FixtureA, FixtureB);

                //    //Reverse the order of the reported fixtures. The first fixture is always the one that the
                //    //user subscribed to.
                //    if (FixtureB != null && FixtureB.OnSeparation != null)
                //        FixtureB.OnSeparation(FixtureB, FixtureA);

                //    if (contactManager.EndContact != null)
                //        contactManager.EndContact(this);
                //}
            }

            if (sensor)
                return;

        }
    }
}
