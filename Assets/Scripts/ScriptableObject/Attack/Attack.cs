using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObjects/Attacks/Attack", order = 1)]

public class Attack : ScriptableObject
{
    [SerializeField] protected float damage;
    [SerializeField] protected float stunDuration;
    [SerializeField] protected Vector2 launchDirection;
    [SerializeField] protected string ANIMATION_NAME;
    public virtual void StartAttack(PlayerController player)
    {
        player.animator.SetTrigger("Attack");
        player.currentAnimation.Value = ANIMATION_NAME;
    }

    public virtual void OnHit(PlayerController player, PlayerController target)
    {
        target.GetComponent<PlayerController>().Hurt(damage, stunDuration, new Vector2(launchDirection.x * player.transform.localScale.x, launchDirection.y));

    }
}
