using UnityEngine;

public class PlayerObjectFollower : MonoBehaviour
{
    public Transform playerTransform;
    public Vector2 offset;

    private void LateUpdate()
    {
        if (playerTransform != null)
            transform.position = (Vector2)playerTransform.position + offset;
    }
}
