using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LowKick", menuName = "ScriptableObjects/Attacks/LowKick", order = 1)]
public class LowKick : Attack
{
    public override void StartAttack(PlayerController player)
    {
        base.StartAttack(player);
        player.animator.SetTrigger("LowKick");

    }
}

