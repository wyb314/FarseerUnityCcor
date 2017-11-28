using System;
using System.Collections;
using System.Collections.Generic;

namespace TrueSync.Physics2D.Specialized
{
    public class KinematicCharacterController2D
    {


        private World world;
        private Body body;
        private Shape shape;
        private Fixture fixture;

        private TSVector2 _oldPosition;
        private TSVector2 _desiredPosition;


        public KinematicCharacterController2D(World world , Body body , Shape shape)
        {
            this.world = world;
            this.body = body;
            this.shape = shape;
            this.fixture = this.body.FixtureList[0];

        }

        public void Move(TSVector2 moveVelocity , FP deltaTime)
        {

            this.body.LinearVelocity = TSVector2.zero;
            this._oldPosition = this.body.Position;

            TSVector2 desiredMovement = moveVelocity * deltaTime;

            FP desiredMovementLength = desiredMovement.magnitude;

            CollectObstacles(desiredMovementLength);

            _desiredPosition = _oldPosition + desiredMovement;

            this.Fly(desiredMovementLength);
        }
        
        private List<Contact> _obstacles = new List<Contact>();
        private void CollectObstacles(FP radius)
        {
            if (this._obstacles.Count > 0)
            {
                this._obstacles.Clear();
            }
            AABB aabb;
            this.shape.ComputeAABB(out aabb, ref body._xf, 0);
            
            this.world.QueryAABB(_fixture =>
            {
                if (_fixture != this.fixture)
                {
                    Contact contact = Contact.Create(this.fixture, 0, _fixture, 0);
                    this._obstacles.Add(contact);
                }

                return true;

            }, ref aabb);
        }



        private readonly List<Line> _bounds = new List<Line>();
        private readonly List<CCContact> _backupContacts = new List<CCContact>();
        private readonly List<CCContact> _contacts = new List<CCContact>();

        FP AllowedPenetration = 0.01f;
        private void Fly(FP desiredMovementLength)
        {
            TSVector2 desiredMovement = _desiredPosition - _oldPosition;

            if ( Numeric.IsZero(desiredMovementLength, Numeric.EpsilonFSquared))
            {
                return;
            }

            this._bounds.Clear();

            TSVector2 startPosition = this.body.Position;

            this.BackupContacts();

            bool hasUnallowedContacts = true;

            int iterationCount = 0;

            do
            {
                iterationCount++;
                AddBounds(this.body.Position);
                TSVector2 currentMovement = desiredMovement;

                bool targetPositionFound;
                int solverIterationCount = 0;
                do
                {
                    targetPositionFound = true;
                    int numberOfBounds = _bounds.Count;

                    for (int i = 0; i < numberOfBounds; i++)
                    {
                        Line line = _bounds[i];

                        FP distance = TSVector2.Dot(line.Normal, currentMovement);

                        if (distance < AllowedPenetration)
                        {
                            TSVector2 correctoin = line.Normal * (-distance);
                            currentMovement += correctoin;
                            targetPositionFound = false;
                        }
                    }

                } while (!targetPositionFound && solverIterationCount < 4);

                if (solverIterationCount >= 4 || TSVector2.Dot(currentMovement, desiredMovement) < 0)
                {
                    break;
                }

                UnityEngine.Debug.LogError("currentMovement->" + currentMovement);
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
        

        public FP CollisionDetectionEpsilon = 0.001f;

        private void AddBounds(TSVector2 position)
        {
            int oldNumberOfBounds = _bounds.Count;
            int numberOfContacts = _contacts.Count;

            for (int i = 0; i < numberOfContacts; i++)
            {
                var contact = _contacts[i];
                TSVector2 normal = contact.Normal;
                FP penetrationDepth = contact.PenetrationDepth;

                //Debug.LogError("normal->" + normal+ " penetrationDepth->" + penetrationDepth);
                Line line = new Line(normal, this.body.Position + normal * penetrationDepth);

                bool lineIsNew = false;
                int numberOfBounds = _bounds.Count;
                for (int j = 0; j < numberOfBounds; j++)
                {
                    Line existingLine = _bounds[j];
                    if (TSVector2.AreNumericallyEqual(existingLine.Normal, line.Normal, CollisionDetectionEpsilon)
                 && TSVector2.AreNumericallyEqual(existingLine.positoin, line.positoin, CollisionDetectionEpsilon))
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
        private bool HasUnallowedContact(TSVector2 currentMovement)
        {
            bool noMovement = (currentMovement == TSVector2.zero);
            FP maxPenetrationDepth = 0.01f + 0.001f;

            int numberOfContacts = _contacts.Count;
            for (int i = 0; i < numberOfContacts; i++)
            {
                var contact = _contacts[i];
                FP val = TSVector2.Dot(contact.Normal, currentMovement);
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

                int pointCount = contact.Manifold.PointCount;
                if (pointCount != 0)
                {
                    for (int j = 0; j < pointCount; j++)
                    {
                        Manifold manifold = contact.Manifold;
                        ManifoldPoint point = manifold.Points[j];


                        TSVector2 contactPos = body.GetWorldPoint(point.LocalPoint);
                        TSVector2 normal = manifold.LocalNormal;
                        //Debug.LogError("contactPos-> " + contactPos + " normal->" + normal + " penetrationDepth->" + manifold.PenetrationDepth);
                        this._contacts.Add(new CCContact()
                        {

                            Position = body.GetWorldPoint(point.LocalPoint),
                            Normal = -manifold.LocalNormal,
                            PenetrationDepth = manifold.PenetrationDepth

                        });
                    }
                }
            }


        }


    }
}

