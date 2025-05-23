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
            ApplyRecoil(collision);
        }
    }

    private void ApplyRecoil(Collider2D collision)
    {
        Vector2 playerDirection = PlayerManager.Instance.isFacingRight ? Vector2.left : Vector2.right;

        // apply recoil to the player
        playerRB.linearVelocity = Vector2.zero;
        playerRB.AddForce(playerDirection.normalized * PlayerManager.Instance.data.recoilForce, ForceMode2D.Impulse);

        // apply recoil to enemy if it has a rigidbody
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Rigidbody2D enemyRB = collision.gameObject.GetComponent<Rigidbody2D>();
            if (enemyRB != null)
            {
                Vector2 enemyDirection = -playerDirection;
                enemyRB.linearVelocity = Vector2.zero;
                enemyRB.AddForce(enemyDirection.normalized * PlayerManager.Instance.data.recoilForce, ForceMode2D.Impulse);
            }
        }
    }
}
