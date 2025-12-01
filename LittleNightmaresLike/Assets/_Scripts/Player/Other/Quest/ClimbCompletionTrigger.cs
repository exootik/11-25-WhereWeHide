using UnityEngine;
//a moitié tester
public class ClimbCompletionTrigger : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireClimbingState = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            QuestManager.Instance.UpdateQuestProgress(questId);
        }
    }
}