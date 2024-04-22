using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Punch", menuName = "ScriptableObjects/Attacks/Punch", order = 1)]
public class Punch : Attack
{
    private string PUNCH_ANIMATION_NAME = "Punch";
    public override void StartAttack(PlayerController player)
    {
        base.StartAttack(player);
        player.animator.Play(PUNCH_ANIMATION_NAME);
    }

    public override void OnHit(PlayerController player, PlayerController target)
    {
        base.OnHit(player, target);

    }
}
