using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "ScriptableObjects/Attacks/AttackData", order = 1)]
public class AttackSegmentData : ScriptableObject
{
    public List<AttackFrame> frames;

    public List<AttackTransition> transitions;

    public void DisplayPlayerAtFrame(PlayerController player, int frameIndex, Vector2 initialPosition)
    {
        AttackFrame frame = frames[frameIndex];
        player.transform.position = initialPosition + frame.position;
        player.GetComponent<SpriteRenderer>().sprite = frame.sprite;
    }
}

[System.Serializable] // This allows the struct to show up in the inspector
public struct AttackFrame
{
    public Vector2 position; // Offset from the character's origin
    public Sprite sprite;
    public List<Rect> hitboxes;
    public Rect hurtbox;

    public AttackFrame(Vector2 position, Sprite sprite, List<Rect> hitboxes, Rect hurtbox)
    {
        this.position = position;
        this.sprite = sprite;
        this.hitboxes = hitboxes;
        this.hurtbox = hurtbox;
    }

    public AttackFrame Duplicate()
    {
        AttackFrame newFrame = new AttackFrame(position, sprite, hitboxes, hurtbox);
        return newFrame;
    }
}

public class AttackTransition
{
    public AttackTransitionCondition condition;
    public AttackSegmentData nextAttackSegment;
}

public enum AttackTransitionCondition
{
    hit,
    missed,
    grounded,
    inAir
}
