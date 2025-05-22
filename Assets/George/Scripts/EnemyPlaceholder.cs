using System;
using UnityEngine;

public class EnemyPlaceholder : MonoBehaviour
{
    #region Variables
    public float health = 100f;

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
