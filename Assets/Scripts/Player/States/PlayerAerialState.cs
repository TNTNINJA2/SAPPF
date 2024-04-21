using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerAerialState : PlayerState
{
    string AERIAL_UP_ANIMATION = "Aerial Up";
    string AERIAL_DOWN_ANIMATION = "Aerial Down";
    public PlayerAerialState(PlayerController playerController, Animator animator) : base(playerController, animator)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Enter Aerial State");
    }

    public override void Update()
    {

        animator.Play(AERIAL_UP_ANIMATION);
        if (player.controls.Player.Jump.ReadValue<bool>())
        {
            player.TryJump();
        }

        if (player.inputDirection.y < 0 && !player.isOnGound) FastFall();


        if (player.isOnGound)
        {
            player.ChangeState(player.idleState);
        }
    }

    private void FastFall()
    {
        float amount = player.data.fastFallAcceleration * Time.deltaTime;
        if (!(player.rb2D.velocity.y * amount > 0 && (player.rb2D.velocity.y + amount) < player.data.maxFastFallSpeed))
        {
            player.rb2D.velocity += new Vector2(0, amount);
        }
    }
}
