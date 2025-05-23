using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    #region Variables
    [SerializeField] private ParticleSystem hitParticlesPrefab;
    #endregion

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealthHandler enemy = collision.GetComponent<EnemyHealthHandler>();
        if (enemy != null)
        {
            Vector2 hitDirection = (enemy.transform.position - transform.position).normalized;
            float damage = PlayerManager.Instance.data.attackDamage;

            enemy.TakeDamage(damage);
            SpawnHitParticles(enemy.transform.position, hitDirection);
        }
    }

    #region Visual FX 
    private void SpawnHitParticles(Vector2 enemyPos, Vector2 hitDirection)
    {
        // Calculate the angle in degrees
        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;

        Instantiate(hitParticlesPrefab, enemyPos, Quaternion.Euler(0, 0, angle));
    }
    #endregion
}
