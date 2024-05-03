using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerState
{
    string WALK_ANIMATION_NAME = "Walk";

    public PlayerWalkState(PlayerController playerController, Animator animator) : base(playerController, animator)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Enter Walk State");
    }

    public override void ExitState()
    {
        base.ExitState();

    }

    public override void Update()
    {
        base.Update();

        //animator.Play(WALK_ANIMATION_NAME);
        player.currentAnimation.Value = WALK_ANIMATION_NAME;

        if (player.inputDirection.x > 0)
        {
            player.transform.localScale = new Vector3(1, 1, 1);
        }
        if (player.inputDirection.x < 0)
        {
            player.transform.localScale = new Vector3(-1, 1, 1);
        }
        
        
        if (Mathf.Abs(player.inputDirection.x) < player.data.movementThreshold)
        {
            player.ChangeState(player.idleState);
        }

        if (!player.isOnGound)
        {
            player.ChangeState(player.aerialState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Mathf.Abs(player.inputDirection.x) > player.data.movementThreshold)
        {
            Move();
        }

    }

    private void Move()
    {
        float amount = player.inputDirection.x * player.data.groundedMoveAcceleration;
        float currentSpeed = player.rb2D.velocity.x;

        if (Mathf.Abs(currentSpeed) < player.data.maxSpeed) // if the current speed is less than maxSpeed
        {
            if (Mathf.Abs(currentSpeed + amount) < player.data.maxSpeed) // if target speed is less than max speed set it, otherwise set speed to max speed
            {
                player.rb2D.velocity += Vector2.right * amount;
            }
            else
            {
                player.rb2D.velocity = new Vector2(Mathf.Sign(amount) * player.data.maxSpeed, player.rb2D.velocity.y);
            }
        }
        else
        {
            if (Mathf.Abs(currentSpeed + amount) < Mathf.Abs(currentSpeed)) // if current speed is over maxSpeed, but target speed is lower than current speed,
                                                                            // set speed to target speed
            {
                player.rb2D.velocity += Vector2.right * amount;
            }
        }
    }

}
