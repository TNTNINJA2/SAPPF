using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Kick", menuName = "ScriptableObjects/Attacks/Kick", order = 1)]
public class Kick : Attack
{
    public override void StartAttack(PlayerController player)
    {
        player.animator.SetTrigger("Kick");

    }
}
