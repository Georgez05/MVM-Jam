using System;
using Unity.Cinemachine;
using UnityEngine;

public class RecoilOnHit : MonoBehaviour
{
    #region Variables
    [Header("Recoil Triggers")]
    [SerializeField] private LayerMask recoilLayers;

    [Header("References")]
    private Rigidbody2D playerRB;
    #endregion

    private void Awake()
    {
        playerRB = gameObject.GetComponentInParent<Rigidbody2D>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & recoilLayers) != 0)
        {
            ApplyRecoil();
        }
    }

    private void ApplyRecoil()
    {
        Vector2 direction = PlayerManager.Instance.isFacingRight ? Vector2.left : Vector2.right;

        playerRB.linearVelocity = Vector2.zero;
        playerRB.AddForce(direction.normalized * PlayerManager.Instance.data.recoilForce, ForceMode2D.Impulse);
    }
}
