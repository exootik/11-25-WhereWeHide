using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class VictoryMenuManager : MonoBehaviour
{
    private static VictoryMenuManager _instance;
    public static VictoryMenuManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<VictoryMenuManager>();
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject victoryMenuPanel;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float clickVolume = 0.7f;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;

    private bool isVictoryShown = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (victoryMenuPanel != null)
            victoryMenuPanel.SetActive(false);

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgain);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnDestroy()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveListener(OnPlayAgain);
    }

    public void ShowVictoryMenu()
    {
        if (isVictoryShown) return;

        isVictoryShown = true;

        if (victoryMenuPanel != null)
        {
            victoryMenuPanel.SetActive(true);
            StartCoroutine(FadeIn());
        }

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Victory Menu Shown");
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

    private void OnPlayAgain()
    {
        PlayClickSound();

        Time.timeScale = 1f;

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    private void PlayClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.volume = clickVolume;
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    [ContextMenu("Test Victory Menu")]
    private void TestVictoryMenu()
    {
        ShowVictoryMenu();
    }
}