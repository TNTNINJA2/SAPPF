using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStunState : PlayerState
{
    string STUN_ANIMATION_NAME = "Stun";
    public PlayerStunState(PlayerController playerController, Animator animator) : base(playerController, animator)
    {}

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Enter Stun State");

    }
    public override void Update()
    {
        base.Update();

        player.animator.Play(STUN_ANIMATION_NAME);

        if (player.stunTime > 0)
        {
            player.stunTime -= Time.deltaTime;
        }
        else
        {
            if (player.isOnGound)
            {
                player.ChangeState(player.idleState);
            } else
            {
                player.ChangeState(player.aerialState);
            }
        }
    }

    public override bool ShouldTryAttack()
    {
        return false;
    }
    public override bool ShouldTryJump()
    {
        return false;
    }
}
