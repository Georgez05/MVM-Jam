using System;
using UnityEngine;

public class EnemyHealthHandler : MonoBehaviour
{
    #region Variables
    public float health = 100f;
    public float damageCooldown = 0.25f;
    private float lastDamageTime = 0f;

    private DamageFlash damageFlash;
    private Dissolve dissolve;
    #endregion
    private void Awake()
    {
        damageFlash = GetComponent<DamageFlash>();
        dissolve = GetComponent<Dissolve>();
    }
    public void TakeDamage(float amount)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;
        if (health <= 0f) return;

        lastDamageTime = Time.time;
        health -= amount;

        if (health <= 0f)
        {
            Die();
        }

        // damage flash effect
        damageFlash.CallDamageFlash();
    }

    private void Die()
    {
        Debug.Log("Enemy died");
        // Call the dissolve effect
        dissolve.CallDissolve();

        Destroy(gameObject, dissolve.dissolveDuration);
    }
}
