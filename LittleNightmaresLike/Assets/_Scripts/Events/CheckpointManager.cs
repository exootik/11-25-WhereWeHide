using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    private static CheckpointManager _instance;
    public static CheckpointManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CheckpointManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CheckpointManager");
                    _instance = go.AddComponent<CheckpointManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Checkpoint")]
    [SerializeField] private Transform lastCheckpoint;
    [SerializeField] private Vector3 defaultSpawnPosition = Vector3.zero;
    [SerializeField] private Quaternion defaultSpawnRotation = Quaternion.identity;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        lastCheckpoint = checkpoint;
        Debug.Log($"Checkpoint mis à jour: {checkpoint.name}");
    }

    public Vector3 GetLastCheckpointPosition()
    {
        if (lastCheckpoint != null)
            return lastCheckpoint.position;
        return defaultSpawnPosition;
    }

    public Quaternion GetLastCheckpointRotation()
    {
        if (lastCheckpoint != null)
            return lastCheckpoint.rotation;
        return defaultSpawnRotation;
    }

    public void SetDefaultSpawn(Vector3 position, Quaternion rotation)
    {
        defaultSpawnPosition = position;
        defaultSpawnRotation = rotation;
    }
}