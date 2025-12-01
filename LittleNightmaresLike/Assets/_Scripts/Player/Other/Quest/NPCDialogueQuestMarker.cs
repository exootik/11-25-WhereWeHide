using UnityEngine;
using UnityEngine.InputSystem;

public class NPCDialogueQuestMarker : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private DialogueData dialogueData;

    [SerializeField] private CameraByPnj cameraByPnj;

    private bool playerInRange = false;
    private bool dialogueCompleted = false;
    private bool hasStartedDialogue = false;

    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }

        if (cameraByPnj == null)
        {
            cameraByPnj = GetComponent<CameraByPnj>();
            if (cameraByPnj == null)
            {
                cameraByPnj = GetComponentInChildren<CameraByPnj>();
            }
        }
    }

    private void Update()
    {
        if (playerInRange && !dialogueCompleted && !DialogueManager.Instance.IsDialogueActive()
            && interactAction != null && interactAction.WasPressedThisFrame())
        {
            StartDialogue();
        }

        if (hasStartedDialogue && !DialogueManager.Instance.IsDialogueActive() && !dialogueCompleted)
        {
            if (cameraByPnj != null)
            {
                cameraByPnj.Deactivate(restoreInput: true);
            }

            QuestManager.Instance.UpdateQuestProgress(questId);
            dialogueCompleted = true;
            hasStartedDialogue = false;
        }
    }

    private void StartDialogue()
    {
        if (dialogueData != null)
        {
            if (cameraByPnj != null)
            {
                cameraByPnj.Activate(seconds: 0f, disableInput: true);
            }

            DialogueManager.Instance.StartDialogue(dialogueData);
            hasStartedDialogue = true;
        }
        else
        {
            Debug.LogError("pas de dialogue data au npc");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void OnDisable()
    {
        if (hasStartedDialogue && cameraByPnj != null)
        {
            cameraByPnj.Deactivate(restoreInput: true);
            hasStartedDialogue = false;
        }
    }

    private void OnDestroy()
    {
        if (hasStartedDialogue && cameraByPnj != null)
        {
            cameraByPnj.Deactivate(restoreInput: true);
            hasStartedDialogue = false;
        }
    }
}