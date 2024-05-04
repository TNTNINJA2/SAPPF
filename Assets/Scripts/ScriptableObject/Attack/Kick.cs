using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Kick", menuName = "ScriptableObjects/Attacks/Kick", order = 1)]
public class Kick : Attack
{
    private string KICK_ANIMATION_NAME = "Kick";
    public override void StartAttack(PlayerController player)
    {
        base.StartAttack(player);
        player.
        player.animator.Play(KICK_ANIMATION_NAME);

    }

    public override void OnHit(PlayerController player, PlayerController target)
    {
        base.OnHit(player, target);

    }
}
