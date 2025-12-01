using UnityEngine;
//pas encore testé
public class PushableQuestTarget : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float completionDistance = 1f;
    [SerializeField] private string pushableTag = "Pushable";

    private bool questCompleted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (questCompleted) return;

        if (other.CompareTag(pushableTag))
        {
            float distance = Vector3.Distance(other.transform.position, targetPosition.position);

            if (distance <= completionDistance)
            {
                QuestManager.Instance.UpdateQuestProgress(questId);
                questCompleted = true;
                Debug.Log("objet poussé à la bonne position");
            }
        }
    }
}