using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Button))]
public class HorrorMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Image buttonBackground;

    [Header("Normal State")]
    [SerializeField] private Color normalTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color normalBgColor = new Color(0.1f, 0.05f, 0.05f, 0.7f);

    [Header("Hover State")]
    [SerializeField] private Color hoverTextColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color hoverBgColor = new Color(0.2f, 0.05f, 0.05f, 0.9f);
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float transitionSpeed = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float hoverVolume = 0.3f;
    [SerializeField] private float clickVolume = 0.7f;

    private Vector3 originalScale;
    private Button button;
    private Coroutine hoverCoroutine;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        if (buttonBackground == null)
            buttonBackground = GetComponent<Image>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        ApplyNormalState();

        button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClick);
    }

    private void ApplyNormalState()
    {
        if (buttonText != null)
            buttonText.color = normalTextColor;

        if (buttonBackground != null)
            buttonBackground.color = normalBgColor;

        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        PlayHoverSound();

        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        hoverCoroutine = StartCoroutine(TransitionToHover());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        hoverCoroutine = StartCoroutine(TransitionToNormal());
    }

    private void OnButtonClick()
    {
        PlayClickSound();
    }

    private IEnumerator TransitionToHover()
    {
        float elapsed = 0f;
        Color startTextColor = buttonText != null ? buttonText.color : normalTextColor;
        Color startBgColor = buttonBackground != null ? buttonBackground.color : normalBgColor;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * hoverScale;

        while (elapsed < transitionSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionSpeed;

            if (buttonText != null)
                buttonText.color = Color.Lerp(startTextColor, hoverTextColor, t);

            if (buttonBackground != null)
                buttonBackground.color = Color.Lerp(startBgColor, hoverBgColor, t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        if (buttonText != null)
            buttonText.color = hoverTextColor;

        if (buttonBackground != null)
            buttonBackground.color = hoverBgColor;

        transform.localScale = targetScale;
    }

    private IEnumerator TransitionToNormal()
    {
        float elapsed = 0f;
        Color startTextColor = buttonText != null ? buttonText.color : hoverTextColor;
        Color startBgColor = buttonBackground != null ? buttonBackground.color : hoverBgColor;
        Vector3 startScale = transform.localScale;

        while (elapsed < transitionSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionSpeed;

            if (buttonText != null)
                buttonText.color = Color.Lerp(startTextColor, normalTextColor, t);

            if (buttonBackground != null)
                buttonBackground.color = Color.Lerp(startBgColor, normalBgColor, t);

            transform.localScale = Vector3.Lerp(startScale, originalScale, t);

            yield return null;
        }

        ApplyNormalState();
    }

    private void PlayHoverSound()
    {
        if (audioSource != null && hoverSound != null)
        {
            audioSource.volume = hoverVolume;
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(hoverSound);
        }
    }

    private void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.volume = clickVolume;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clickSound);
        }
    }
}