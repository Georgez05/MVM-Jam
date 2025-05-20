using System.Collections;
using UnityEngine;

public class CameraFollowObject : MonoBehaviour
{
    [Header("Flip Rotation Attributes")]
    [SerializeField] private float flipYRotationTime = 0.5f;


    private Transform playerTransform;

    private void Start()
    {
        playerTransform = PlayerManager.Instance.transform;
    }

    private void Update()
    {
        transform.position = playerTransform.position;
    }

    public void CallTurn()
    {
        LeanTween.rotateY(gameObject, DetermineEndRotation(), flipYRotationTime)
            .setEase(LeanTweenType.easeInOutSine);
    }

    private float DetermineEndRotation()
    {
        if (!PlayerManager.Instance.isFacingRight)
            return 180f;
        else
            return 0f;
    }
}
