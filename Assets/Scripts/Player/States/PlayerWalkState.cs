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

        animator.Play(WALK_ANIMATION_NAME);


        if (player.inputDirection.x > 0)
        {
            player.transform.localScale = new Vector3(1, 1, 1);
        }
        if (player.inputDirection.x < 0)
        {
            player.transform.localScale = new Vector3(-1, 1, 1);
        }

        if (player.controls.Player.Jump.ReadValue<float>() == 1)
        {
            player.Jump();
        } else if (Mathf.Abs(player.inputDirection.x) > player.data.movementThreshold)
        {
            Move(player.data.moveAcceleration * player.inputDirection.x);
        }
        else
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
            Move(player.data.moveAcceleration * player.inputDirection.x);
        }
        else
        {
            player.ChangeState(player.idleState);
        }
    }

    private void Move(float amount)
    {
        //if (!(player.rb2D.velocity.x * amount > 0 && Mathf.Abs(player.rb2D.velocity.x + amount) > player.data.maxSpeed))
        //{
        //    player.rb2D.velocity = player.rb2D.velocity + new Vector2(amount, 0);
        //} 
        player.rb2D.velocity = Vector2.right * player.data.maxSpeed * player.inputDirection.x;
    }
}
