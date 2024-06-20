using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlayerAttackState : PlayerState
{
    private Vector3 startPosition;
    public AttackSegmentData currentAttack;
    public int currentFrame;
    public float frameDuration = 0.02f; // 50 FPS

    private bool hasHit = false;

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


    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (currentAttack != null)
        {
            // Ensure frame index is within bounds
            currentFrame = Mathf.Clamp(currentFrame, 0, currentAttack.frames.Count - 1);

            AttackFrame frameData = currentAttack.frames[currentFrame];

            // Update sprite and position
            player.spriteRenderer.sprite = frameData.sprite;
            player.transform.position = (Vector2)startPosition + Vector2.Scale(frameData.position, new Vector2(player.transform.localScale.x, 1)); 

            // TODO: Hitboxes
            // ...

            currentFrame++;

            // Check for attack end and reset
            if (currentFrame >= currentAttack.frames.Count)
            {
                if (!TryTransitionToNewAttackSegment())
                {
                    currentAttack = null;
                    currentFrame = 0;

                    player.EndAttack();
                }
            }
        }
    }

    public bool TryTransitionToNewAttackSegment()
    {
        bool hasTransitioned = false;
        foreach (AttackTransition transition in currentAttack.transitions)
        {
            if (transition.condition == AttackTransitionCondition.hit && hasHit)
            {
                EnterAttackSegment(transition.nextAttackSegment);
                hasTransitioned = true;
                break;
            }
            if (transition.condition == AttackTransitionCondition.missed && !hasHit)
            {
                EnterAttackSegment(transition.nextAttackSegment);
                hasTransitioned = true;
                break;
            }
            if (transition.condition == AttackTransitionCondition.grounded && player.isOnGound)
            {
                EnterAttackSegment(transition.nextAttackSegment);
                hasTransitioned = true;
                break;
            }
            if (transition.condition == AttackTransitionCondition.inAir && !player.isOnGound)
            {
                EnterAttackSegment(transition.nextAttackSegment);
                hasTransitioned = true;
                break;
            }
        }
        return hasTransitioned;
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
        currentFrame = 0;
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
            EnterAttackSegment(player.data.upLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            EnterAttackSegment(player.data.downLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            EnterAttackSegment(player.data.sideLeft);
        }
    }

    public void RightGroundedAttack()
    {
        if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Up || player.roundedInputDirection == PlayerController.RoundedInputDirection.None)
        {
            EnterAttackSegment(player.data.upRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            EnterAttackSegment(player.data.downRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            EnterAttackSegment(player.data.sideRight);
        }
    }

    public void LeftAirAttack()
    {
        if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Up || player.roundedInputDirection == PlayerController.RoundedInputDirection.None)
        {
            EnterAttackSegment(player.data.upAirLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            EnterAttackSegment(player.data.downAirLeft);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            EnterAttackSegment(player.data.sideAirLeft);
        }
    }

    public void RightAirAttack()
    {
        if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Up || player.roundedInputDirection == PlayerController.RoundedInputDirection.None)
        {
            EnterAttackSegment(player.data.upAirRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Down)
        {
            EnterAttackSegment(player.data.downAirRight);
        }
        else if (player.roundedInputDirection == PlayerController.RoundedInputDirection.Side)
        {
            EnterAttackSegment(player.data.sideAirRight);
        }
    }

    private void EnterAttackSegment(AttackSegmentData newAttack)
    {
        currentAttack = newAttack;
        player.activeAttack = newAttack;
        currentFrame = 0;
        
    }

    public override void OnHit(PlayerController target)
    {
        base.OnHit(target);
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

