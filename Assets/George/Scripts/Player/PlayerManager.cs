using System;
using System.Collections;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    #region Variables

    [Header("Player Data")]
    public PlayerData data;
    public Rigidbody2D rb { get; private set; }
    public Vector2 moveInput;


    [Header("Checks")]
    public Transform GroundCheckPoint;
    public Vector2 GroundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private Transform RightWallCheckPoint;
    [SerializeField] private Transform LeftWallCheckPoint;
    [SerializeField] private Vector2 WallCheckSize = new Vector2(0.5f, 1f);
    public Transform attackPoint;
    [SerializeField] private Vector2 attackSize = new Vector2(1f, 1f);

    [Header("Camera Attributes")]
    [SerializeField] private GameObject cameraFollowObject;
    private CameraFollowObject cameraFollowObjectScript;
    private float fallSpeedYDampingChangeThreshold;

    [Header("Variables calculated at runtime")]
    [HideInInspector] public float gravityStrength;
    [HideInInspector] public float gravityScale;
    [HideInInspector] public float runAccelerationAmount;
    [HideInInspector] public float runDecelerationAmount;
    [HideInInspector] public float jumpForce;

    [Header("Control Booleans")]
    public bool isFacingRight;
    public bool isJumping;
    public bool isWallJumping;
    public bool isSliding;
    public bool isJumpCut;
    private bool isJumpFalling;

    public bool canDash = true;
    public bool canAttack = true;

    [Header("Timers")]
    public float lastPressedJumpTime;
    public float lastOnGroundTime;
    public float lastOnWallTime;
    public float lastOnWallRightTime;
    public float lastOnWallLeftTime;
    public float lastDashTime;
    public float lastAttackTime;
    public float lastWallJumpTime;

    public int lastWallJumpDirection;

    [Header("Layers & Tags")]
    public LayerMask GroundLayer;


    [Header("State Machine Attributes")]
    public PlayerStateMachine stateMachine;
    public PlayerIdleState idleState;
    public PlayerRunState runState;
    public PlayerJumpState jumpState;
    public PlayerFallState fallState;
    public PlayerWallJumpState wallJumpState;
    public PlayerWallSlideState wallSlideState;
    public PlayerDashState dashState;
    public MeleeAttackState meleeAttackState;


    [Header("Coroutines")]
    private Coroutine attackCoroutine;
    #endregion

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        stateMachine = new PlayerStateMachine();

        #region State Instantiation
        // instantiate all states only once
        idleState = new PlayerIdleState(this);
        runState = new PlayerRunState(this);
        jumpState = new PlayerJumpState(this);
        fallState = new PlayerFallState(this);
        wallJumpState = new PlayerWallJumpState(this);
        wallSlideState = new PlayerWallSlideState(this);
        dashState = new PlayerDashState(this);
        meleeAttackState = new MeleeAttackState(this);
        #endregion

        // start in idle
        stateMachine.ChangeState(idleState);

        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        SetGravityScale(gravityScale);
        isFacingRight = true;
        CalculateDerivedValues();

        cameraFollowObjectScript = cameraFollowObject.GetComponent<CameraFollowObject>();
        fallSpeedYDampingChangeThreshold = CameraManager.Instance.fallSpeedYDampingChangeThreshold;
    }

    private void Update()
    {
        #region Timers
        lastOnGroundTime -= Time.deltaTime;
        lastOnWallTime -= Time.deltaTime;
        lastOnWallRightTime -= Time.deltaTime;
        lastOnWallLeftTime -= Time.deltaTime;
        lastWallJumpTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;
        lastDashTime -= Time.deltaTime;
        lastAttackTime -= Time.deltaTime;

        // dash cooldown reset
        if (!canDash && lastDashTime <= -data.dashCooldown)
            canDash = true;
        // attack cooldown reset
        if (!canAttack && lastAttackTime <= -data.attackCooldown)
            canAttack = true;
        #endregion

        #region Input Handler
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (moveInput.x != 0)
            CheckDirectionToFace(moveInput.x > 0);

        if (Input.GetKeyDown(KeyCode.Space))
            HandleJumpInputDown();

        //if (Input.GetKeyUp(KeyCode.Space))
        //    HandleJumpInputUp();

        if (Input.GetKeyDown(KeyCode.Q) && canDash)
            stateMachine.ChangeState(dashState);

        if (Input.GetMouseButtonDown(0) && canAttack)
            stateMachine.ChangeState(meleeAttackState);
        #endregion

        // Collision Checks
        PerformCollisionChecks();

        // Gravity
        ApplyDynamicGravity();

        // Camera Handler
        UpdateCameraDamping();

        // State Machine 
        stateMachine.Update();
    }


    private void FixedUpdate()
    {
        // handle Run
        if (isWallJumping)
            Run(data.wallJumpRunLerp);
        else
            Run(1);

        // handle Slide
        if (isSliding)
            Slide();

        stateMachine.FixedUpdate();
    }


    #region Input Callbacks
    public void HandleJumpInputDown()
    {
        lastPressedJumpTime = data.jumpInputBufferTime;
    }

    public void HandleJumpInputUp()
    {
        if (CanJumpCut() || CanWallJumpCut())
            isJumpCut = true;
    }
    #endregion


    #region General Methods
    public void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
    }

    private void Turn()
    {
        transform.Rotate(0f, 180f, 0f);

        isFacingRight = !isFacingRight;

        cameraFollowObjectScript.CallTurn();
    }
    #endregion


    #region Run Methods
    public void Run(float lerpAmount)
    {
        if (stateMachine.currentState is PlayerDashState)
            return;

        float targetSpeed = moveInput.x * data.runMaxSpeed;

        targetSpeed = Mathf.Lerp(rb.linearVelocityX, targetSpeed, lerpAmount);

        // calculate acceleration rate
        float accelerationRate;

        // gets an acceleration value base on if the player is accelerating (including turning)
        // or decelerating. As well as applying a multiplier if player is air borne.
        if (lastOnGroundTime > 0)
            accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelerationAmount
                                                                : runDecelerationAmount;
        else
            accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelerationAmount * data.accelerationInAir
                                                                : runDecelerationAmount * data.decelerationInAir;

        // add bonus jump apex acceleration
        // increases the accelerating and maxSpeed when at the apex of their jump
        // makes jump feel a bit more bouncy, responsive and natural
        if ((isJumping || isWallJumping || isJumpFalling) && Mathf.Abs(rb.linearVelocityY) < data.jumpHangTimeThreshold)
        {
            accelerationRate *= data.jumpHangAccelerationMult;
            targetSpeed *= data.jumpHangMaxSpeedMult;
        }

        // conserve momentum
        // don't slow the player down if they they are moving in their desired direction
        if (data.doConserveMomentum && Mathf.Abs(rb.linearVelocityX) > Mathf.Abs(targetSpeed)
            && Mathf.Sign(rb.linearVelocityX) == Mathf.Sign(targetSpeed)
            && Mathf.Abs(targetSpeed) > 0.01f && lastOnGroundTime < 0)
        {
            accelerationRate = 0;
        }

        float speedDif = targetSpeed - rb.linearVelocityX;

        float movement = speedDif * accelerationRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }
    #endregion


    #region Slide Methods
    public void Slide()
    {
        // works the same as Run() but on the y axis
        float speedDif = data.slideSpeed - rb.linearVelocityY;
        float movement = speedDif * data.slideAcceleration;

        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

        rb.AddForce(movement * Vector2.up);
    }
    #endregion


    #region Attack Methods
    public void StartMeleeAttack(float duration)
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(HandleMeleeAttack(duration));
    }

    private IEnumerator HandleMeleeAttack(float duration)
    {
        attackPoint.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        attackPoint.gameObject.SetActive(false);
    }
    #endregion


    #region Check Methods
    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight)
            Turn();
    }

    private bool CanJump()
    {
        return lastOnGroundTime > 0 && !isJumping;
    }

    public bool CanWallJump()
    {
        return lastPressedJumpTime > 0 && lastOnWallTime > 0 && lastOnGroundTime <= 0 && (!isWallJumping ||
                (lastOnWallRightTime > 0 && lastWallJumpDirection == 1) || (lastOnWallLeftTime > 0 && lastWallJumpDirection == -1));
    }

    private bool CanJumpCut()
    {
        return isJumping && rb.linearVelocityY > 0;
    }

    private bool CanWallJumpCut()
    {
        return isWallJumping && rb.linearVelocityY > 0;
    }

    public bool CanSlide()
    {
        if (lastOnWallTime > 0 && !isJumping && !isWallJumping && lastOnGroundTime <= 0)
            return true;
        else
            return false;
    }

    private void PerformCollisionChecks()
    {
        // ground check
        if (Physics2D.OverlapBox(GroundCheckPoint.position, GroundCheckSize, 0, GroundLayer))
            lastOnGroundTime = data.coyoteTime;

        bool isTouchingRightWall = Physics2D.OverlapBox(RightWallCheckPoint.position, WallCheckSize, 0, GroundLayer);
        bool isTouchingLeftWall = Physics2D.OverlapBox(LeftWallCheckPoint.position, WallCheckSize, 0, GroundLayer);

        if (isTouchingRightWall && !isWallJumping)
            lastOnWallRightTime = data.coyoteTime;

        if (isTouchingLeftWall && !isWallJumping)
            lastOnWallLeftTime = data.coyoteTime;

        lastOnWallTime = Mathf.Max(lastOnWallLeftTime, lastOnWallRightTime);
    }
    #endregion


    #region Camera
    private void UpdateCameraDamping()
    {
        // if player is falling past a certain speed threshold
        if (rb.linearVelocityY < fallSpeedYDampingChangeThreshold && !CameraManager.Instance.isLerpingYDamping
            && !CameraManager.Instance.lerpedFromPlayerFalling)
        {
            CameraManager.Instance.LerpYDamping(true);
        }

        // if player is standing still or moving up
        if (rb.linearVelocityY >= 0 && !CameraManager.Instance.isLerpingYDamping && CameraManager.Instance.lerpedFromPlayerFalling)
        {
            // reset so it can be called again
            CameraManager.Instance.lerpedFromPlayerFalling = false;

            CameraManager.Instance.LerpYDamping(false);
        }
    }
    #endregion


    #region Gravity
    private void ApplyDynamicGravity()
    {
        float targetGravity = gravityScale;

        // higher gravity if player released the jump input or is falling
        if (isSliding)
            targetGravity = 0;
        else if (rb.linearVelocityY < 0 && moveInput.y < 0)
        {
            // much higher gravity if holding down
            targetGravity *= data.fastFallGravityMult;
            // caps maximum fall speed so when falling over large distances player doesn't accelerate to insanely high speeds
            rb.linearVelocity = new Vector2(rb.linearVelocityX, Mathf.Max(rb.linearVelocityY, -data.maxFastFallSpeed));
        }
        else if (isJumpCut)
        {
            // higher gravity if jump button released
            targetGravity *= data.jumpCutGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocityX, Mathf.Max(rb.linearVelocityY, -data.maxFallSpeed));
        }
        else if ((isJumping || isWallJumping || isJumpFalling) && Mathf.Abs(rb.linearVelocityY) < data.jumpHangTimeThreshold)
        {
            targetGravity *= data.jumpHangGravityMult;
        }
        else if (rb.linearVelocityY < 0)
        {
            // higher gravity if falling
            targetGravity *= data.fastFallGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocityX, Mathf.Max(rb.linearVelocityY, -data.maxFallSpeed));
        }
        SetGravityScale(targetGravity);
    }
    #endregion


    #region Editor Methods
    private void OnValidate()
    {
        //CalculateDerivedValues();
    }

    private void CalculateDerivedValues()
    {
        gravityStrength = -(2 * data.jumpHeight) / (Mathf.Pow(data.jumpTimeToApex, 2));

        gravityScale = gravityStrength / Physics2D.gravity.y;

        // calculate run acceleration & deceleration forces
        if (data.runMaxSpeed > 0)
        {
            runAccelerationAmount = (10 * data.runAcceleration) / data.runMaxSpeed;
            runDecelerationAmount = (10 * data.runDeceleration) / data.runMaxSpeed;
        }
        else
        {
            data.runAcceleration = 0;
            data.runDeceleration = 0;
        }

        jumpForce = Mathf.Abs(gravityStrength) * data.jumpTimeToApex;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(GroundCheckPoint.position, GroundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(RightWallCheckPoint.position, WallCheckSize);
        Gizmos.DrawWireCube(LeftWallCheckPoint.position, WallCheckSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackSize);
    }
    #endregion
}
