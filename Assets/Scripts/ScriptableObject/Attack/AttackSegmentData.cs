using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "ScriptableObjects/Attacks/AttackData", order = 1)]
public class AttackSegmentData : ScriptableObject
{
    public List<Sprite> spritePalette;

    public List<AttackFrame> frames;


    public Vector2 endVelocity;

    public void DisplayPlayerAtFrame(PlayerController player, int frameIndex, Vector2 initialPosition)
    {
        AttackFrame frame = frames[frameIndex];
        player.transform.position = initialPosition + frame.position;
        player.GetComponent<SpriteRenderer>().sprite = frame.sprite;
    }
}

[System.Serializable] // This allows the struct to show up in the inspector
public class AttackFrame
{
    public Vector2 position; // Offset from the character's origin
    public Sprite sprite;
    public List<Hitbox> hitboxes = new List<Hitbox>();
    public Rect hurtbox;
    public int pauseDuration;
    public bool controlsPosition = true;
    public bool isHoldFrame = false; // If attack should hold on frame while player is holding attack button
    public bool controlsHurtbox = false; 
    public List<AttackTransition> transitions = new List<AttackTransition>();


    public AttackFrame(Vector2 position, Sprite sprite, List<Hitbox> hitboxes, Rect hurtbox, int pauseDuration, bool controlsPosition, bool isHoldFrame, bool controlsHurtbox, List<AttackTransition> transitions)
    {
        this.position = position;
        this.sprite = sprite;
        this.hitboxes = hitboxes.ToArray().ToList(); // Make sure the list of hitboxes is separate (create a new instance with same data)
        this.hurtbox = hurtbox;
        this.pauseDuration = pauseDuration;
        this.controlsPosition = controlsPosition;
        this.isHoldFrame = isHoldFrame;
        this.controlsHurtbox = controlsHurtbox;
        this.transitions = transitions.ToArray().ToList(); // Make sure the list of hitboxes is separate (create a new instance with same data)
    }
     
    public AttackFrame Duplicate()
    {
        AttackFrame newFrame = new AttackFrame(position, sprite, hitboxes, hurtbox, pauseDuration, controlsPosition, isHoldFrame, controlsHurtbox, transitions);
        return newFrame;
    }
}

[System.Serializable]
public struct Hitbox
{
    public Rect rect;
    public Vector2 hitVector;
    public bool isGrab;
    public float damage;
    public float stun;
}

[System.Serializable]
public struct AttackTransition
{
    public AttackTransitionCondition condition;
    public AttackSegmentData nextAttackSegment;
}


