using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlayerAttackState : PlayerState
{
    private Attack currentAttack;
    private Vector3 startPosition;

    public PlayerAttackState(PlayerController playerController, Animator animator) : base(playerController, animator)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Enter Attack State");
        startPosition = player.transform.position;

    }

    public override void Update()
    {
        float attackTime = Time.time - startTime;
        Debug.Log("attackTime:" + attackTime);
        foreach (KeyFrame<SpriteKeyFrameData> spriteKeyFrame in currentAttack.spriteKeyFrames)
        {
            if (spriteKeyFrame.time > attackTime)
            {
                player.spriteRenderer.sprite = spriteKeyFrame.data.sprite;
                break;
            }
        }

        foreach (KeyFrame<PosKeyFrameData> posKeyFrame in currentAttack.posKeyFrames)
        {
            if (posKeyFrame.time > attackTime)
            {
                player.transform.position = startPosition +  new Vector3(player.transform.localScale.x * posKeyFrame.data.pos.x, posKeyFrame.data.pos.y, 0);
                break;
            }
        }
    }

    public override void LeftClickPerformed()
    {
        if (player.isOnGound)
        {
            LeftGroundedAttack();
        } else
        {
            LeftAirAttack();
        }
    }

    public override void RightClickPerformed()
    {
        if (player.isOnGound)
        {
            RightGroundedAttack();
        } else
        {
            RightAirAttack();
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        player.animationPositionController.ResetTargetPos();

    }

    public override void EndAttack()
    {
        base.EndAttack();
        if (player.isOnGound)
        {
            player.ChangeState(player.idleState);
        } else
        {
            player.ChangeState(player.aerialState);
        }
    }


    public void LeftGroundedAttack()
    {
        if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Up || player.roundedInputDirection == PlayerController.RoundedInputDirection.None)
        {
            StartAttack(player.data.upLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            StartAttack(player.data.downLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            StartAttack(player.data.sideLeft);
        }
    }

    public void RightGroundedAttack()
    {
        if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Up || player.roundedInputDirection == PlayerController.RoundedInputDirection.None)
        {
            StartAttack(player.data.upRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            StartAttack(player.data.downRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            StartAttack(player.data.sideRight);
        }
    }

    public void LeftAirAttack()
    {
        if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Up || player.roundedInputDirection == PlayerController.RoundedInputDirection.None)
        {
            StartAttack(player.data.upAirLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            StartAttack(player.data.downAirLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            StartAttack(player.data.sideAirLeft);
        }
    }

    public void RightAirAttack()
    {
        if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Up || player.roundedInputDirection == PlayerController.RoundedInputDirection.None)
        {
            StartAttack(player.data.upAirRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            StartAttack(player.data.downAirRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            StartAttack(player.data.sideAirRight);
        }
    }

    private void StartAttack(Attack newAttack)
    {
        currentAttack = newAttack;
        player.activeAttack = newAttack;
        player.activeAttack.StartAttack(player);
    }

    public override void OnHit(PlayerController target)
    {
        base.OnHit(target);
        player.activeAttack.OnHit(player, target);
    }

    public override bool ShouldTryJump()
    {
        return false;
    }

    public override bool ShouldTryAttack()
    {
        return false;
    }
}

