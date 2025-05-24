using Spine.Unity;
using System.Collections;
using UnityEngine;

public class SpineDamageFlash : MonoBehaviour
{
    #region Variables
    [Header("Private Variables")]
    private SkeletonAnimation skeletonAnimation;
    private Material originalMaterial;
    private Coroutine flashCoroutine;

    [Header("Damage Flash Attributes")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.15f;
    #endregion

    private void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();

        originalMaterial = skeletonAnimation.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial;    
    }

    public void CallDamageFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        var matOverride = skeletonAnimation.CustomMaterialOverride;
        matOverride[originalMaterial] = flashMaterial;

        yield return new WaitForSeconds(flashDuration);

        matOverride.Remove(originalMaterial);
    }
}
