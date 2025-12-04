using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuManager : MonoBehaviour
{
    private static StartMenuManager _instance;
    public static StartMenuManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<StartMenuManager>();
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject infoInputPause;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float clickVolume = 0.7f;

    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;

    private bool gameStarted = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        SetupStartMenu();
    }

    private void Start()
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(true);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartGame);
    }

    private void SetupStartMenu()
    {
        Time.timeScale = 0f;

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnStartGame()
    {
        if (gameStarted) return;

        gameStarted = true;

        PlayClickSound();

        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);

        Time.timeScale = 1f;

        StartCoroutine(ShowInfoInputPause(20));

        if (playerController != null)
            playerController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Game Started");
    }

    private void PlayClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.volume = clickVolume;
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    private IEnumerator ShowInfoInputPause(float waitSeconds)
    {
        infoInputPause.SetActive(true);

        yield return new WaitForSeconds(waitSeconds);

        infoInputPause.SetActive(false) ;
    }
}