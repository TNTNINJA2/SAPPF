using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationVelocityControl : MonoBehaviour
{

    public Vector3 velocityInput = Vector3.zero;
    private Vector3 oldVelocityInput = Vector3.zero;

    Animator animator;
    Rigidbody2D rb2D;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    public void Update()
    {
        if (velocityInput != oldVelocityInput)
        {
            rb2D.velocity = new Vector3(velocityInput.x * transform.localScale.x, velocityInput.y, velocityInput.z);
        }

        velocityInput = oldVelocityInput;
    }


}
