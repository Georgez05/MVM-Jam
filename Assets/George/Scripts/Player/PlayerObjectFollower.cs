using UnityEngine;

public class PlayerObjectFollower : MonoBehaviour
{
    public Transform target; // player
    public Vector2 offset;

    private void LateUpdate()
    {
        if (target != null)
            transform.position = (Vector2)target.position + offset;
    }
}
