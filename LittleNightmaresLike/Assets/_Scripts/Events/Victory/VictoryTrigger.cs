using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VictoryTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Déclencher la victoire au trigger du joueur")]
    [SerializeField] private bool triggerOnEnter = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter || hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            TriggerVictory();
        }
    }

    public void TriggerVictory()
    {
        if (hasTriggered) return;

        hasTriggered = true;

        if (VictoryMenuManager.Instance != null)
        {
            VictoryMenuManager.Instance.ShowVictoryMenu();
        }

        Debug.Log("Victory Triggered!");
    }

    [ContextMenu("Trigger Victory Manually")]
    private void TriggerVictoryManually()
    {
        TriggerVictory();
    }
}