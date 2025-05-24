using System;
using System.Collections;
using UnityEngine;

public class EnemyHealthHandler : MonoBehaviour
{
    #region Variables
    public float health = 100f;
    public float damageCooldown = 0.25f;
    private float lastKnockbackTime = 0f;

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
        if (Time.time - lastKnockbackTime < damageCooldown) return;
        if (health <= 0f) return;

        lastKnockbackTime = Time.time;
        health -= amount;
        StartCoroutine(FreezeFrame(0.05f));
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
     
        StartCoroutine(FreezeFrame(0.1f));

        dissolve.CallDissolve();

        Destroy(gameObject, dissolve.dissolveDuration);
    }

    private IEnumerator FreezeFrame(float duration)
    {
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
