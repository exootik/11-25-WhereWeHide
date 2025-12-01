using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Position de spawn")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Activer ce checkpoint au démarrage")]
    [SerializeField] private bool isDefaultCheckpoint = false;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject activeVisual; // faire apparaitre quelque chose au contact
    [SerializeField] private GameObject inactiveVisual; // l'inverse

    private bool isActivated = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (spawnPoint == null)
            spawnPoint = transform;

        if (isDefaultCheckpoint)
        {
            CheckpointManager.Instance.SetCheckpoint(spawnPoint);
            isActivated = true;
        }

        UpdateVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;
        CheckpointManager.Instance.SetCheckpoint(spawnPoint);
        UpdateVisuals();
        Debug.Log($"Checkpoint activé: {gameObject.name}");
    }

    private void UpdateVisuals()
    {
        if (activeVisual != null)
            activeVisual.SetActive(isActivated);
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isActivated);
    }
}