using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : ScriptableObject
{
    [SerializeField] protected float damage;
    [SerializeField] protected float stunDuration;
    [SerializeField] protected Vector2 launchDirection;
    public virtual void StartAttack(PlayerController player)
    {
        player.animator.SetTrigger("Attack");
    }

    public virtual void OnHit(PlayerController player, PlayerController target)
    {
        target.GetComponent<PlayerHealth>().TakeDamage(damage, stunDuration, new Vector2(launchDirection.x * player.transform.localScale.x, launchDirection.y));

    }
}
