using System;
using System.Collections;
using System.Collections.Generic;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerUnity.Base.FarseerPhysics;
using Microsoft.Xna.Framework;
using UnityEditor;
using UnityEngine;

public class TestKeni : MonoBehaviour
{
    public float moveSpeed = 0;
    private FSBodyComponent fsb;
    private Body body;
    private FSShapeComponent fsShapeComponent;
    private Shape shape;
    private Fixture fixture;
    // Use this for initialization
    void Start ()
    {
        fsb = this.GetComponent<FSBodyComponent>();
        fsShapeComponent = this.GetComponent<FSShapeComponent>();
        shape = this.fsShapeComponent.GetShape();
        this.body = fsb.PhysicsBody;
        this.fsb.PhysicsBody.GravityScale = 0;
        this.fixture = this.body.FixtureList[0];
        //Debug.LogError("Self BodyId->"+this.body.BodyId);
    }
	
	// Update is called once per frame
	void FixedUpdate ()
	{
	    float h = Input.GetAxis("Horizontal");
	    float v = Input.GetAxis("Vertical");

	    float val = h*h + v*v;
	    if (val > 0)
	    {
            FVector2 moveVelocity = new FVector2(h,v);
	        moveVelocity.Normalize();
	        moveVelocity = moveVelocity*moveSpeed;
            this.Move(moveVelocity,Time.fixedDeltaTime);
	        //this.fsb.PhysicsBody.LinearVelocity= new FVector2(h, v) * moveSpeed;
	    }

	}


    private FVector2 _oldPosition;
    private FVector2 _desiredPosition;

    void Move(FVector2 moveVelocity, float deltaTime)
    {
        this.body.LinearVelocity = FVector2.Zero;

        this._oldPosition = this.body.Position;

        FVector2 desiredMovement = moveVelocity*deltaTime;

        float desiredMovementLength = desiredMovement.Length();

        CollectObstacles(desiredMovementLength);

        _desiredPosition = _oldPosition + desiredMovement;

        this.Fly();

    }

    private List<Contact> _obstacles = new List<Contact>();
    private void CollectObstacles(float radius)
    {
        if (this._obstacles.Count > 0)
        {
            this._obstacles.Clear();
        }
        AABB aabb;
        this.shape.ComputeAABB(out aabb, ref body.Xf, 0);

        this.body.World.QueryAABB(_fixture =>
        {

            //if (_fixture != this.fixture && _fixture.ShapeType == ShapeType.Circle)
            if (_fixture != this.fixture)
            {
                //UnityEngine.Debug.LogError("Body id->"+_fixture.Body.BodyId);
                Contact contact = Contact.Create(this.fixture, 0, _fixture, 0);
                this._obstacles.Add(contact);
            }

            return true;

        },ref aabb);

    }


    private readonly List<Line> _bounds = new List<Line>();
    private readonly List<CCContact> _backupContacts = new List<CCContact>();
    private readonly List<CCContact> _contacts = new List<CCContact>();

    float AllowedPenetration = 0.01f;
    private void Fly()
    {
        FVector2 desiredMovement = _desiredPosition - _oldPosition;

        if (desiredMovement.LengthSquared() < float.Epsilon)
        {
            return;
        }

        this._bounds.Clear();

        FVector2 startPosition = this.body.Position;

        this.BackupContacts();

        bool hasUnallowedContacts = true;

        int iterationCount = 0;
        
        do
        {
            iterationCount++;
            AddBounds(this.body.Position);
            FVector2 currentMovement = desiredMovement;

            bool targetPositionFound;
            int solverIterationCount = 0;
            do
            {
                solverIterationCount++;
                targetPositionFound = true;
                int numberOfBounds = _bounds.Count;
                
                for (int i = 0; i < numberOfBounds; i++)
                {
                    Line line = _bounds[i];

                    //Vector3 start = new Vector3(line.positoin.X, line.positoin.Y, 0);
                    
                    //Vector3 end = start + new Vector3(line.Normal.Y,-line.Normal.X, 0) * 10;
                    //Debug.DrawLine
                    //    (start
                    //    , end
                    //    , Color.red);
                    

                    FVector2 vec2 = startPosition + currentMovement - line.positoin;
                    //float distance = FVector2.Dot(line.Normal, vec2) + 0.01f;
                    float distance = FVector2.Dot(line.Normal, vec2);
                    //UnityEngine.Debug.LogError("distance : " + distance.ToString("f6")+" normal: "+line.Normal);
                    if (distance < 0)
                    {
                        
                        FVector2 correctoin = line.Normal * (-distance);
                        currentMovement += correctoin;
                        targetPositionFound = false;
                    }
                }

            } while (!targetPositionFound && solverIterationCount < 4);

          
            bool movementDirIsInValid = FVector2.Dot(currentMovement, desiredMovement) < 0;
           
            if (solverIterationCount >= 4 || movementDirIsInValid)
            {
                break;
            }

            this.body.Position = startPosition + currentMovement;

            UpdateContacts();

            hasUnallowedContacts = HasUnallowedContact(currentMovement);
            //Debug.LogError("solverIterationCount : " + solverIterationCount + 
            //    " movementDirIsInValid-> " + movementDirIsInValid + 
            //    " currentMovement->" + currentMovement
            //    + "hasUnallowedContacts->" + hasUnallowedContacts);
        } while (hasUnallowedContacts && iterationCount < 4);


        if (hasUnallowedContacts)
        {
            this.body.Position = startPosition;
            RollbackContacts();
        }
    }

    private bool? _hasGroundContact;
    private bool? _backupHasGroundContact;
    private void BackupContacts()
    {
        this._backupContacts.Clear();

        foreach (var contact in _contacts)
        {
            this._backupContacts.Add(contact);
        }

        _backupHasGroundContact = _hasGroundContact;

    }

    private void RollbackContacts()
    {
        _contacts.Clear();

        // (Note: We could use _contacts.AddRange(_backupContacts) instead of the foreach-loop.
        // But AddRange() creates garbage on the managed heap!)
        foreach (var contact in _backupContacts)
            _contacts.Add(contact);

        _hasGroundContact = _backupHasGroundContact;
    }

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

    public float CollisionDetectionEpsilon = 0.001f;

    private void AddBounds(FVector2 position)
    {
        int oldNumberOfBounds = _bounds.Count;
        int numberOfContacts = _contacts.Count;
        //Debug.LogError("numberOfContacts->" + numberOfContacts);
        for (int i = 0; i < numberOfContacts; i++)
        {
            var contact = _contacts[i];
            FVector2 normal = contact.Normal;
            float penetrationDepth = contact.PenetrationDepth;

            //Debug.LogError("normal->" + normal+ " penetrationDepth->" + penetrationDepth);
            Line line = new Line(normal, position + normal * penetrationDepth);

            bool lineIsNew = true;
            int numberOfBounds = _bounds.Count;
            for (int j = 0; j < numberOfBounds;j++)
            {
                Line existingLine = _bounds[j];
                if (FVector2.AreNumericallyEqual(existingLine.Normal, line.Normal, CollisionDetectionEpsilon)
             && FVector2.AreNumericallyEqual(existingLine.positoin, line.positoin, CollisionDetectionEpsilon))
                {
                    lineIsNew = false;
                }
            }

            if (lineIsNew)
            {
                this._bounds.Add(line);
            }

        }

    }

    /// </remarks>
    private bool HasUnallowedContact(FVector2 currentMovement)
    {
        bool noMovement = (currentMovement == FVector2.Zero);
        float maxPenetrationDepth = 0.01f + 0.001f;

        int numberOfContacts = _contacts.Count;
        for (int i = 0; i < numberOfContacts; i++)
        {
            var contact = _contacts[i];
            float val = FVector2.Dot(contact.Normal, currentMovement);
            if ((noMovement || val < 0)
                && contact.PenetrationDepth > maxPenetrationDepth)
            {
                return true;
            }
        }

        // No forbidden contacts.
        return false;
    }

    private void UpdateContacts()
    {
        this._contacts.Clear();
        _hasGroundContact = null;

        int numberOfObstacles = _obstacles.Count;
        for (int i = 0; i < numberOfObstacles; i++)
        {
            var contact = this._obstacles[i];


            contact.Update1();

            if(contact.Manifold.PointCount == 0)
            {
                continue;
            }

            Manifold manifold = contact.Manifold;
            FVector2 worldNormal = manifold.WorldNormal;
            FVector2 contactPos = body.GetWorldPoint(manifold.contactPoint);

            Vector3 start = new Vector3(contactPos.X, contactPos.Y, 0);
            FVector2 _end = contactPos - worldNormal * 1;
            Vector3 end = new Vector3(_end.X, _end.Y, 0);
            Debug.DrawLine
                (start
                , end
                , Color.red);
            _end = contactPos - new FVector2(worldNormal.Y, -worldNormal.X) * 1;
            end = new Vector3(_end.X, _end.Y, 0);
            Debug.DrawLine
                (start
                , end
                , Color.green);
            //Debug.LogError("contactPos-> " + contactPos + " normal->" + worldNormal + " penetrationDepth->" + manifold.PenetrationDepth);

            this._contacts.Add(new CCContact()
            {

                Position = contactPos,
                Normal = -worldNormal,
                PenetrationDepth = manifold.PenetrationDepth

            });

            //int pointCount = contact.Manifold.PointCount;
            //if (pointCount != 0)
            //{
            //    for (int j = 0; j < pointCount; j++)
            //    {
            //        Manifold manifold = contact.Manifold;
            //        ManifoldPoint point = manifold.Points[j];

                    
            //        FVector2 contactPos = body.GetWorldPoint(point.LocalPoint);
            //        FVector2 worldNormal = manifold.WorldNormal;


            //        Vector3 start = new Vector3(contactPos.X, contactPos.Y, 0);
            //        FVector2 _end = contactPos - worldNormal*manifold.PenetrationDepth;
            //        Vector3 end = new Vector3(_end.X,_end.Y,0);
            //        Debug.DrawLine
            //            (start
            //            , end
            //            , Color.red);
            //        _end = contactPos - new FVector2(worldNormal.Y,-worldNormal.X) * manifold.PenetrationDepth;
            //        end = new Vector3(_end.X, _end.Y, 0);
            //        Debug.DrawLine
            //            (start
            //            , end
            //            , Color.green);
            //        Debug.LogError("contactPos-> " + contactPos+ " normal->" + worldNormal + " penetrationDepth->" + manifold.PenetrationDepth);
            //        this._contacts.Add(new CCContact()
            //        {

            //            Position = contactPos,
            //            Normal = -worldNormal,
            //            PenetrationDepth = manifold.PenetrationDepth

            //        });
            //    }
            //}
        }

        Debug.LogError("this._contacts count : " + this._contacts.Count);


    }

    public static bool HaveContact(AABB aabbA, AABB aabbB)
    {
        // Note: The following check is safe if one AABB is undefined (NaN).
        // Do not change the comparison operator!
        return AABB.TestOverlap(ref aabbA, ref aabbB);
    }


    

}

public static class ContactExtend
{
    internal static void Update1(this Contact contact)
    {
        Manifold oldManifold = contact.Manifold;

            // Re-enable this contact.
        contact.Flags |= ContactFlags.Enabled;

        bool touching;
        bool wasTouching = (contact.Flags & ContactFlags.Touching) == ContactFlags.Touching;

        bool sensor = contact.FixtureA.IsSensor || contact.FixtureB.IsSensor;

        Body bodyA = contact.FixtureA.Body;
        Body bodyB = contact.FixtureB.Body;

        //contact.Evaluate(ref contact.Manifold, ref bodyA.Xf, ref bodyB.Xf);
        //return;
        // Is this contact a sensor?
        if (sensor)
        {
            Shape shapeA = contact.FixtureA.Shape;
            Shape shapeB = contact.FixtureB.Shape;
            touching = AABB.TestOverlap(shapeA, contact.ChildIndexA, shapeB, contact.ChildIndexB, ref bodyA.Xf, ref bodyB.Xf);

                // Sensors don't generate manifolds.
                contact.Manifold.PointCount = 0;
        }
        else
        {
            
            contact.Evaluate(ref contact.Manifold, ref bodyA.Xf, ref bodyB.Xf);
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

        if (touching)
        {
            contact.Flags |= ContactFlags.Touching;
        }
        else
        {
                contact.Flags &= ~ContactFlags.Touching;
        }

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
                if (!contact.Enabled)
                {
                   contact.Flags &= ~ContactFlags.Touching;
                }


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

public class ContactSet
{
    public bool HaveContact { get; set; }

    //public CollisionObject ObjectA { get; private set; }
}




public struct CCContact
{
    public FVector2 Position;       // Local position on the capsule.
    public FVector2 Normal;         // Normal pointing to capsule.
    public float PenetrationDepth;
}