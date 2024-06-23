using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttackTransitionCondition : ScriptableObject
{
    public abstract bool CheckCondition(PlayerAttackState attackState);
}
