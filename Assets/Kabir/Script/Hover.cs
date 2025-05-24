using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Or TMPro if using TextMeshPro

public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.1f;
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private bool isHovering = false;
    private TextMeshProUGUI text; // Or TMP_Text

    private void Awake()
    {
        originalScale = transform.localScale;
        text = GetComponent<TextMeshProUGUI>(); // Replace with TMP_Text if needed
    }

    private void Update()
    {
        Update_Text();
    }

    private void Update_Text()
    {
        Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}
