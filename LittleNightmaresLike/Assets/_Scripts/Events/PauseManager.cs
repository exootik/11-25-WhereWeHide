using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    private static PauseManager _instance;
    public static PauseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PauseManager>();
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    private InputAction pauseAction;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (playerInput != null && playerInput.actions != null)
        {
            pauseAction = playerInput.actions.FindAction("Pause");
        }

        SetupHorrorButtons();
    }

    private void SetupHorrorButtons()
    {
        if (resumeButton != null && resumeButton.GetComponent<HorrorMenuButton>() == null)
            resumeButton.gameObject.AddComponent<HorrorMenuButton>();

        if (quitButton != null && quitButton.GetComponent<HorrorMenuButton>() == null)
            quitButton.gameObject.AddComponent<HorrorMenuButton>();
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Paused");
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Game Resumed");
    }

    private void QuitGame()
    {
        Debug.Log("Quitting Game");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}