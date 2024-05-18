using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDodgeState : PlayerState
{
    public PlayerDodgeState(PlayerController playerController, Animator animator) : base(playerController, animator)
    {
    }

    public override void EnterState()
    {
        base.EnterState();

        player.dodgeDirection = player.inputDirection;
        player.rb2D.velocity = player.dodgeDirection * player.data.dodgeSpeed;

    }

    public override void Update()
    {
        base.Update();

        if (player.dodgeTime > 0)
        {
            player.dodgeTime -= Time.deltaTime;
        }
        else
        {
            if (player.isOnGound)
            {
                player.ChangeState(player.idleState);
            }
            else
            {
                player.ChangeState(player.aerialState);
            }
        }

    }
    public override bool IsVulnerable()
    {
        return false;
    }
}
