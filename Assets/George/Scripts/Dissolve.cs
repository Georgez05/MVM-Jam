using System;
using System.Collections;
using UnityEngine;

public class Dissolve : MonoBehaviour
{
    #region Variables
    public float dissolveDuration = 0.75f;

    private SpriteRenderer[] spriteRenderers;
    private Material[] materials;

    private int dissolveAmount = Shader.PropertyToID("_DissolveAmount");
    #endregion

    private void Start()
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

    public void CallDissolve()
    {
        // start flash coroutine
        StartCoroutine(DissolveCoroutine());
    }

    private IEnumerator DissolveCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;

            // lerp dissolve amount
            float currentDissolveAmount = Mathf.Lerp(0f, 1.1f, elapsedTime / dissolveDuration);
            
            // set dissolve amount
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].SetFloat(dissolveAmount, currentDissolveAmount);
            }

            yield return null;
        }
    }

    private IEnumerator AppearCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;

            // lerp dissolve amount
            float currentDissolveAmount = Mathf.Lerp(1.1f, 0f, elapsedTime / dissolveDuration);

            // set dissolve amount
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].SetFloat(dissolveAmount, currentDissolveAmount);
            }

            yield return null;
        }
    }
}
