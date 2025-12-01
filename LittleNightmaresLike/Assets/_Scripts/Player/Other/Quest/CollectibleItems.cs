using UnityEngine;
//fonctionnel
public class CollectibleItems : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool playSound = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            CollectItem();
        }
    }

    private void CollectItem()
    {
        QuestManager.Instance.UpdateQuestProgress(questId);

        //faire jouer un son ici 
        if (playSound)
        {
            //comme ça par exemple
            // AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        Destroy(gameObject);
    }
}