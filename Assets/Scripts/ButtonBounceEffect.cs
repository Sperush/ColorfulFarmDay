using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonBounceEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;
    public float scaleFactor = 0.9f;
    public float bounceSpeed = 10f;

    private void Start()
    {
        originalScale = transform.localScale;
    }
    public void TriggerBounce()
    {
        StopAllCoroutines();
        transform.localScale = originalScale * scaleFactor;
        StartCoroutine(BounceBack());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.Instance.Play(GameSound.ButtonClick);
        transform.localScale = originalScale * scaleFactor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(BounceBack());
    }

    private System.Collections.IEnumerator BounceBack()
    {
        while (Vector3.Distance(transform.localScale, originalScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.unscaledDeltaTime * bounceSpeed);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}
