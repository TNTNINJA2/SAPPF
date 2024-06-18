using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlayerAttackState : PlayerState
{
    private Attack currentAttack;
    private Vector3 startPosition;
    private float attackTime;
    private float attackLength;

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
        attackTime = Time.time - startTime;

        HandleSprites();
        HandleHitboxes();



        // End attack if past attack length
        if (attackTime >= attackLength)
        {
            player.EndAttack();
        }


    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        HandlePos();

    }

    private void HandleSprites()
    {
        foreach (KeyFrame<SpriteKeyFrameData> spriteKeyFrame in currentAttack.spriteKeyFrames)
        {
            if (spriteKeyFrame.frame > attackTime)
            {
                Sprite sprite = currentAttack.spriteKeyFrames[currentAttack.spriteKeyFrames.IndexOf(spriteKeyFrame) - 1].data.sprite;
                player.spriteRenderer.sprite = sprite;
                break;
            }
        }
    }

    private void HandlePos()
    {
        player.transform.position = currentAttack.GetPosAtFrame(player, (int)(attackTime / Time.fixedDeltaTime), startPosition);
        float lastposFrameTime = currentAttack.posKeyFrames.Count;
        if (attackTime > lastposFrameTime) {
            Vector2 newVelocity = currentAttack.GetVelocityAtFrame(player, lastposFrameTime);
            player.rb2D.velocity = newVelocity;
            Debug.Log("new velocity: " + newVelocity);
        }
    }

    private void HandleHitboxes()
    {
        // For each hitbox, if its between its start and end time, boxcast at its pos and size and handle hits
        foreach (KeyFrame<HitboxKeyFrameData> hitboxKeyFrame in currentAttack.hitboxKeyFrames)
        {
            Debug.Log(hitboxKeyFrame.data);
            if (hitboxKeyFrame.frame < attackTime && hitboxKeyFrame.frame + hitboxKeyFrame.data.length > attackTime)
            {
                Vector2 hitboxPos = (Vector2)player.transform.position + Vector2.Scale(hitboxKeyFrame.data.rect.position, Vector2.right * player.transform.localScale.x);
                Vector2 hitboxSize = hitboxKeyFrame.data.rect.size;
                Debug.Log("hitboxPos: " + hitboxPos);
                Debug.Log("hitboxSize: " + hitboxSize);

                RaycastHit2D[] hits = Physics2D.BoxCastAll(hitboxPos, hitboxSize, 0, Vector2.zero, 0, player.playerLayer);
                foreach (RaycastHit2D hit in hits)
                {
                    Debug.Log(hit);
                    if (hit.collider != null && hit.collider.gameObject.TryGetComponent(out PlayerController playerHit))
                    {
                        if (playerHit != player) OnHit(playerHit);
                        Debug.Log("Hit a player");
                    }
                }
            }
        }
    }

    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        DrawHitboxGizmos();
    }

    private void DrawHitboxGizmos()
    {
        // For each hitbox, if its between its start and end time, draw the hitbox

        foreach (KeyFrame<HitboxKeyFrameData> hitboxKeyFrame in currentAttack.hitboxKeyFrames)
        {
            if (hitboxKeyFrame.frame < attackTime && hitboxKeyFrame.frame + hitboxKeyFrame.data.length > attackTime)
            {
                Vector3 hitboxPos = player.transform.position + new Vector3(player.transform.localScale.x * hitboxKeyFrame.data.rect.position.x, hitboxKeyFrame.data.rect.position.y, 0);
                Vector3 hitboxSize = new Vector3(hitboxKeyFrame.data.rect.size.x, hitboxKeyFrame.data.rect.size.y, 0);
                Gizmos.color = Color.red;
                Gizmos.DrawCube(hitboxPos, hitboxSize);
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
        attackLength = currentAttack.GetTotalDuration();
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

