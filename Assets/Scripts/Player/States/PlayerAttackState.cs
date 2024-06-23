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
    public int currentFrameIndex;
    public int currentFrameStep = 0; // sub frames throughg a pause frame
    public float frameDuration = 0.02f; // 50 FPS

    private AttackInput currentAttackInput = AttackInput.none;

    private bool hasHit = false;
    public bool HasHit
    {
        get { return hasHit; }
    }

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
            currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, currentAttack.frames.Count - 1);

            AttackFrame frame = currentAttack.frames[currentFrameIndex];

            // Update sprite and position
            player.spriteRenderer.sprite = frame.sprite;

            UpdatePlayerPositionOrVelocity(frame);
            
            HandleHitboxes(frame);
            
            TryTransitionToNewAttackSegment(frame);

            if (frame.pauseDuration > 1 && currentFrameStep <= frame.pauseDuration)
            {
                currentFrameStep++;
            } else if (frame.isHoldFrame && (currentAttackInput == AttackInput.leftClick && player.controls.Player.LeftAttack.ReadValue<float>() == 1 ||
                currentAttackInput == AttackInput.leftClick && player.controls.Player.RightAttack.ReadValue<float>() == 1)) {
                // Do nothing, hold the frame
            } else {
                currentFrameIndex++;
                currentFrameStep = 0;
            }

            // Check for attack end and reset
            if (currentFrameIndex >= currentAttack.frames.Count && currentFrameStep <= frame.pauseDuration)
            {

                EndAttack();
                
            }
        }
    }

    private void UpdatePlayerPositionOrVelocity(AttackFrame frame)
    {
        if (frame.controlsPosition)
        {
            player.rb2D.velocity = Vector2.zero;
            Vector2 move = new Vector2();
            if (currentFrameIndex == 0)
                move = frame.position;
            else
                move = frame.position - currentAttack.frames[currentFrameIndex - 1].position;

            move *= player.transform.localScale;

            player.transform.position += (Vector3)move;

            if (currentFrameIndex == currentAttack.frames.Count - 1)
            {
                // Set velocity based on previousframe
                AttackFrame frame1 = currentAttack.frames[currentFrameIndex - 1];
                AttackFrame frame2 = frame;

                Vector2 difference = frame2.position - frame1.position;
                difference /= Mathf.Max(1, frame2.pauseDuration);
                difference /= Time.fixedDeltaTime;
                player.rb2D.velocity = difference * player.transform.localScale;
            }
        }
        if (!frame.controlsPosition && currentFrameIndex > 1 && currentAttack.frames[currentFrameIndex - 1].controlsPosition) // if previous frame controled position, but current doesn't or if it is the last frame
        {
            // Set velocity based on last two frames
            AttackFrame frame1 = currentAttack.frames[currentFrameIndex - 2];
            AttackFrame frame2 = currentAttack.frames[currentFrameIndex - 1];

            Vector2 difference = frame2.position - frame1.position;
            difference /= Mathf.Max(1, frame2.pauseDuration);
            difference /= Time.fixedDeltaTime;
            player.rb2D.velocity = difference * player.transform.localScale;

        }
    }

    private void HandleHitboxes(AttackFrame frame)
    {
        foreach (Hitbox hitbox in frame.hitboxes)
        {
            Rect rect = hitbox.rect;
            rect.center *= (Vector2)player.transform.localScale;
            rect.center += (Vector2)player.transform.position;
            


            Collider2D[] hitColliders = Physics2D.OverlapAreaAll(rect.min, rect.max, LayerMask.GetMask("Player")); // Filter by layer
            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider.gameObject != player.gameObject) // Exclude self
                {
                    PlayerController otherPlayer = hitCollider.GetComponent<PlayerController>();
                    if (otherPlayer != null)
                    {
                        // Hit!
                        HitPlayer(otherPlayer, hitbox);
                        hasHit = true;
                    }
                }
            }
        }
    }

    private void HitPlayer(PlayerController target, Hitbox hitbox)
    {
        if (hitbox.isGrab)
        {
            target.transform.position = (hitbox.hitVector + hitbox.rect.center) * player.transform.localScale + (Vector2)player.transform.position;
        } else
        {
            target.rb2D.velocity = hitbox.hitVector * player.transform.localScale / Time.fixedDeltaTime;
        }
        target.TakeDamage(hitbox.damage);
        target.Stun(hitbox.stun);
        Debug.Log("Hit " + target);
    }


    public bool TryTransitionToNewAttackSegment(AttackFrame frame)
    {
        bool hasTransitioned = false;
        foreach (AttackTransition transition in frame.transitions)
        {
            if (transition.condition.CheckCondition(this))
            {
                hasTransitioned = true;
                EnterAttackSegment(transition.nextAttackSegment);
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
        currentAttackInput = AttackInput.leftClick;
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
        currentAttackInput = AttackInput.rightClick;

    }

    public override void ExitState()
    {
        base.ExitState();
        currentFrameIndex = 0;
        player.animationPositionController.ResetTargetPos();

    }

    public override void EndAttack()
    {
        base.EndAttack();

        //player.rb2D.velocity = Vector2.Scale(currentAttack.endVelocity / Time.fixedDeltaTime, new Vector2(player.transform.localScale.x, 1));

        currentAttack = null;
        currentFrameIndex = 0;

        if (player.isOnGound)
        {
            player.ChangeState(player.idleState);
        } else
        {
            player.ChangeState(player.aerialState);
        }
        currentAttackInput = AttackInput.none;
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
        currentFrameIndex = 0;
        hasHit = false;
        
    }

    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        DrawHitboxGizmos();
    }

    private void DrawHitboxGizmos()
    {
        foreach (Hitbox hitbox in currentAttack.frames[currentFrameIndex].hitboxes)
        {
            Rect rect = hitbox.rect;
            rect.center *= (Vector2)player.transform.localScale;
            rect.center += (Vector2)player.transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawCube(rect.center, rect.size);
        } 
    }



    public override bool ShouldTryJump()
    {
        return false;
    }

    public override bool ShouldTryAttack()
    {
        return false;
    }

    public enum AttackInput
    {
        none,
        rightClick,
        leftClick
    }
}

