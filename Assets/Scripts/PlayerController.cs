using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;
using UnityEngine.PlayerLoop;

public class PlayerController : MonoBehaviour
{

# region Fields
    public Controls controls;
    [SerializeField] BoxCollider2D collistionsHitbox;
    [SerializeField] BoxCollider2D groundDetectionHitbox;
    [SerializeField] Rigidbody2D rb2D;
    [SerializeField] Collider2D attackHitbox;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] public Animator animator;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] Attack uplight;
    [SerializeField] Attack downlight;
    [SerializeField] Attack sidelight;


    [SerializeField] float movementThreshold = 0.1f;
    [SerializeField] float moveAcceleration = 10;
    [SerializeField] float maxSpeed = 1;
    [SerializeField] float jumpStrength = 10;
    [SerializeField] float jumpBuffer = 0.1f;
    [SerializeField] float fastFallAcceleration = -10f;
    [SerializeField] float maxFastFallSpeed = -10f;
    [SerializeField] int maxAirJumps = 2;
    [SerializeField] int airJumps;
    [SerializeField] bool wasOnGound;
    [SerializeField] float joystickBuffer = 0.15f;
    [SerializeField] float Gravity = 1f;

    // Property which returns input to the nearest whole number vector (up, down, or side)
    [SerializeField] RoundedInputDirection roundedInputDirection = RoundedInputDirection.None;


    public float timeLastJumpPressed;

#endregion



    private void Awake() {
        controls = new Controls();
        controls.Enable();

        controls.Player.Jump.performed += ctx => TryJump();

        controls.Player.LightAttack.performed += ctx => LightAttack();

    }


    private void Update()
    {
        CalculateRoundedInputDirection();
        TryBufferedJump();

        HandleMovementInput();

        if (!wasOnGound && IsOnGround()) OnLand();

        if (wasOnGound && !IsOnGround()) OnLeaveGround();
        
        animator.SetBool("IsFalling", (rb2D.velocity.y < -0.1));

        EndFrame();
    }

    private void CalculateRoundedInputDirection() {
        Vector2 inputDirection = controls.Player.Move.ReadValue<Vector2>();
        if (inputDirection.y > joystickBuffer)
        {
            roundedInputDirection = RoundedInputDirection.Up;
        }
        else if (inputDirection.y < -joystickBuffer)
        {
            roundedInputDirection = RoundedInputDirection.Down;
        }
        else if (inputDirection.sqrMagnitude < Mathf.Pow(joystickBuffer,2))
        {
            roundedInputDirection = RoundedInputDirection.None;
        }
        else
        {
            roundedInputDirection = RoundedInputDirection.Side;
        }
    }

    private void EndFrame()
    {
        wasOnGound = IsOnGround();
    }



    public void HitEnemy(Collider2D collision)
    {
        collision.gameObject.transform.position += new Vector3(0, 10, 0);
    }

    #region Attacks
    private void LightAttack()
    {
        if (roundedInputDirection == RoundedInputDirection.Up)
        {
            UpLight();
        }
        else if (roundedInputDirection == RoundedInputDirection.Down)
        {
            DownLight();
        }
        else if (roundedInputDirection == RoundedInputDirection.Side)
        {
            SideLight();
        }
    }


   private void UpLight()
    {
        uplight.StartAttack(this);
    }
    private void DownLight()
    {
        downlight.StartAttack(this);
    }
    private void SideLight()
    {
        sidelight.StartAttack(this);
    }

    public void OnEnterAttack()
    {
    }
    public void OnExitAttack()
    {
    }

    #endregion
    private void OnLand()
    {
        airJumps = maxAirJumps;
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsInAir", false);
        rb2D.velocity *= new Vector3(0, 1, 0);
    }


    private void OnLeaveGround()
    {
        animator.SetBool("IsInAir", true);
    }

    #region Movement

    private void HandleMovementInput()
    {

        if (controls.Player.Move.IsPressed())
        {
            Vector2 input = controls.Player.Move.ReadValue<Vector2>();
            if (Mathf.Abs(input.x) > movementThreshold)
            {
                Move(moveAcceleration * input.x * Time.deltaTime);
            }

            if (input.y < 0 && !IsOnGround()) FastFall();

            if (input.x > 0)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            if (input.x < 0)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

        animator.SetBool("InputUp", roundedInputDirection == RoundedInputDirection.Up);
        animator.SetBool("InputDown", roundedInputDirection == RoundedInputDirection.Down);
        animator.SetBool("InputSide", roundedInputDirection == RoundedInputDirection.Side);

    }
    private void Move(float amount)
    {
        if (!(rb2D.velocity.x * amount > 0 && Mathf.Abs(rb2D.velocity.x + amount) > maxSpeed))
        {
            rb2D.velocity = rb2D.velocity + new Vector2(amount, 0);

            if (IsOnGround())
            {
                animator.SetBool("IsMoving", true);
            }
        }
    }

    private void TryJump()
    {
        if (IsOnGround())
        {
            Jump();
        }
        else if (airJumps > 0)
        {
            AirJump();
            airJumps -= 1;
        }
        else
        {
            timeLastJumpPressed = Time.time;
        }
    }

    private void TryBufferedJump()
    {
        if (timeLastJumpPressed + jumpBuffer >= Time.time && IsOnGround())
        {
            timeLastJumpPressed = -1;
            Jump();
        }
    }

    private void Jump() {
        rb2D.velocity = Vector2.up * jumpStrength + rb2D.velocity * Vector2.right;
        animator.SetBool("IsJumping", true);
        animator.SetTrigger("IsJumping");
    }

    private void AirJump()
    {
        Jump();
    }

    private void FastFall()
    {
        float amount = fastFallAcceleration * Time.deltaTime;
        if (!(rb2D.velocity.y * amount > 0 && (rb2D.velocity.y + amount) < maxFastFallSpeed))
        {
            rb2D.velocity += new Vector2(0, amount);
        }
    }

    #endregion

    private bool IsOnGround()
    {
        return groundDetectionHitbox.IsTouchingLayers(LayerMask.GetMask("Ground"));
    }

    #region Enabling
    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }

    #endregion

    private enum RoundedInputDirection
    {
        None,
        Up,
        Down,
        Side
    }
}
