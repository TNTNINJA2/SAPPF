using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AirPunch", menuName = "ScriptableObjects/Attacks/AirPunch", order = 1)]
public class AirPunch : Attack
{
    string AIR_PUNCH_ANIMATION_NAME = "Air Punch";
    public override void StartAttack(PlayerController player)
    {
        base.StartAttack(player);
        player.animator.Play(AIR_PUNCH_ANIMATION_NAME);
    }

    public override void OnHit(PlayerController player, PlayerController target)
    {
        base.OnHit(player, target);

    }
}
