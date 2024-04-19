using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 20;
    [SerializeField] float health;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb2D;
    float stunTime;


    private void Awake()
    {
        health = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (stunTime > 0)
        {
            stunTime -= Time.deltaTime;
        } else
        {
            animator.SetBool("IsStunned", false);
        }

    }
    public void TakeDamage(float amount, float stunDuration, Vector2 launchDirection)
    {
        health -= amount;
        if (health <= 0) Destroy(gameObject);
        stunTime = stunDuration;
        animator.SetBool("IsStunned", stunDuration > 0);
        animator.SetTrigger("Stun");

        rb2D.velocity = launchDirection;
    }

}
