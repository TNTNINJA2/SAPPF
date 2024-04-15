using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Punch", menuName = "ScriptableObjects/Attacks/Punch", order = 1)]
public class Punch : Attack
{
    public override void StartAttack(PlayerController player)
    {
        player.animator.SetTrigger("Punch");
    }
}
