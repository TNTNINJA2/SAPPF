using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallClingState : PlayerState
{
    string CLING_ANIMATION_NAME = "Idle";
    public PlayerWallClingState(PlayerController playerController, Animator animator) : base(playerController, animator)
    {
    }
    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Enter Wall Cling State");
    }

    public override void ExitState()
    {
     

    }

    public override void Update()
    {
        base.Update();
        player.ChangeNetworkAnimation(CLING_ANIMATION_NAME);


        if (!player.IsOnWall())
        {
            player.ChangeState(player.aerialState);
        }

        if (player.isOnGound)
        {
            player.ChangeState(player.idleState);
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
    public override bool ShouldTryDodge()
    {
        return false;
    }
    public override bool ShouldTryWallJump()
    {
        return true;
    }
    public override bool ShouldTryWallMove()
    {
        return true;
    }
}
