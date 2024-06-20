using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]

public class PlayerData : ScriptableObject
{
    [SerializeField] public AttackSegmentData upLeft;
    [SerializeField] public AttackSegmentData downLeft;
    [SerializeField] public AttackSegmentData sideLeft;
    [SerializeField] public AttackSegmentData upAirLeft;
    [SerializeField] public AttackSegmentData downAirLeft;
    [SerializeField] public AttackSegmentData sideAirLeft;

    [SerializeField] public AttackSegmentData upRight;
    [SerializeField] public AttackSegmentData downRight;
    [SerializeField] public AttackSegmentData sideRight;
    [SerializeField] public AttackSegmentData upAirRight;
    [SerializeField] public AttackSegmentData downAirRight;
    [SerializeField] public AttackSegmentData sideAirRight;


    [SerializeField] public float movementThreshold = 0.1f;
    [SerializeField] public float moveAcceleration = 10;
    [SerializeField] public float groundedMoveAcceleration = 10;
    [SerializeField] public float groundedMoveDeceleration = 10;
    [SerializeField] public float maxSpeed = 1;
    [SerializeField] public float jumpStrength = 10;
    [SerializeField] public float jumpBuffer = 0.1f;
    [SerializeField] public float wallJumpStrengthX = 8;
    [SerializeField] public float wallJumpStrengthY = 8;
    [SerializeField] public float wallJumpBuffer = 0.1f;
    [SerializeField] public float fastFallAcceleration = -10f;
    [SerializeField] public float maxFastFallSpeed = -10f;
    [SerializeField] public int maxAirJumps = 2;
    [SerializeField] public float joystickBuffer = 0.15f;
    [SerializeField] public float Gravity = 1f;
    [SerializeField] public float maxHealth = 20;
    [SerializeField] public float maxDodgeTime = 0.5f;
    [SerializeField] public float dodgeSpeed = 5f;


}
