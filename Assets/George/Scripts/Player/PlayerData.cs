using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Player/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("Run")]
    public float runMaxSpeed;
    public float runAcceleration;
    public float runDeceleration;
    [Range(0f, 1f)] public float accelerationInAir;
    [Range(0f, 1f)] public float decelerationInAir;
    public bool doConserveMomentum = true;

    [Header("Jump")]
    public float jumpHeight;
    public float jumpTimeToApex;
    public float jumpCutGravityMult;
    [Range(0f, 1f)] public float jumpHangGravityMult;
    public float jumpHangTimeThreshold;
    public float jumpHangAccelerationMult;
    public float jumpHangMaxSpeedMult;

    [Header("Wall Jump")]
    public Vector2 wallJumpForce;
    [Range(0f, 1f)] public float wallJumpRunLerp;
    [Range(0f, 1f)] public float wallJumpTime;
    public bool doTurnOnWallJump = true;

    [Header("Slide")]
    public float slideSpeed;
    public float slideAcceleration;

    [Header("Dash")]
    public float dashPower;
    public float dashTime;
    public float dashCooldown;

    [Header("Gravity")]
    public float fallGravityMult;
    public float fastFallGravityMult;
    public float maxFallSpeed;
    public float maxFastFallSpeed;

    [Header("Assists")]
    [Range(0.01f, 0.5f)] public float coyoteTime;
    [Range(0.01f, 0.5f)] public float jumpInputBufferTime;

    [Space(20f)]
    [Header("Health")]
    public int maxHealth;

    [Header("Melee Attack")]
    public float attackDamage;
    public float attackDuration;
    public float attackCooldown;

    [Header("Recoil")]
    public float recoilForce;
    public float knockbackForce;
    public float knockbackDuration;
}
