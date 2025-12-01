using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DeathManager : MonoBehaviour
{
    private static DeathManager _instance;
    public static DeathManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DeathManager>();
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject deathMenuPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private CanvasGroup deathCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float deathScreenDelay = 2f;
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;

    private bool isDead = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (deathMenuPanel != null)
            deathMenuPanel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    private void OnDestroy()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
    }

    public void TriggerDeath()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player is dead");

        if (playerController != null)
            playerController.enabled = false;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathScreenDelay);

        ShowDeathMenu();
    }

    private void ShowDeathMenu()
    {
        if (deathMenuPanel != null)
        {
            deathMenuPanel.SetActive(true);
            StartCoroutine(FadeInDeathMenu());
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator FadeInDeathMenu()
    {
        if (deathCanvasGroup == null) yield break;

        deathCanvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            deathCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        deathCanvasGroup.alpha = 1f;
    }

    private void OnRetryClicked()
    {
        Respawn();
    }

    private void Respawn()
    {
        isDead = false;

        if (deathMenuPanel != null)
            deathMenuPanel.SetActive(false);

        Vector3 spawnPos = CheckpointManager.Instance.GetLastCheckpointPosition();
        Quaternion spawnRot = CheckpointManager.Instance.GetLastCheckpointRotation();

        if (playerController != null)
        {
            playerController.transform.position = spawnPos;
            playerController.transform.rotation = spawnRot;

            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            playerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Player respawned at checkpoint");
    }

    [ContextMenu("Test Death")]
    private void TestDeath()
    {
        TriggerDeath();
    }
}