using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LowKick", menuName = "ScriptableObjects/Attacks/LowKick", order = 1)]
public class LowKick : Attack
{
    string LOW_KICK_ANIMATION_NAME = "LowKick";
    public override void StartAttack(PlayerController player)
    {
        base.StartAttack(player);
        player.animator.Play(LOW_KICK_ANIMATION_NAME);

    }
}

