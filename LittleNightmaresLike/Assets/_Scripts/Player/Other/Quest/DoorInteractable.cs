using UnityEngine;
using UnityEngine.InputSystem;
//fonctionnel
public class DoorInteractable : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private string requiredItemQuestId;
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private bool destroyOnOpen = false;

    private bool playerInRange = false;
    private bool isOpen = false;

    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }
    }

    private void Update()
    {
        if (playerInRange && !isOpen && interactAction != null)
        {
            if (interactAction.WasPressedThisFrame())
            {
                OpenDoor();
            }
        }
    }

    private void OpenDoor()
    {
        Quest currentQuest = QuestManager.Instance.GetCurrentQuest();

        if (currentQuest != null && currentQuest.questId == requiredItemQuestId)
        {
            QuestManager.Instance.UpdateQuestProgress(requiredItemQuestId);
            isOpen = true;

            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger("Open");
            }

            if (destroyOnOpen)
            {
                Destroy(gameObject);
            }
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
}