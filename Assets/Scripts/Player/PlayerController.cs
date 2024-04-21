using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;
using UnityEngine.PlayerLoop;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{

    #region Fields
    [SerializeField] bool isDummy;

    public Controls controls;
    [SerializeField] public BoxCollider2D collistionsHitbox;
    [SerializeField] public BoxCollider2D groundDetectionHitbox;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D attackHitbox;
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] public Animator animator;
    [SerializeField] public PlayerData data;
    [SerializeField] LayerMask playerLayer;

    public bool isOnGound { get; private set; }
    public bool wasOnGound { get; private set; }
    public int airJumps;




    public Vector2 inputDirection;

    // Property which returns input to the nearest whole number vector (up, down, or side)
    public RoundedInputDirection roundedInputDirection = RoundedInputDirection.None;
    [SerializeField] ActiveAttackType activeAttackType = ActiveAttackType.None;


    public float timeLastJumpPressed;

    PlayerState state;

    public PlayerIdleState idleState;
    public PlayerWalkState walkState;
    public PlayerStunState stunState;
    public PlayerAerialState aerialState;
    public PlayerAttackState attackState;

    #endregion



    private void Awake() {
        if (!isDummy)
        {
            controls = new Controls();
            controls.Enable();

            controls.Player.Jump.performed += ctx => TryJump();


            controls.Player.LeftAttack.performed += ctx =>
            {
                if (IsOwner) {
                    if (IsOnGround())
                    {
                        LeftAttack();
                    }
                    else
                    {
                        LeftAirAttack();
                    }
                }
            };
        }
        InitializeStates();
    }

    private void InitializeStates()
    {
        idleState = new PlayerIdleState(this, animator);
        walkState = new PlayerWalkState(this, animator);
        stunState = new PlayerStunState(this, animator);
        aerialState = new PlayerAerialState(this, animator);
        attackState = new PlayerAttackState(this, animator);

        InitializeState(idleState);
    }

    private void InitializeState(PlayerState newState)
    {
        this.state = newState;
        newState.EnterState();
    }

    public void ChangeState(PlayerState newState)
    {
        state.ExitState();
        state = newState;
        state.EnterState();
    }


    private void Update()
    {
        UpdateInputDirection();
        CalculateRoundedInputDirection();

        UpdateIsOnGround();


        if (!wasOnGound && IsOnGround()) OnLand();
        if (wasOnGound && !IsOnGround()) OnLeaveGround();


        state.Update();

        if (!isDummy)
        {
            TryBufferedJump();

            HandleMovementInput();




           
        }
        EndFrame();
    }


    private void FixedUpdate()
    {
        state.FixedUpdate();
    }
    private void UpdateIsOnGround()
    {
        isOnGound = IsOnGround();
    }


    private void UpdateInputDirection()
    {
        inputDirection = controls.Player.Move.ReadValue<Vector2>();
    }

    private void CalculateRoundedInputDirection() {
        Vector2 inputDirection = controls.Player.Move.ReadValue<Vector2>();
        if (inputDirection.y > data.joystickBuffer)
        {
            roundedInputDirection = RoundedInputDirection.Up;
        }
        else if (inputDirection.y < -data.joystickBuffer)
        {
            roundedInputDirection = RoundedInputDirection.Down;
        }
        else if (inputDirection.sqrMagnitude < Mathf.Pow(data.joystickBuffer,2))
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
        if (activeAttackType == ActiveAttackType.UpLeft)
        {
            data.upLeft.OnHit(this, collision.gameObject.GetComponent<PlayerController>());
        }
        else if (activeAttackType == ActiveAttackType.DownLeft)
        {
            data.downLeft.OnHit(this, collision.gameObject.GetComponent<PlayerController>());
        } 
        else if (activeAttackType == ActiveAttackType.SideLeft)
        {
            data.sideLeft.OnHit(this, collision.gameObject.GetComponent<PlayerController>());
        }
        else if (activeAttackType == ActiveAttackType.UpAirLeft)
        {
            data.upAirLeft.OnHit(this, collision.gameObject.GetComponent<PlayerController>());
        }
        else if (activeAttackType == ActiveAttackType.DownAirLeft)
        {
            data.downAirLeft.OnHit(this, collision.gameObject.GetComponent<PlayerController>());
        }
        else if (activeAttackType == ActiveAttackType.SideAirLeft)
        {
            data.sideAirLeft.OnHit(this, collision.gameObject.GetComponent<PlayerController>());
        }

    }

    #region Attacks
    private void LeftAttack()
    {
        if (roundedInputDirection == RoundedInputDirection.Up || roundedInputDirection == RoundedInputDirection.None)
        {
            UpLeft();
            activeAttackType = ActiveAttackType.UpLeft;
        }
        else if (roundedInputDirection == RoundedInputDirection.Down)
        {
            DownLeft();
            activeAttackType = ActiveAttackType.DownLeft;

        }
        else if (roundedInputDirection == RoundedInputDirection.Side)
        {
            SideLeft();
            activeAttackType = ActiveAttackType.SideLeft;

        }
    }
    private void LeftAirAttack()
    {
        if (roundedInputDirection == RoundedInputDirection.Up || roundedInputDirection == RoundedInputDirection.None)
        {
            UpAirLeft();
            activeAttackType = ActiveAttackType.UpAirLeft;
        }
        else if (roundedInputDirection == RoundedInputDirection.Down)
        {
            DownAirLeft();
            activeAttackType = ActiveAttackType.DownAirLeft;

        }
        else if (roundedInputDirection == RoundedInputDirection.Side)
        {
            SideAirLeft();
            activeAttackType = ActiveAttackType.SideAirLeft;

        }
    }



    private void UpLeft()
    {
        data.upLeft.StartAttack(this);
    }
    private void DownLeft()
    {
        data.downLeft.StartAttack(this);
    }
    private void SideLeft()
    {
        data.sideLeft.StartAttack(this);
    }
    private void UpAirLeft()
    {
        data.upAirLeft.StartAttack(this);
    }
    private void DownAirLeft()
    {
        data.downAirLeft.StartAttack(this);
    }
    private void SideAirLeft()
    {
        data.sideAirLeft.StartAttack(this);
    }

    public void OnEnterAttack()
    {
    }
    public void OnExitAttack()
    {
        activeAttackType = ActiveAttackType.None;
    }

    #endregion
    private void OnLand()
    {
        airJumps = data.maxAirJumps;

        rb2D.velocity *= new Vector3(0, 1, 0);
    }


    private void OnLeaveGround()
    {
    }

    #region Movement

    private void HandleMovementInput()
    {
        if (IsOwner)
        {

            if (controls.Player.Move.IsPressed())
            {
                Vector2 input = controls.Player.Move.ReadValue<Vector2>();
                if (Mathf.Abs(input.x) > data.movementThreshold)
                {
                    //Move(data.moveAcceleration * input.x * Time.deltaTime);
                }


               
            }
        }
    }


    public void TryJump()
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
        if (timeLastJumpPressed + data.jumpBuffer >= Time.time && IsOnGround())
        {
            timeLastJumpPressed = -1;
            Jump();
        }
    }

    public void Jump() {
        rb2D.velocity = Vector2.up * data.jumpStrength + rb2D.velocity * Vector2.right;
        if (state != aerialState) ChangeState(aerialState);
    }

    private void AirJump()
    {
        Jump();
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

    public enum RoundedInputDirection
    {
        None,
        Up,
        Down,
        Side
    }

    private enum ActiveAttackType
    {
        None,
        UpLeft,
        DownLeft,
        SideLeft,
        UpAirLeft,
        DownAirLeft,
        SideAirLeft
    }
}
