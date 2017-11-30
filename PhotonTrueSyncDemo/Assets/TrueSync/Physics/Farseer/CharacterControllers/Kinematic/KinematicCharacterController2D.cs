using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

            //UnityEngine.Debug.LogError("desiredMovement: " + desiredMovement);

            FP desiredMovementLength = desiredMovement.magnitude;

            CollectObstacles(desiredMovementLength);

            _desiredPosition = _oldPosition + desiredMovement;

            this.Fly(desiredMovementLength);

            //UnityEngine.Debug.LogError("Body LinearVelocity->" + this.body.LinearVelocity);

            this.body.LinearVelocity = TSVector2.zero;
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
            if (Numeric.IsZero(desiredMovementLength, Numeric.EpsilonFSquared))
            {
                return;
            }

            TSVector2 desiredMovement = _desiredPosition - _oldPosition;

            

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
                    solverIterationCount++;
                    targetPositionFound = true;
                    int numberOfBounds = _bounds.Count;

                    for (int i = 0; i < numberOfBounds; i++)
                    {
                        Line line = _bounds[i];

                        TSVector2 vec2 = startPosition + currentMovement - line.positoin;
                        //FP distance = TSVector2.Dot(line.Normal, vec2) + FP.EN2;
                        FP distance = TSVector2.Dot(line.Normal, vec2);


                        if (distance < FP.Zero)
                        {
                            TSVector2 correctoin = line.Normal * (-distance);
                            currentMovement += correctoin;
                            targetPositionFound = false;
                        }
                    }

                } while (!targetPositionFound && solverIterationCount < 4);

                if (solverIterationCount >= 4 || TSVector2.Dot(currentMovement, desiredMovement) < FP.Zero)
                {
                    break;
                }

                //UnityEngine.Debug.LogError("currentMovement->" + currentMovement);
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
                Line line = new Line(normal, position + normal * penetrationDepth);

                bool lineIsNew = true;
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
                if ((noMovement || val < FP.Zero)
                    && contact.PenetrationDepth > Settings.Epsilon)
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
                if (contact.Manifold.PointCount == 0)
                {
                    continue;
                }

                Manifold manifold = contact.Manifold;
                TSVector2 worldNormal = manifold.WorldNormal;
                TSVector2 contactPos = body.GetWorldPoint(manifold.contactPoint);

                UnityEngine.Vector3 start = new UnityEngine.Vector3(contactPos.x.AsFloat(), contactPos.y.AsFloat(), 0);
                TSVector2 _end = contactPos - worldNormal * 1;
                UnityEngine.Vector3 end = new UnityEngine.Vector3(_end.x.AsFloat(), _end.y.AsFloat(), 0);
                UnityEngine.Debug.DrawLine
                    (start
                    , end
                    , UnityEngine.Color.red);
                _end = contactPos - new TSVector2(worldNormal.y.AsFloat(), -worldNormal.x.AsFloat()) * 1;
                end = new Vector3(_end.x.AsFloat(), _end.y.AsFloat(), 0);
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
            }


        }


    }
}

