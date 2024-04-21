using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    protected PlayerController player;
    protected Animator animator;

    public PlayerState(PlayerController playerController, Animator animator)
    {
        this.player = playerController;
        this.animator = animator;

    }

    public virtual void EnterState()
    {

    }
    public virtual void ExitState()
    {

    }
    public virtual void Update()
    {

    }
    public virtual void FixedUpdate()
    {

    }
}
