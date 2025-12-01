using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HorrorMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.03f, 0f, 0f, 0.92f);
    [SerializeField] private Color titleColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private bool enableTitleFlicker = true;
    [SerializeField] private float flickerMinInterval = 0.1f;
    [SerializeField] private float flickerMaxInterval = 0.6f;

    private Coroutine flickerCoroutine;

    private void Awake()
    {
        if (backgroundPanel != null)
            backgroundPanel.color = backgroundColor;

        if (titleText != null)
            titleText.color = titleColor;
    }

    private void OnEnable()
    {
        StartCoroutine(FadeIn());

        if (enableTitleFlicker && titleText != null)
        {
            if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
            flickerCoroutine = StartCoroutine(TitleFlickerEffect());
        }
    }

    private void OnDisable()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        if (titleText != null)
            titleText.enabled = true;
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator TitleFlickerEffect()
    {
        while (true)
        {
            float waitTime = Random.Range(flickerMinInterval, flickerMaxInterval);
            yield return new WaitForSecondsRealtime(waitTime);

            if (titleText != null && Random.value < 0.15f)
            {
                titleText.enabled = false;
                yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.1f));
                titleText.enabled = true;
            }
        }
    }
}