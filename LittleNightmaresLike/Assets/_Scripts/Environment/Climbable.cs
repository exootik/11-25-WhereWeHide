using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Climbable : MonoBehaviour
{
    public enum ClimbType
    {
        Wall = 1,
        Rope = 2
    }

    [Header("Climbable")]
    [Tooltip("Style de climb")]
    [SerializeField] private ClimbType climbType = ClimbType.Wall;

    [Tooltip("Un top point où ira notre joueur qui climb si null défaut est ses bounds + offset")]
    [SerializeField] private Transform topPoint;

    [Tooltip("Offset si top point null")]
    [SerializeField] private float topOffset = 0.2f;

    [Tooltip("Forcer le collider à être on trigger selon le mode")]
    [SerializeField] private bool makeTrigger = true;

    [Tooltip("Autoriser des trigger-bases montée")]
    [SerializeField] private bool enableTriggerMount = true;

    [Tooltip("Optionnel : alignement horizontal")]
    [SerializeField] private bool alignHorizontally = true;

    private Collider _col;

    public int ClimbMode => (int)climbType;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        if (makeTrigger && _col) _col.isTrigger = true;
        gameObject.tag = gameObject.tag == "Untagged" ? "Climbable" : gameObject.tag; 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enableTriggerMount) return;
        if (!_col || !_col.isTrigger) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        // Nécessite d'être en jump
        if (!pc.IsClimbing && pc.CurrentState == PlayerController.MovementState.Jumping)
        {
            // seulement si la vélocité verticale est positive
            pc.BeginClimb(this);
        }
    }

    // Top point
    public Vector3 GetTopPoint()
    {
        if (topPoint != null) return topPoint.position;
        if (_col != null)
        {
            Bounds b = _col.bounds;
            return new Vector3(b.center.x, b.max.y + topOffset, b.center.z);
        }
        return transform.position + Vector3.up * (1f + topOffset);
    }

    // Alignement horizontal
    public Vector3 GetMountHorizontalPosition(Vector3 playerPosition)
    {
        if (!alignHorizontally) return playerPosition;
        if (_col == null) return playerPosition;
        Bounds b = _col.bounds;
        return new Vector3(b.center.x, playerPosition.y, b.center.z);
    }

    //les gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 top = topPoint != null ? topPoint.position : GetTopPoint();
        Gizmos.DrawSphere(top, 0.08f);
        Gizmos.DrawLine(transform.position, top);
    }
#endif
}