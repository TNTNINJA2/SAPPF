using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerIdleState : PlayerState
{
    string IDLE_ANIMATION_NAME = "Idle";
    public PlayerIdleState(PlayerController playerController, Animator animator) : base(playerController, animator)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Enter Idle State");

    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void Update()
    {
        base.Update();



        if (player.controls.Player.Jump.ReadValue<float>() == 1)
        {
            player.Jump();
        }

        animator.Play(IDLE_ANIMATION_NAME);



        if (Mathf.Abs(player.inputDirection.x) > player.data.movementThreshold)
        {
            player.ChangeState(player.walkState);
        }

        if (!player.isOnGound)
        {
            player.ChangeState(player.aerialState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (player.rb2D.velocity.sqrMagnitude < Mathf.Pow(player.data.moveDeceleration * Time.deltaTime, 2))
        {
           player. rb2D.velocity = Vector2.zero;
        }
        else
        {
            player.rb2D.velocity -= player.rb2D.velocity.normalized * player.data.moveDeceleration * Time.deltaTime;
        }
    }
}
