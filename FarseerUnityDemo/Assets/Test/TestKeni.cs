using System.Collections;
using System.Collections.Generic;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
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
    // Use this for initialization
    void Start ()
    {
        fsb = this.GetComponent<FSBodyComponent>();
        fsShapeComponent = this.GetComponent<FSShapeComponent>();
        shape = this.fsShapeComponent.GetShape();
        this.body = fsb.PhysicsBody;
        this.fsb.PhysicsBody.GravityScale = 0;
    }
	
	// Update is called once per frame
	void FixedUpdate ()
	{
	    float h = Input.GetAxis("Horizontal");
	    float v = Input.GetAxis("Vertical");

	    float val = h*h + v*v;
	    if (val > 0)
	    {
            
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

    private List<Fixture> _obstacles = new List<Fixture>();
    private void CollectObstacles(float radius)
    {
        if (this._obstacles.Count > 0)
        {
            this._obstacles.Clear();
        }
        AABB aabb;
        this.shape.ComputeAABB(out aabb, ref body.Xf, 0);

        this.body.World.QueryAABB(a =>
        {
            if (a.Shape != this.shape )
            {
                this._obstacles.Add(a);
            }

            return true;

        },ref aabb);

    }


    private readonly List<Line> _bounds = new List<Line>();
    private readonly List<CCContact> _backupContacts = new List<CCContact>();
    private readonly List<CCContact> _contacts = new List<CCContact>();
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
                targetPositionFound = true;
                int numberOfBounds = _bounds.Count;

                for (int i = 0; i < numberOfBounds; i++)
                {
                    Line line = _bounds[i];

                    float distance = FVector2.Dot(line.Normal, currentMovement);

                    if (distance < 0)
                    {
                        FVector2 correctoin = line.Normal * (-distance);
                        currentMovement += correctoin;
                        targetPositionFound = false;
                    }
                }

            } while (!targetPositionFound && solverIterationCount < 4);

            if (solverIterationCount >= 4 || FVector2.Dot(currentMovement,desiredMovement) < 0)
            {
                break;
            }

            this.body.Position = startPosition + currentMovement;

            UpdateContacts();

            hasUnallowedContacts = HasUnallowedContact(currentMovement);

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



    private void AddBounds(FVector2 position)
    {
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
        //_contacts.Clear();
        //_hasGroundContact = null;

        //int numberOfObstacles = _obstacles.Count;
        //for (int i = 0; i < numberOfObstacles; i++)
        //{
        //    var contactSet = _obstacles[i];

        //    // Let the collision detection compute the contact information (positions, normals, 
        //    // penetration depths, etc.).
        //    CollisionDetection.UpdateContacts(contactSet, 0);

        //    AABB selfAabb;
        //    this.shape.ComputeAABB(out selfAabb, ref body.Xf, 0);
        //    AABB aabb;
        //    contactSet.GetAABB(out aabb,0);
        //    if (HaveContact(selfAabb, aabb))
        //    {
        //    }
        //    else
        //    {
        //        contactSet.Body
        //    }
        //    int numberOfContacts = contactSet.Count;
        //    for (int j = 0; j < numberOfContacts; j++)
        //    {
        //        var contact = contactSet[j];
        //        _contacts.Add(new CCContact
        //        {
        //            Position = contact.PositionALocal,  // Position in local space of character.
        //            Normal = -contact.Normal,           // Normal that points to character.
        //            PenetrationDepth = contact.PenetrationDepth,
        //        });
        //    }
        //}
    }

    public static bool HaveContact(AABB aabbA, AABB aabbB)
    {
        // Note: The following check is safe if one AABB is undefined (NaN).
        // Do not change the comparison operator!
        return AABB.TestOverlap(ref aabbA, ref aabbB);
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