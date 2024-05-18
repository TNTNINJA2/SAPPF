using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{

    #region Fields
    [SerializeField] public bool isDummy;

    public Controls controls;
    [SerializeField] public ClientNetworkAnimator networkAnimator;
    [SerializeField] public BoxCollider2D collistionsHitbox;
    [SerializeField] public BoxCollider2D groundDetectionHitbox;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D attackHitbox;
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] public Animator animator;
    [SerializeField] public PlayerData data;
    [SerializeField] public LayerMask playerLayer;

    public AnimationPositionController animationPositionController;


    public bool isOnGound { get; private set; }
    public bool wasOnGound { get; private set; }
    public int airJumps;
    public float stunTime;
    public float dodgeTime;
    public Vector2 dodgeDirection;

    float health;

    private NetworkVariable<NetString> currentAnimation = new NetworkVariable<NetString>(new NetString("Idle"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public struct NetString : INetworkSerializable, System.IEquatable<NetString>
    {
        public string value;

        public NetString(string value)
        {
            this.value = value;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out value);
            } else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(value);
            }
        }
        public bool Equals(NetString other)
        {
            if (String.Equals(other.value, value, StringComparison.CurrentCulture))
            {
                return true;
            }
            return false;
        }

    }


    public Vector2 inputDirection;

    // Property which returns input to the nearest whole number vector (up, down, or side)
    public RoundedInputDirection roundedInputDirection = RoundedInputDirection.None;
    [SerializeField] ActiveAttackType activeAttackType = ActiveAttackType.None;


    public float timeLastJumpPressed;

    public PlayerState state;

    public PlayerIdleState idleState;
    public PlayerWalkState walkState;
    public PlayerStunState stunState;
    public PlayerDodgeState dodgeState;
    public PlayerAerialState aerialState;
    public PlayerAttackState attackState;


    public Attack activeAttack;

    #endregion



    private void Awake() {

        controls = new Controls();
        controls.Enable();

        networkAnimator = GetComponent<ClientNetworkAnimator>();
        animationPositionController = GetComponent <AnimationPositionController>();

 
        health = data.maxHealth;


        InitializeStates();
    }

    private void InitializeStates()
    {
        idleState = new PlayerIdleState(this, animator);
        walkState = new PlayerWalkState(this, animator);
        stunState = new PlayerStunState(this, animator);
        dodgeState = new PlayerDodgeState(this, animator);
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

    private void Start()
    {
        if (IsOwner)
        {
            if (!isDummy)
            {
                controls.Player.LeftAttack.performed += ctx =>
                {
                    if (state.ShouldTryAttack())
                    {
                        ChangeState(attackState);
                        state.LeftClickPerformed();
                    }
                };

                controls.Player.RightAttack.performed += ctx =>
                {
                    if (state.ShouldTryAttack())
                    {
                        ChangeState(attackState);
                        state.RightClickPerformed();
                    }
                };

                controls.Player.Jump.performed += ctx =>
                {
                    if (state.ShouldTryJump()) TryJump();
                };

                controls.Player.Dodge.performed += ctx =>
                {
                    if (state.ShouldTryDodge()) ChangeState(dodgeState);
                };
            }

        }

        currentAnimation.OnValueChanged += (previousValue, newValue) =>
        {
            Debug.Log("Current animation changed to" + newValue.value);
        };

    }

 


    private void Update()
    {
        if (IsOwner || (IsServer && isDummy))
        {
            UpdateIsOnGround();

            if (!wasOnGound && IsOnGround()) OnLand();
            if (wasOnGound && !IsOnGround()) OnLeaveGround();

            state.Update();

            if (!isDummy)
            {
                TryBufferedJump();

                HandleMovementInput();
                UpdateInputDirection();

                CalculateRoundedInputDirection();
            }
            EndFrame();
        }

        animator.Play(currentAnimation.Value.value);
    }

    private void OnDrawGizmos()
    {
        if (state != null) state.OnDrawGizmos();
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            state.FixedUpdate();
        }
    }

    public void ChangeNetworkAnimation(string animationName)
    {
        currentAnimation.Value = new NetString(animationName);

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


    #region Attacks

    public void OnEnterAttack()
    {
    }
    public void EndAttack()
    {
        state.EndAttack();
    }
    public void Hurt(float amount, float stunDuration, Vector2 launchDirection)
    {

        HurtRpc(amount, stunDuration, launchDirection.x, launchDirection.y);


    }



    [Rpc(SendTo.Owner)]
    private void HurtRpc(float amount, float stunDuration, float launchX, float launchY)
    {
        state.OnHurt(amount, stunDuration, new Vector2(launchX, launchY));
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0) Destroy(gameObject);
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
