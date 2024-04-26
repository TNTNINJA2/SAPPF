using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]

public class PlayerData : ScriptableObject
{
    [SerializeField] public Attack upLeft;
    [SerializeField] public Attack downLeft;
    [SerializeField] public Attack sideLeft;
    [SerializeField] public Attack upAirLeft;
    [SerializeField] public Attack downAirLeft;
    [SerializeField] public Attack sideAirLeft;


    [SerializeField] public float movementThreshold = 0.1f;
    [SerializeField] public float moveAcceleration = 10;
    [SerializeField] public float moveDeceleration = 10;
    [SerializeField] public float maxSpeed = 1;
    [SerializeField] public float jumpStrength = 10;
    [SerializeField] public float jumpBuffer = 0.1f;
    [SerializeField] public float fastFallAcceleration = -10f;
    [SerializeField] public float maxFastFallSpeed = -10f;
    [SerializeField] public int maxAirJumps = 2;
    [SerializeField] public float joystickBuffer = 0.15f;
    [SerializeField] public float Gravity = 1f;
}