using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : ScriptableObject
{
    public virtual void StartAttack(PlayerController player)
    {
        player.animator.SetTrigger("Attack");
    }

    public virtual void OnHit(PlayerController player)
    {

    }
}
