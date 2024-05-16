using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    protected PlayerController player;
    protected Animator animator;
    protected float startTime;

    public PlayerState(PlayerController playerController, Animator animator)
    {
        this.player = playerController;
        this.animator = animator;

    }

    public virtual void EnterState()
    {
        startTime = Time.time;

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
    public virtual void OnDrawGizmos()
    {

    }

    public virtual bool ShouldTryAttack() { return true; }
    public virtual bool ShouldTryJump() { return true; }
    public virtual bool IsVulnerable() { return true; }

    public virtual void LeftClickPerformed() { }
    public virtual void RightClickPerformed() { }


    public virtual void EndAttack() { }

    public virtual void OnHit(PlayerController target) { }

    public virtual void OnHurt(float amount, float stunDuration, Vector2 launchDirection) { 
        if (IsVulnerable())
        {
            player.rb2D.velocity = launchDirection;
            player.stunTime = stunDuration;
            player.TakeDamage(amount);
            player.ChangeState(player.stunState);

        }
    }


}
