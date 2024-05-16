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
        Debug.Log("attackTime:" + attackTime);
        foreach (KeyFrame<SpriteKeyFrameData> spriteKeyFrame in currentAttack.spriteKeyFrames)
        {
            if (spriteKeyFrame.time > attackTime)
            {
                player.spriteRenderer.sprite = spriteKeyFrame.data.sprite;
                break;
            }
        }

        foreach (KeyFrame<PosKeyFrameData> posKeyFrame2 in currentAttack.posKeyFrames)
        {
            if (posKeyFrame2.time > attackTime)
            {
                KeyFrame<PosKeyFrameData> posKeyFrame1 = currentAttack.posKeyFrames[currentAttack.posKeyFrames.IndexOf(posKeyFrame2) - 1]; // Get the next target keyframe
                float time1 = posKeyFrame1.time; // Keyframe last passed time
                float time2 = posKeyFrame2.time; // Next keyframe time

                float t = (attackTime - time1) / (time2 - time1); // Calculate t (percentage along path between pos1 and 2)
                float tSquared = Mathf.Pow(t, 2);
                float tCubed = Mathf.Pow(t, 3);

                Debug.Log("time1: " + time1 + " time2: " + time2);
                Debug.Log("T:" + t);

                // Get Bezier control points
                Vector2 point1 = posKeyFrame1.data.pos;
                Vector2 point2 = posKeyFrame1.data.afterBezierControlPoint;
                Vector2 point3 = posKeyFrame2.data.beforeBezierControlPoint;
                Vector2 point4 = posKeyFrame2.data.pos;

                // Calculate position on bezier curve
                Vector2 pos = point1 * (-tCubed + 3 * tSquared - 3 * t + 1) +
                    point2 * (3 * tCubed - 6 * tSquared + 3 * t) +
                    point3 * (-3 * tCubed + 3 * tSquared) +
                    point4 * (tCubed);

                Debug.Log("Pos: (" + pos.x + ", " + pos.y + ")");

                // Set Pos
                player.transform.position = startPosition + new Vector3(player.transform.localScale.x * pos.x, pos.y, 0);
                break;
            }
        }

        foreach (KeyFrame<HitboxKeyFrameData> hitboxKeyFrame in currentAttack.hitboxKeyFrames)
        {
            if (hitboxKeyFrame.time < attackTime && hitboxKeyFrame.time + hitboxKeyFrame.data.length > attackTime)
            {
                Vector2 hitboxPos = new Vector2(player.transform.position.x, player.transform.position.y) + new Vector2(player.transform.localScale.x * hitboxKeyFrame.data.pos.x, hitboxKeyFrame.data.pos.y);
                Vector3 hitboxSize = new Vector2(hitboxKeyFrame.data.size.x, hitboxKeyFrame.data.size.y);
                ContactFilter2D contactFilter = new ContactFilter2D();
                RaycastHit2D[] hits = Physics2D.BoxCastAll(hitboxPos, hitboxSize, 0, Vector2.zero, 0, player.playerLayer);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject.TryGetComponent(out PlayerController playerHit))
                    {
                        if (playerHit != player) OnHit(playerHit);
                    }
                }

                break;
            }
        }

        if (attackTime >= attackLength)
        {
            player.EndAttack();
        }
    }

    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        foreach (KeyFrame<HitboxKeyFrameData> hitboxKeyFrame in currentAttack.hitboxKeyFrames)
        {
            if (hitboxKeyFrame.time < attackTime && hitboxKeyFrame.time + hitboxKeyFrame.data.length > attackTime)
            {
                Vector3 hitboxPos = player.transform.position + new Vector3(player.transform.localScale.x * hitboxKeyFrame.data.pos.x, hitboxKeyFrame.data.pos.y, 0);
                Vector3 hitboxSize = new Vector3(hitboxKeyFrame.data.size.x, hitboxKeyFrame.data.size.y, 0);
                Gizmos.color = Color.red;
                Gizmos.DrawCube(hitboxPos, hitboxSize);
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
        attackLength = currentAttack.GetAttackLength();
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

