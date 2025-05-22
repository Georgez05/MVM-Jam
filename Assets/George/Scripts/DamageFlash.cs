using System.Collections;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    #region Variables
    [Header("Damage Flash Attributes")]
    [ColorUsage(true, true)]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private AnimationCurve flashSpeedCurve;

    [Header("Private Variables")]
    private SpriteRenderer[] spriteRenderers;
    private Material[] materials;

    [Header("Coroutines")]
    private Coroutine damageFlashCoroutine;
    #endregion
    private void Awake()
    {
        // placeholder to get only one sprite and one material
        spriteRenderers = new SpriteRenderer[1];
        spriteRenderers[0] = GetComponent<SpriteRenderer>();

        materials = new Material[1];
        materials[0] = spriteRenderers[0].material;

        // // use this IF the enemy has multiple sprite renderers
        //spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        //materials = new Material[spriteRenderers.Length];
        //for (int i = 0; i < spriteRenderers.Length; i++)
        //{
        //    materials[i] = spriteRenderers[i].material;
        //}
    }

    public void CallDamageFlash()
    {
        // start flash coroutine
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);

        damageFlashCoroutine = StartCoroutine(DamageFlasher());
    }

    private IEnumerator DamageFlasher()
    {
        // set color
        SetFlashColor();

        // lerp flash amount
        float currentFlashAmount = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;

            // lerp flash amount
            currentFlashAmount = flashSpeedCurve.Evaluate(elapsedTime / flashDuration);
            SetFlashAmount(currentFlashAmount);
            yield return null;
        }

        SetFlashAmount(0f); // reset flash amount
    }

    private void SetFlashColor()
    {
        // sets color to all materials
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor("_FlashColor", flashColor);
        }
    }

    private void SetFlashAmount(float amount)
    {
        // sets flash amount to all materials
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat("_FlashAmount", amount);
        }
    }
}
