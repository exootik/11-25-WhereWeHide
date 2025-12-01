using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreamerUI : MonoBehaviour
{
    [Header("Screamer Settings")]
    [Tooltip("Les frames du GIF en ordre")]
    [SerializeField] private Sprite[] screamerFrames;

    [Tooltip("Durée d'affichage de chaque frame (en secondes)")]
    [SerializeField] private float frameRate = 0.05f;

    [Tooltip("Durée totale du screamer avant de désactiver")]
    [SerializeField] private float screamerDuration = 2f;

    [Tooltip("AudioClip du cri (optionnel)")]
    [SerializeField] private AudioClip screamerSound;

    [Header("UI References")]
    [SerializeField] private Image screamerImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (screamerImage == null)
        {
            screamerImage = GetComponent<Image>();
            if (screamerImage == null)
                Debug.LogError("image manquant screamer");
        }
        if (screamerFrames == null || screamerFrames.Length == 0)
        {
            Debug.LogError("screamer ui aucune frame");
        }
        Hide();
    }

    public void ShowScreamer()
    {

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ScreamerRoutine());
    }

    private IEnumerator ScreamerRoutine()
    {
        if (screamerFrames == null || screamerFrames.Length == 0)
        {
            Debug.LogError("pas de frame dans le screamer");
            yield break;
        }

        if (screamerImage == null)
        {
            Debug.Log("Screamer manquant image");
            yield break;
        }


        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (audioSource != null && screamerSound != null)
        {
            audioSource.PlayOneShot(screamerSound);
            if (showDebugLogs)
                Debug.Log("ScreamerUI: Son joué");
        }
        float elapsed = 0f;
        int frameIndex = 0;

        while (elapsed < screamerDuration)
        {
            if (screamerFrames != null && screamerFrames.Length > 0)
            {
                screamerImage.sprite = screamerFrames[frameIndex];
                frameIndex = (frameIndex + 1) % screamerFrames.Length;
            }

            yield return new WaitForSeconds(frameRate);
            elapsed += frameRate;
        }

        if (showDebugLogs)
            Debug.Log("ScreamerUI: Animation terminée");

        Hide();
        OnScreamerComplete();
    }

    private void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnScreamerComplete()
    {

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }
    }

    [ContextMenu("Test Screamer")]
    private void TestScreamer()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ShowScreamer();
    }
}