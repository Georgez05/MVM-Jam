using System;
using System.Collections;
using UnityEngine;
using Spine.Unity;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    #region Variables
    [Header("Player Data")]
    public PlayerData data;
    public Rigidbody2D rb { get; private set; }
    public Vector2 moveInput;

    [Header("Skeleton Animation")]
    public GameObject spineGameobject;

    [Header("Checks")]
    public Transform groundCheckPoint;
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private Transform rightWallCheckPoint;
    [SerializeField] private Transform leftWallCheckPoint;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.5f, 1f);
    public GameObject horizontalAttackPoint;
    public GameObject upwardsAttackPoint;
    public GameObject downwardsAttackPoint;
    private GameObject attackPoint;
    [SerializeField] private Vector2 horizontalAttackSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 upwardsAttackSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 downwardAttackSize = new Vector2(1.5f, 2f);

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
    public float lastKnockbackTime;

    public int lastWallJumpDirection;

    [Header("Layers & Tags")]
    public LayerMask groundLayer;
    public LayerMask hazardAndEnemyLayers;


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
        lastKnockbackTime -= Time.deltaTime;

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

        if (Input.GetKeyUp(KeyCode.Space))
            HandleJumpInputUp();

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

        // Check for hazards
        CheckForHazardDamage();
    }


    private void FixedUpdate()
    {
        if (lastKnockbackTime > 0) return;
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

        AttackPointHandler();
        attackCoroutine = StartCoroutine(HandleMeleeAttack(duration));
    }

    private void AttackPointHandler()
    {
        // if in air & moveInput.y < 0 Downwards attack
        if (lastOnGroundTime < 0 && moveInput.y < 0)
            attackPoint = downwardsAttackPoint;
        // moveInput.y > 0 Upwards attack
        else if (moveInput.y > 0)
            attackPoint = upwardsAttackPoint;
        // default to horizontal attack
        else
            attackPoint = horizontalAttackPoint;
    }

    private IEnumerator HandleMeleeAttack(float duration)
    {
        attackPoint.SetActive(true);
        //fade in
        float fadeTime = 0.05f;
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            SetSlashAlpha(Mathf.Lerp(0, 1, t / fadeTime));
            yield return null;
        }

        yield return new WaitForSeconds(duration - 2 * fadeTime);

        // fade out
        t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            SetSlashAlpha(Mathf.Lerp(1f, 0f, t / fadeTime));
            yield return null;
        }

        SetSlashAlpha(0f);
        attackPoint.SetActive(false);
        attackCoroutine = null;
    }

    private void SetSlashAlpha(float alpha)
    {
        if (attackPoint.GetComponent<SpriteRenderer>() != null)
        {
            Color color = attackPoint.GetComponent<SpriteRenderer>().color;
            color.a = alpha;
            attackPoint.GetComponent<SpriteRenderer>().color = color;
        }
    }
    #endregion


    #region Damage Methods
    public void TakeDamage(float damageAmount, Collider2D hit)
    {
        if (lastKnockbackTime > 0)
            return;
        lastKnockbackTime = data.knockbackDuration;

        Debug.Log("Player took " + damageAmount + " damage from " + hit.name);

        // take damage

        // apply damage flash
        spineGameobject.GetComponent<SpineDamageFlash>().CallDamageFlash();

        // apply knockback
        ApplyKnockback(hit);

        // apply damage VFX

        // apply damage SFX
    }
    private void ApplyKnockback(Collider2D hit)
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 playerDirection = isFacingRight ? Vector2.left : Vector2.right;

        if (lastOnGroundTime < 0)
        {
            Vector2 knockbackDirection = (Vector2.up * 0.5f).normalized;

            rb.AddForce(knockbackDirection * data.knockbackForce, ForceMode2D.Impulse);
        }
        else
        {
            Vector2 knockbackDirection = (playerDirection * 1.5f + Vector2.up * 0.5f).normalized;

            rb.AddForce(knockbackDirection * data.knockbackForce, ForceMode2D.Impulse);
        }
    }

    private void CheckForHazardDamage()
    {

        Collider2D hit = Physics2D.OverlapBox(transform.position, new Vector2(3f, 3f), 0, hazardAndEnemyLayers);
        if (hit)
        {
            TakeDamage(1, hit);
        }
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
        if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
            lastOnGroundTime = data.coyoteTime;

        bool isTouchingRightWall = Physics2D.OverlapBox(rightWallCheckPoint.position, wallCheckSize, 0, groundLayer);
        bool isTouchingLeftWall = Physics2D.OverlapBox(leftWallCheckPoint.position, wallCheckSize, 0, groundLayer);

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

        // sliding
        if (isSliding)
            targetGravity = 0;
        // jump cut
        else if (isJumpCut && lastWallJumpTime <= -0.15f)
        {
            // higher gravity if jump button released
            targetGravity *= data.jumpCutGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocityX, Mathf.Max(rb.linearVelocityY, -data.maxFallSpeed));
        }
        // is jumping / wall jumping
        else if ((isJumping || isWallJumping || isJumpFalling) && Mathf.Abs(rb.linearVelocityY) < data.jumpHangTimeThreshold)
        {
            targetGravity *= data.jumpHangGravityMult;
        }
        // fast fall
        else if (rb.linearVelocityY < 0 && moveInput.y < 0)
        {
            targetGravity *= data.fastFallGravityMult;
            // caps maximum fall speed so when falling over large distances player doesn't accelerate to insanely high speeds
            rb.linearVelocity = new Vector2(rb.linearVelocityX, Mathf.Max(rb.linearVelocityY, -data.maxFastFallSpeed));
        }
        // normal fall
        else if (rb.linearVelocityY < 0)
        {
            // higher gravity if falling
            targetGravity *= data.fallGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocityX, Mathf.Max(rb.linearVelocityY, -data.maxFallSpeed));
        }
        SetGravityScale(targetGravity);
    }
    #endregion


    #region Editor Methods
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
        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(rightWallCheckPoint.position, wallCheckSize);
        Gizmos.DrawWireCube(leftWallCheckPoint.position, wallCheckSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(horizontalAttackPoint.transform.position, horizontalAttackSize);
        Gizmos.DrawWireCube(upwardsAttackPoint.transform.position, upwardsAttackSize);
        Gizmos.DrawWireCube(downwardsAttackPoint.transform.position, downwardAttackSize);
    }
    #endregion
}
