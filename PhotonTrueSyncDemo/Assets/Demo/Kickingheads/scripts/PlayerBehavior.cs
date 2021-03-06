﻿using UnityEngine;
using TrueSync;
using TrueSync.Physics2D;
using TrueSync.Physics2D.Specialized;

/**
* @brief Manages player's behavior.
**/
public class PlayerBehavior : TrueSyncBehaviour {

    /**
    * @brief Key to set/get player's movement from {@link TrueSyncInput}.
    **/
    private const byte INPUT_KEY_MOVE_X = 0;

    private const byte INPUT_KEY_MOVE_Y = 2;

    /**
    * @brief Key to set/get player's jump from {@link TrueSyncInput}.
    **/
    private const byte INPUT_KEY_JUMP = 1;

    /**
    * @brief Player's movement speed.
    **/
    public int speed;

    /**
    * @brief Array of animator controllers (one for each player).
    **/
    public RuntimeAnimatorController[] animatorControllers;

    /**
    * @brief Array of player sprites (one for each player).
    **/
    public Sprite[] sprites;

    /**
    * @brief Instance of an Animator for fast access.
    **/
    Animator animator;

    /**
    * @brief Instance of an SpriteRenderer for fast access.
    **/
    SpriteRenderer spriteRenderer;

    KinematicCharacterController2D characterController;
    void Start()
    {
        TSCircleCollider2D circleCollider2D = this.GetComponent<TSCircleCollider2D>();
        this.characterController = new KinematicCharacterController2D(
            (World)PhysicsManager.instance.GetWorld(),
            circleCollider2D._body,circleCollider2D.Shape);
    }



    /**
    * @brief Initial setup when game is started.
    **/
    public override void OnSyncedStart () {
		animator = GetComponent<Animator> ();
		spriteRenderer = GetComponent<SpriteRenderer> ();

        // Sets sprite and animator controller based on player's id
		if (owner.Id == 1) {
			animator.runtimeAnimatorController = animatorControllers [0];
			spriteRenderer.sprite = sprites [0];

			tsRigidBody2D.position = new TSVector2(1, 0);
		} else {
			animator.runtimeAnimatorController = animatorControllers [1];
			spriteRenderer.sprite = sprites [1];
			spriteRenderer.flipX = true;

            TSVector2 offset = new TSVector2(-0.63f, -0.87f);
            tsCollider2D.Center = offset;
			tsRigidBody2D.position = new TSVector2(-1 - offset.x, 0);
		}

        // take control of the animation
        animator.StopPlayback();
    }

    /**
    * @brief Updates player's animation.
    **/
    void UpdateAnimations() {
        animator.Update(Time.deltaTime);

        if (tsRigidBody2D.velocity.LengthSquared() > FP.Half) {
            animator.SetBool("running", true);
        } else {
            animator.SetBool("running", false);
        }
    }

    /**
    * @brief Sets player inputs.
    **/
    public override void OnSyncedInput () {
		int movementX = (int)(Input.GetAxis("Horizontal") * 100);
        byte jump = Input.GetButton("Jump") ? (byte)1 : (byte)0;

        int movementY = (int)(Input.GetAxis("Vertical") * 100);

        TrueSyncInput.SetInt(INPUT_KEY_MOVE_X, movementX);
        TrueSyncInput.SetInt(INPUT_KEY_MOVE_Y, movementY);

        //TrueSyncInput.SetByte(INPUT_KEY_JUMP, jump);
	}

    /**
    * @brief Updates player animations and movements.
    **/
 //   public override void OnSyncedUpdate () {
	//	UpdateAnimations();




 //       // Set a velocity based on player's speed and inputs
	//	TSVector2 velocity = tsRigidBody2D.velocity;
	//	velocity.x = TrueSyncInput.GetInt(INPUT_KEY_MOVE) * speed / (FP) 100;

	//	if (TrueSyncInput.GetByte(INPUT_KEY_JUMP) > 0) {
 //           velocity.y = 10;
 //       }

 //       // Assigns this velocity as new player's linear velocity
	//	tsRigidBody2D.velocity = velocity;
	//}

    public override void OnSyncedUpdate()
    {
        UpdateAnimations();

        int x_key = TrueSyncInput.GetInt(INPUT_KEY_MOVE_X);
        int y_Key = TrueSyncInput.GetInt(INPUT_KEY_MOVE_Y);
        if (x_key == 0 && y_Key == 0)
        {
            return;
        }
        TSVector2 moveVelocity = new TSVector2(TrueSyncInput.GetInt(INPUT_KEY_MOVE_X) * speed / (FP)100, TrueSyncInput.GetInt(INPUT_KEY_MOVE_Y) * speed / (FP)100);
        
        //UnityEngine.Debug.LogError("In fixed time step : "+Time.inFixedTimeStep);
        this.characterController.Move(moveVelocity,Time.fixedDeltaTime);

    }


}