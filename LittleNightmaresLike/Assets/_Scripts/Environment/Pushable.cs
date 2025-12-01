using UnityEngine;
//demande obligatoirement un collider et un rigidbody
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Pushable : MonoBehaviour
{
    [Header("Trigger Zone")]
    [Tooltip("Taille de la zone trigger autour de l'objet")]
    [SerializeField] private Vector3 triggerSize = new Vector3(1.5f, 1.5f, 1.5f);

    private BoxCollider triggerCollider;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        CreateTriggerZone();

    }

    private void CreateTriggerZone()
    {
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        foreach (var col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
                break;
            }
        }

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.size = triggerSize;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetPushingState(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetPushingState(false);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, triggerSize);
    }
#endif
}