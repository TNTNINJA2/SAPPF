using com.cyborgAssets.inspectorButtonPro;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObjects/Attacks/Attack", order = 1)]

public class Attack : ScriptableObject
{
    [SerializeField] protected float damage;
    [SerializeField] protected float stunDuration;
    [SerializeField] protected Vector2 launchDirection;
    [SerializeField] protected string ATTACK_ANIMATION_NAME;
    [SerializeField] protected PlayerController playerController;
    [SerializeField] public List<KeyFrame<SpriteKeyFrameData>> spriteKeyFrames = new List<KeyFrame<SpriteKeyFrameData>>();
    [SerializeField] public List<KeyFrame<HitboxKeyFrameData>> hitboxKeyFrames = new List<KeyFrame<HitboxKeyFrameData>>();
    [SerializeField] public List<KeyFrame<PosKeyFrameData>> posKeyFrames = new List<KeyFrame<PosKeyFrameData>>();
    public virtual void StartAttack(PlayerController player)
    {
        player.animator.SetTrigger("Attack");
        player.ChangeNetworkAnimation(ATTACK_ANIMATION_NAME);

    }

    [ProButton]
    public virtual void AddSprite()
    {
        KeyFrame<SpriteKeyFrameData> newSpriteKeyFrame = spriteKeyFrames[spriteKeyFrames.Count - 1];
        newSpriteKeyFrame.frame += 1;
        spriteKeyFrames.Add(newSpriteKeyFrame);
    }

    [ProButton]
    public virtual void AddHitboxKeyFrame(int frame, int length)
    {
        KeyFrame<HitboxKeyFrameData> newHitboxKeyFrame = new KeyFrame<HitboxKeyFrameData>();
        newHitboxKeyFrame.frame = frame;
        newHitboxKeyFrame.data.length = length;
        newHitboxKeyFrame.data.rect = new Rect(Vector2.zero, Vector2.one * 0.2f);
        hitboxKeyFrames.Add(newHitboxKeyFrame);
    }

    [ProButton]
    public virtual void AddPosKeyFrame(int frame, Vector2 pos, Vector2 beforeBezierControlPoint, Vector2 afterBezierControlPoint)
    {
        KeyFrame<PosKeyFrameData> newPosKeyFrame = new KeyFrame<PosKeyFrameData>();
        newPosKeyFrame.frame = frame;
        newPosKeyFrame.data.pos = pos;
        newPosKeyFrame.data.beforeBezierControlPoint = beforeBezierControlPoint;
        newPosKeyFrame.data.afterBezierControlPoint = afterBezierControlPoint;

        int index = 0;
        for (int i = 0; i < posKeyFrames.Count; i++)
        {
            if (posKeyFrames[i].frame < frame) index++;
        }
        posKeyFrames.Insert(index, newPosKeyFrame);
    }


    public int GetTotalDuration()
    {
        int lastSpriteFrameTime = (spriteKeyFrames.Count > 0 )? spriteKeyFrames[spriteKeyFrames.Count-1].frame: 0;
        int lastHitboxFrameTime = 0;
        foreach (KeyFrame<HitboxKeyFrameData> hitboxFrame in hitboxKeyFrames)
        {
            int lastTime = hitboxFrame.frame + hitboxFrame.data.length;
            if (lastTime > lastHitboxFrameTime) lastHitboxFrameTime = lastTime;
        }
        int lastposFrameTime = (posKeyFrames.Count > 0) ? posKeyFrames[posKeyFrames.Count-1].frame : 0;

        return Mathf.Max(lastSpriteFrameTime, lastHitboxFrameTime, lastposFrameTime);
    }

    private float GetHighestKeyFrameTime<T>(List<KeyFrame<T>> keyFrames) where T: KeyFrameData
    {

        return keyFrames[keyFrames.Count - 1].frame;
    }

    public virtual void OnHit(PlayerController player, PlayerController target)
    {
        target.GetComponent<PlayerController>().Hurt(damage, stunDuration, new Vector2(launchDirection.x * player.transform.localScale.x, launchDirection.y));

    }

    public void DisplayAtFrame(PlayerController player, int frame, Vector3 startPosition)
    {
        HandleSprites(player, frame, startPosition);
        HandlePos(player, frame, startPosition);
        HandleHitboxes(player, frame, startPosition);
    }

    private void HandleSprites(PlayerController player, int frame, Vector3 startPosition)
    {
        foreach (KeyFrame<SpriteKeyFrameData> spriteKeyFrame in spriteKeyFrames)
        {
            Sprite sprite;
            if (spriteKeyFrame.frame > frame)
            {
                sprite = spriteKeyFrames[spriteKeyFrames.IndexOf(spriteKeyFrame) - 1].data.sprite;
                player.spriteRenderer.sprite = sprite;
                break;
            }
            sprite = spriteKeyFrames[spriteKeyFrames.Count - 1].data.sprite;
            player.spriteRenderer.sprite = sprite;
        }
    }

    private void HandlePos(PlayerController player, int frame, Vector3 startPosition)
    {
        player.transform.position = GetPosAtFrame(player, frame, startPosition);
        int lastposFrameTime = posKeyFrames.Count;
        if (frame > lastposFrameTime)
        {
            Vector2 newVelocity = GetVelocityAtFrame(player, lastposFrameTime);
            player.rb2D.velocity = newVelocity;
            Debug.Log("new velocity: " + newVelocity);
        }
    }

    public Vector3 GetPosAtFrame(PlayerController player, int frame, Vector3 startPosition)
    {
        foreach (KeyFrame<PosKeyFrameData> posKeyFrame2 in posKeyFrames)
        {
            if (posKeyFrame2.frame > frame)
            {
                KeyFrame<PosKeyFrameData> posKeyFrame1 = posKeyFrames[posKeyFrames.IndexOf(posKeyFrame2) - 1]; // Get the next target keyframe
                float time1 = posKeyFrame1.frame; // Keyframe last passed time
                float time2 = posKeyFrame2.frame; // Next keyframe time

                float t = (frame - time1) / (time2 - time1); // Calculate t (percentage along path between pos1 and 2)
                float tSquared = Mathf.Pow(t, 2);
                float tCubed = Mathf.Pow(t, 3);


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


                // Set Pos
                return startPosition + new Vector3(player.transform.localScale.x * pos.x, pos.y, 0);
            } else if (posKeyFrame2 == posKeyFrames[posKeyFrames.Count - 1])
            {
                return startPosition + new Vector3(player.transform.localScale.x * posKeyFrame2.data.pos.x, posKeyFrame2.data.pos.y, 0);
            }
        }
        return Vector3.zero;
    }


    public Vector3 GetVelocityAtFrame(PlayerController player, float frame)
    {
        foreach (KeyFrame<PosKeyFrameData> posKeyFrame2 in posKeyFrames)
        {
            if (posKeyFrame2.frame > frame)
            {
                KeyFrame<PosKeyFrameData> posKeyFrame1 = posKeyFrames[posKeyFrames.IndexOf(posKeyFrame2) - 1]; // Get the previous target keyframe
                float time1 = posKeyFrame1.frame; // Keyframe last passed time
                float time2 = posKeyFrame2.frame; // Next keyframe time

                float t = (frame - time1) / (time2 - time1); // Calculate t (percentage along path between pos1 and 2)
                float tSquared = Mathf.Pow(t, 2);


                // Get Bezier control points
                Vector2 point1 = posKeyFrame1.data.pos;
                Vector2 point2 = posKeyFrame1.data.afterBezierControlPoint;
                Vector2 point3 = posKeyFrame2.data.beforeBezierControlPoint;
                Vector2 point4 = posKeyFrame2.data.pos;

                // Calculate position on bezier curve
                Vector2 velocity = point1 * (-3 * tSquared + 6 * t - 3) +
                    point2 * (9 * tSquared - 12 * t + 3) +
                    point3 * (-9 * tSquared + 6 * t) +
                    point4 * (3 * tSquared);


                // Set Pos
                return new Vector3(player.transform.localScale.x * velocity.x, velocity.y, 0);
            }

        }
        return Vector3.zero;
    }

    private void HandleHitboxes(PlayerController player, float time, Vector3 startPosition)
    {
        // For each hitbox, if its between its start and end time, boxcast at its pos and size and handle hits
        foreach (KeyFrame<HitboxKeyFrameData> hitboxKeyFrame in hitboxKeyFrames)
        {
            if (hitboxKeyFrame.frame < time && hitboxKeyFrame.frame + hitboxKeyFrame.data.length > time)
            {
                Vector2 hitboxPos = new Vector2(player.transform.position.x, player.transform.position.y) + new Vector2(player.transform.localScale.x * hitboxKeyFrame.data.rect.position.x, hitboxKeyFrame.data.rect.position.y);
                Vector3 hitboxSize = new Vector2(hitboxKeyFrame.data.rect.size.x, hitboxKeyFrame.data.rect.size.y);
                RaycastHit2D[] hits = Physics2D.BoxCastAll(hitboxPos, hitboxSize, 0, Vector2.zero, 0, player.playerLayer);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject.TryGetComponent(out PlayerController playerHit))
                    {
                        if (playerHit != player) OnHit(player, playerHit);
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class KeyFrame<T> where T : KeyFrameData
{
    public int frame;
    public T data;
}

public interface KeyFrameData
{

}

[System.Serializable]
public struct SpriteKeyFrameData : KeyFrameData
{
    public Sprite sprite;
}

[System.Serializable]
public struct HitboxKeyFrameData : KeyFrameData
{

    public Rect rect;
    public int length;

}

[System.Serializable]
public struct PosKeyFrameData : KeyFrameData
{
    public Vector2 pos;
    public Vector2 beforeBezierControlPoint;
    public Vector2 afterBezierControlPoint;
}