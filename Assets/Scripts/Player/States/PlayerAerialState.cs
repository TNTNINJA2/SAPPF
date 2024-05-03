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

        player.currentAnimation.Value = AERIAL_UP_ANIMATION;




        if (player.isOnGound)
        {
            player.ChangeState(player.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (Mathf.Abs(player.inputDirection.x) > 0 && !player.isOnGound) HandleLateralAcceleration();

        if (player.inputDirection.y < 0) FastFall();

    }

    private void HandleLateralAcceleration()
    {

        float amount = player.inputDirection.x * player.data.moveAcceleration;
        float currentSpeed = player.rb2D.velocity.x;

        if (Mathf.Abs(currentSpeed) < player.data.maxSpeed) // if the current speed is less than maxSpeed
        {
            if (Mathf.Abs(currentSpeed + amount) < player.data.maxSpeed) // if target speed is less than max speed set it, otherwise set speed to max speed
            {
                player.rb2D.velocity += Vector2.right * amount;
            } else
            {
                player.rb2D.velocity = new Vector2(Mathf.Sign(amount) * player.data.maxSpeed, player.rb2D.velocity.y);
            }
        } else
        {
            if (Mathf.Abs(currentSpeed + amount) < Mathf.Abs(currentSpeed)) // if current speed is over maxSpeed, but target speed is lower than current speed,
                                                                            // set speed to target speed
            {
                player.rb2D.velocity += Vector2.right * amount;
            }
        }


    }

    private void FastFall()
    {
        float amount = player.data.fastFallAcceleration;
        if (!(player.rb2D.velocity.y * amount > 0 && (player.rb2D.velocity.y + amount) < player.data.maxFastFallSpeed))
        {
            player.rb2D.velocity += new Vector2(0, amount);
        }
    }
}
