using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public Controls controls;
    [SerializeField] BoxCollider2D groundDetectionHitbox;
    [SerializeField] Rigidbody2D rb2D;

    [SerializeField] float moveAcceleration = 10;
    [SerializeField] float maxSpeed = 1;
    [SerializeField] float jumpStrength = 10;
    [SerializeField] float jumpBuffer = 0.1f;

    public float timeLastJumpPressed;
    


    private void Awake() {
        controls = new Controls();
        controls.Enable();

        controls.Player.Jump.performed += ctx =>
        {
            timeLastJumpPressed = Time.time;
        };

    }

    private void Update()
    {
        if (timeLastJumpPressed + jumpBuffer >= Time.time && IsOnGround())
        {
            timeLastJumpPressed = -1;
            Jump();
        }

        if (controls.Player.Move.IsPressed())
        {
            Move(moveAcceleration * controls.Player.Move.ReadValue<Vector2>().x * Time.deltaTime);
        }
    }

    private void Move(float amount)
    {
        if (!(rb2D.velocity.x * amount > 0 && Mathf.Abs(rb2D.velocity.x + amount) > maxSpeed))
        {
            rb2D.velocity += new Vector2(amount, 0);
            Debug.Log("trymover" + amount);
        }
    }

    private void Jump() {
        rb2D.velocity = Vector2.up * jumpStrength + rb2D.velocity * Vector2.right;
    }

    private bool IsOnGround()
    {
        return groundDetectionHitbox.IsTouchingLayers(LayerMask.GetMask("Ground"));
    }

    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }

    
}
