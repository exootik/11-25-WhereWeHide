using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float typeSpeed = 0.05f;
    [SerializeField] private bool enableTypingEffect = true; // animation (les lettres s'écrivent une par une)

    [Header("Audio")]
    [SerializeField] private AudioSource dialogueAudioSource;

    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private CanvasGroup canvasGroup;
    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
        }

        if (dialogueAudioSource == null)
        {
            dialogueAudioSource = gameObject.GetComponent<AudioSource>();
            if (dialogueAudioSource == null)
            {
                dialogueAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        dialoguePanel.SetActive(false);
        canvasGroup.alpha = 0f;

        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }
    }

    private void Update()
    {
        if (isDialogueActive && interactAction != null && interactAction.WasPressedThisFrame())
        {
            if (isTyping)
            {
                CompleteCurrentLine();
            }
            else
            {
                DisplayNextLine();
            }
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (isDialogueActive || dialogue == null)
        {
            Debug.LogWarning("Dialogue déjà actif ou DialogueData null");
            return;
        }

        if (dialogue.dialogueLines == null || dialogue.dialogueLines.Length == 0)
        {
            Debug.LogError("Aucune data dans le dialogue data");
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;
        npcNameText.text = dialogue.npcName;

        StartCoroutine(ShowDialoguePanel());
    }

    private IEnumerator ShowDialoguePanel()
    {
        dialoguePanel.SetActive(true);
        //le fade
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (currentLineIndex < currentDialogue.dialogueLines.Length)
        {
            string line = currentDialogue.dialogueLines[currentLineIndex];

            StopDialogueSound();

            PlayDialogueSound();

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (enableTypingEffect)
            {
                typingCoroutine = StartCoroutine(TypeLine(line));
            }
            else
            {
                dialogueText.text = line;
                isTyping = false;
            }
        }
        else
        {
            EndDialogue();
        }
    }

    private void PlayDialogueSound()
    {
        if (dialogueAudioSource != null && currentDialogue != null && currentDialogue.npcDialogueSound != null)
        {
            dialogueAudioSource.PlayOneShot(currentDialogue.npcDialogueSound);
        }
    }

    private void StopDialogueSound()
    {
        if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
        {
            dialogueAudioSource.Stop();
        }
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            //animation d'écriture
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    private void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text = currentDialogue.dialogueLines[currentLineIndex];
        isTyping = false;
    }

    private void DisplayNextLine()
    {
        currentLineIndex++;
        DisplayCurrentLine();
    }

    private void EndDialogue()
    {
        StopDialogueSound();
        StartCoroutine(HideDialoguePanel());
    }

    private IEnumerator HideDialoguePanel()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        currentDialogue = null;
        currentLineIndex = 0;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}