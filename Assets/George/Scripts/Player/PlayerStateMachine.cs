using UnityEngine;

public class PlayerStateMachine
{
    public PlayerState currentState { get; private set; }

    public void ChangeState(PlayerState newState)
    {
        if (currentState == newState) return;

        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void Update() => currentState?.Update();
    public void FixedUpdate() => currentState?.FixedUpdate();
}
