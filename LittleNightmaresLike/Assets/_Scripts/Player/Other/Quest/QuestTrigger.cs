using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyAfterTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            QuestManager.Instance.UpdateQuestProgress(questId);

            if (destroyAfterTrigger)
            {
                Destroy(gameObject);
            }
        }
    }
}