using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Punch", menuName = "ScriptableObjects/Attacks/Punch", order = 1)]
public class Punch : Attack
{
    public override void StartAttack(PlayerController player)
    {
        base.StartAttack(player);
        player.animator.SetTrigger("Punch");
    }

    public override void OnHit(PlayerController player, PlayerController target)
    {
        base.OnHit(player, target);

    }
}
