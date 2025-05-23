using Unity.VisualScripting;
using UnityEngine;

#region Base State
public class PlayerState
{
    protected PlayerManager player;

    public PlayerState(PlayerManager player)
    {
        this.player = player;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
}
#endregion

#region Idle State
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerManager player) : base(player) { }

    public override void Update()
    {
        if (Mathf.Abs(player.moveInput.x) > 0.1f)
        {
            player.stateMachine.ChangeState(player.runState);
        }
        else if (player.lastPressedJumpTime > 0 && player.lastOnGroundTime > 0)
        {
            player.stateMachine.ChangeState(player.jumpState);
        }
        else if (player.lastOnGroundTime <= 0)
        {
            player.stateMachine.ChangeState(player.fallState);
        }
        else if (player.CanSlide())
        {
            player.stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
#endregion

#region Run State
public class PlayerRunState : PlayerState
{
    public PlayerRunState(PlayerManager player) : base(player) { }

    public override void Update()
    {
        if (Mathf.Abs(player.moveInput.x) < 0.1f)
        {
            player.stateMachine.ChangeState(player.idleState);
        }
        else if (player.lastPressedJumpTime > 0 && player.lastOnGroundTime > 0)
        {
            player.stateMachine.ChangeState(player.jumpState);
        }
        else if (player.lastOnGroundTime <= 0)
        {
            player.stateMachine.ChangeState(player.fallState);
        }
        else if (player.CanSlide())
        {
            player.stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
#endregion

#region Jump State
public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(PlayerManager player) : base(player) { }

    public override void Enter()
    {
        player.isJumping = true;
        player.isJumpCut = false; // preventsstale jump cut from wall jump

        // ensures Jump can't be called multiple times from one press
        player.lastPressedJumpTime = 0;
        player.lastOnGroundTime = 0;

        float force = player.jumpForce;
        if (player.rb.linearVelocityY < 0)
            force -= player.rb.linearVelocityY;

        player.rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    public override void Update()
    {
        if (player.lastWallJumpTime < 0.1f && player.rb.linearVelocityY < 0)
        {
            player.stateMachine.ChangeState(player.fallState);
        }
        else if (player.CanWallJump())
        {
            player.stateMachine.ChangeState(player.wallJumpState);
        }
    }

    public override void Exit()
    {
        player.isJumping = false;
        player.isJumpCut = false;
    }
}
#endregion

#region Fall State
public class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerManager player) : base(player) { }

    public override void Update()
    {
        if (player.lastOnGroundTime > 0)
        {
            player.stateMachine.ChangeState(player.idleState);
        }
        else if (player.CanWallJump())
        {
            player.stateMachine.ChangeState(player.wallJumpState);
        }
        else if (player.CanSlide())
        {
            player.stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
#endregion

#region WallJump State
public class PlayerWallJumpState : PlayerState
{
    public PlayerWallJumpState(PlayerManager player) : base(player) { }

    public override void Enter()
    {
        // Make Run() less effective if in the wall jump state.
        player.isWallJumping = true;
        player.lastWallJumpTime = 0;
        int dir = (player.lastOnWallRightTime > 0) ? -1 : 1;
        player.lastWallJumpDirection = dir;

        // ensures Wall Jump can't be called multiple times from one press
        player.lastPressedJumpTime = 0;
        player.lastOnGroundTime = 0;
        player.lastOnWallRightTime = 0;
        player.lastOnWallLeftTime = 0;

        Vector2 force = new Vector2(player.data.wallJumpForce.x, player.data.wallJumpForce.y);
        force.x *= dir; // apply force in opposite direction of wall

        if (Mathf.Sign(player.rb.linearVelocityX) != Mathf.Sign(force.x))
            force.x -= player.rb.linearVelocityX;

        if (player.rb.linearVelocityY < 0)
            force.y -= player.rb.linearVelocityY;

        player.rb.AddForce(force, ForceMode2D.Impulse);
    }

    public override void Update()
    {
        if (player.lastWallJumpTime < 0.1f && player.rb.linearVelocityY < 0)
        {
            player.isWallJumping = false;
            player.stateMachine.ChangeState(player.fallState);
        }
    }

    public override void Exit()
    {
        player.isWallJumping = false;
    }
}
#endregion

#region WallSlide State
public class PlayerWallSlideState : PlayerState
{
    public PlayerWallSlideState(PlayerManager player) : base(player) { }

    public override void Enter()
    {
        player.isSliding = true;
    }
    public override void Update()
    {
        if (!player.CanSlide())
        {
            player.isSliding = false;
            player.stateMachine.ChangeState(player.fallState);
        }
        else if (player.CanWallJump())
        {
            player.stateMachine.ChangeState(player.wallJumpState);
        }
    }

    public override void Exit()
    {
        player.isSliding = false;
    }
}
#endregion

#region Dash State
public class PlayerDashState : PlayerState
{
    private float dashStartTime;
    private Vector2 dashDirection;
    public PlayerDashState(PlayerManager player) : base(player) { }

    public override void Enter()
    {
        dashStartTime = Time.time;
        player.lastDashTime = 0;
        player.canDash = false;

        // reset all velocity and lock into a horizontal dash
        player.rb.linearVelocity = Vector2.zero;
        player.SetGravityScale(0);

        dashDirection = player.isFacingRight ? Vector2.right : Vector2.left;
        player.rb.linearVelocity = dashDirection * player.data.dashPower;
    }

    public override void Update()
    {
        if (Time.time - dashStartTime >= player.data.dashTime)
        {
            player.SetGravityScale(player.gravityScale); // restore gravity

            // Transition logic
            if (player.lastOnGroundTime > 0)
            {
                player.stateMachine.ChangeState(Mathf.Abs(player.moveInput.x) > 0.01f
                    ? player.runState
                    : player.idleState);
            }
            else if (player.CanSlide())
            {
                player.stateMachine.ChangeState(player.wallSlideState);
            }
            else
            {
                player.stateMachine.ChangeState(player.fallState);
            }
        }
    }

    public override void FixedUpdate()
    {
        player.rb.linearVelocity = dashDirection * player.data.dashPower;
    }

    public override void Exit()
    {
        player.rb.linearVelocity *= 0.25f;
    }
}
#endregion

#region Melee Attack
public class MeleeAttackState : PlayerState
{
    private float attackStartTime;
    public MeleeAttackState(PlayerManager player) : base(player) { }

    public override void Enter()
    {
        attackStartTime = Time.time;
        player.lastAttackTime = 0;
        player.canAttack = false;

        player.rb.linearVelocityX *= 0.25f;

        player.StartMeleeAttack(player.data.attackDuration);
    }

    public override void Update()
    {
        // allow jumping and dashing mid attack
        if (player.lastPressedJumpTime > 0 && player.lastOnGroundTime > 0)
        {
            player.stateMachine.ChangeState(player.jumpState);
        }

        // change state after attack finishes
        if (Time.time - attackStartTime > player.data.attackDuration)
        {
            if (player.lastOnGroundTime > 0)
                player.stateMachine.ChangeState(Mathf.Abs(player.moveInput.x) > 0.01
                    ? player.runState
                    : player.idleState);
            else
                player.stateMachine.ChangeState(player.fallState);
        }
    }
    public override void Exit()
    {

    }
}
#endregion
