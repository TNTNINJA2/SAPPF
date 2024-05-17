using com.cyborgAssets.inspectorButtonPro;
using Common;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
        newSpriteKeyFrame.time += 0.1f;
        spriteKeyFrames.Add(newSpriteKeyFrame);
    }

    [ProButton]
    public virtual void AddPosKeyFrame()
    {
        KeyFrame<PosKeyFrameData> newPosKeyFrame = posKeyFrames[posKeyFrames.Count - 1];
        newPosKeyFrame.time += 0.1f;
        posKeyFrames.Add(newPosKeyFrame);
    }

    public float GetTotalDuration()
    {
        float lastSpriteFrameTime = (spriteKeyFrames.Count > 0 )? spriteKeyFrames[spriteKeyFrames.Count-1].time: 0;
        float lastHitboxFrameTime = (hitboxKeyFrames.Count > 0) ? hitboxKeyFrames[hitboxKeyFrames.Count-1].time : 0;
        float lastposFrameTime = (posKeyFrames.Count > 0) ? posKeyFrames[posKeyFrames.Count-1].time : 0;

        return Mathf.Max(lastSpriteFrameTime, lastHitboxFrameTime, lastposFrameTime);
    }

    private float GetHighestKeyFrameTime<T>(List<KeyFrame<T>> keyFrames) where T: KeyFrameData
    {

        return keyFrames[keyFrames.Count - 1].time;
    }

    public virtual void OnHit(PlayerController player, PlayerController target)
    {
        target.GetComponent<PlayerController>().Hurt(damage, stunDuration, new Vector2(launchDirection.x * player.transform.localScale.x, launchDirection.y));

    }
}

[System.Serializable]
public struct KeyFrame<T> where T : KeyFrameData
{
    public float time;
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
    public Vector2 pos;
    public Vector2 size;
    public float length;
}

[System.Serializable]
public struct PosKeyFrameData : KeyFrameData
{
    public Vector2 pos;
    public Vector2 beforeBezierControlPoint;
    public Vector2 afterBezierControlPoint;
}