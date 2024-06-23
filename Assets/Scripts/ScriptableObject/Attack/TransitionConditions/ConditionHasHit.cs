using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObjects/TransitionConditions/HasHit", order = 1)]
public class ConditionHasHit : AttackTransitionCondition
{
    public override bool CheckCondition(PlayerAttackState attackState)
    {
        return attackState.HasHit;
    }

}
