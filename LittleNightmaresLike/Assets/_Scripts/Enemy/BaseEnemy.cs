using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(NavMeshAgent))]
public class BaseEnemy : MonoBehaviour
{
    [Header("Common")]
    public Transform playerTransform;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Perception")]
    public Transform eyesTransform;
    public float viewDistance = 12f;
    [Range(0f, 180f)] public float viewAngle = 160f;
    public LayerMask obstructMask;
    public LayerMask playerMask;
    public float heightOffsetPlayer = 1.0f;

    [Header("Combat")]
    public float attackRange = 1.6f;
    public float catchRange = 1.2f;

    [Tooltip("Composant qui implémente IEnemyBehaviour")]
    public MonoBehaviour behaviourComponent;
    private IEnemyBehaviour behaviour;

    [Header("Screamer")]
    [SerializeField] private ScreamerUI screamerUI;
    private bool playerCaught = false;

    [Header("Audio")]
    [Tooltip("AudioSource pour les sons de chase (startChase)")]
    [SerializeField] private AudioSource chaseAudioSource;
    [Tooltip("AudioSource pour les footsteps")]
    [SerializeField] private AudioSource footstepAudioSource;
    [Tooltip("AudioSource pour la respiration (loop)")]
    [SerializeField] private AudioSource breathingAudioSource;
    [Tooltip("AudioSource pour le jumpscare")]
    [SerializeField] private AudioSource jumpscareAudioSource;

    [Header("Audio Clips")]
    [Tooltip("Son joué quand l'ennemi détecte le joueur")]
    [SerializeField] private AudioClip startChaseSound;
    [Tooltip("Son des pas de l'ennemi")]
    [SerializeField] private AudioClip enemyFootstepSound;
    [Tooltip("Son de respiration constant")]
    [SerializeField] private AudioClip breathingSound;
    [Tooltip("Son du jumpscare")]
    [SerializeField] private AudioClip jumpscareSound;

    [Header("Audio Settings")]
    [Tooltip("Intervalle entre les footsteps (secondes)")]
    [SerializeField] private float footstepInterval = 0.5f;
    [Tooltip("Volume de la respiration")]
    [SerializeField][Range(0f, 1f)] private float breathingVolume = 0.3f;

    [Header("Audio 3D Distances")]
    [Tooltip("Distance min où le son est au volume max")]
    [SerializeField] private float chaseMinDistance = 1f;
    [Tooltip("Distance max où le son de chase est audible")]
    [SerializeField] private float chaseMaxDistance = 20f;
    [Tooltip("Distance min pour footsteps")]
    [SerializeField] private float footstepMinDistance = 1f;
    [Tooltip("Distance max où les footsteps sont audibles")]
    [SerializeField] private float footstepMaxDistance = 15f;
    [Tooltip("Distance min pour respiration")]
    [SerializeField] private float breathingMinDistance = 0.5f;
    [Tooltip("Distance max où la respiration est audible")]
    [SerializeField] private float breathingMaxDistance = 8f;

    [Header("Audio Rolloff")]
    [Tooltip("Type de rolloff pour l'atténuation du son")]
    [SerializeField] private AudioRolloffMode audioRolloffMode = AudioRolloffMode.Logarithmic;

    private Coroutine _chaseCoroutine = null;
    private Coroutine _wakeCoroutine = null;
    private bool isChasing = false;
    public bool IsChasing => isChasing;

    private float footstepTimer = 0f;
    private bool isMoving = false;

    public event Action<BaseEnemy> OnPlayerCaught;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (behaviourComponent != null)
            behaviour = behaviourComponent as IEnemyBehaviour;

        if (behaviour != null) behaviour.Init(this);

        SetupAudioSources();
        StartBreathingSound();
    }

    private void SetupAudioSources()
    {
        if (chaseAudioSource == null)
        {
            GameObject chaseAudioObj = new GameObject("ChaseAudio");
            chaseAudioObj.transform.SetParent(transform);
            chaseAudioObj.transform.localPosition = Vector3.zero;
            chaseAudioSource = chaseAudioObj.AddComponent<AudioSource>();
        }

        chaseAudioSource.spatialBlend = 1f; 
        chaseAudioSource.rolloffMode = audioRolloffMode;
        chaseAudioSource.minDistance = chaseMinDistance;
        chaseAudioSource.maxDistance = chaseMaxDistance;
        chaseAudioSource.dopplerLevel = 0f; 
        chaseAudioSource.spread = 0f; 

        if (footstepAudioSource == null)
        {
            GameObject footstepAudioObj = new GameObject("FootstepAudio");
            footstepAudioObj.transform.SetParent(transform);
            footstepAudioObj.transform.localPosition = Vector3.zero;
            footstepAudioSource = footstepAudioObj.AddComponent<AudioSource>();
        }

        footstepAudioSource.spatialBlend = 1f;
        footstepAudioSource.rolloffMode = audioRolloffMode;
        footstepAudioSource.minDistance = footstepMinDistance;
        footstepAudioSource.maxDistance = footstepMaxDistance;
        footstepAudioSource.dopplerLevel = 0f;
        footstepAudioSource.spread = 30f;

        if (breathingAudioSource == null)
        {
            GameObject breathingAudioObj = new GameObject("BreathingAudio");
            breathingAudioObj.transform.SetParent(transform);
            breathingAudioObj.transform.localPosition = Vector3.zero;
            breathingAudioSource = breathingAudioObj.AddComponent<AudioSource>();
        }

        // tentative de configuration
        breathingAudioSource.spatialBlend = 1f;
        breathingAudioSource.rolloffMode = audioRolloffMode;
        breathingAudioSource.minDistance = breathingMinDistance;
        breathingAudioSource.maxDistance = breathingMaxDistance;
        breathingAudioSource.loop = true;
        breathingAudioSource.volume = breathingVolume;
        breathingAudioSource.dopplerLevel = 0f;
        breathingAudioSource.spread = 0f; 

        if (jumpscareAudioSource == null)
        {
            GameObject jumpscareAudioObj = new GameObject("JumpscareAudio");
            jumpscareAudioObj.transform.SetParent(transform);
            jumpscareAudioObj.transform.localPosition = Vector3.zero;
            jumpscareAudioSource = jumpscareAudioObj.AddComponent<AudioSource>();
        }

        jumpscareAudioSource.spatialBlend = 0f; 
        jumpscareAudioSource.volume = 1f;
    }

    private void Update()
    {
        // Update de l'ennemi :
        if (behaviour != null) behaviour.Tick();

        UpdateFootsteps();

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= catchRange && !playerCaught)
            {
                playerCaught = true;

                PlayJumpscareSound();

                if (screamerUI != null)
                {
                    screamerUI.ShowScreamer();
                }

                if (DeathManager.Instance != null)
                {
                    DeathManager.Instance.TriggerDeath();
                }

                OnPlayerCaught?.Invoke(this);
                agent.isStopped = true;

                PlayerController player = playerTransform.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.enabled = false;
                }
            }
            else if (dist <= attackRange)
            {
                animator.SetTrigger("Attack");
                agent.isStopped = true;
            }
        }

        if (CanSeePlayer())
            Debug.DrawLine(eyesTransform.position, playerTransform.position + Vector3.up * 1f, Color.green);
        else
            Debug.DrawLine(eyesTransform.position, playerTransform.position + Vector3.up * 1f, Color.red);
    }

    private void UpdateFootsteps()
    {
        if (agent != null && !agent.isStopped)
        {
            float velocity = agent.velocity.magnitude;
            isMoving = velocity > 0.1f;
        }
        else
        {
            isMoving = false;
        }

        if (isMoving)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayFootstep();
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void StartBreathingSound()
    {
        if (breathingAudioSource != null && breathingSound != null)
        {
            breathingAudioSource.clip = breathingSound;
            breathingAudioSource.Play();
        }
    }

    private void PlayStartChaseSound()
    {
        if (chaseAudioSource != null && startChaseSound != null)
        {
            chaseAudioSource.PlayOneShot(startChaseSound);
        }
    }

    private void PlayFootstep()
    {
        if (footstepAudioSource != null && enemyFootstepSound != null)
        {
            footstepAudioSource.PlayOneShot(enemyFootstepSound);
        }
    }

    private void PlayJumpscareSound()
    {
        if (jumpscareAudioSource != null && jumpscareSound != null)
        {
            jumpscareAudioSource.PlayOneShot(jumpscareSound);
        }
    }

    public bool CanSeePlayer()
    {
        if (eyesTransform == null || playerTransform == null) return false;

        Vector3 origin = eyesTransform.position;
        Vector3 targetPos = playerTransform.position + Vector3.up * heightOffsetPlayer;
        Vector3 dir = targetPos - origin;
        float distance = dir.magnitude;

        if (distance > viewDistance) return false;

        // angle de vision
        Vector3 forwardFlat = Vector3.ProjectOnPlane(eyesTransform.forward, Vector3.up).normalized;
        Vector3 dirFlat = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;

        float angleToPlayer = Vector3.Angle(forwardFlat, dirFlat);
        if (angleToPlayer > viewAngle * 0.5f)
        {
            Debug.DrawLine(origin, targetPos, Color.red);
            return false;
        }

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            // si on touche le joueur :
            if (hit.collider != null &&
                (hit.collider.transform == playerTransform || hit.collider.transform.IsChildOf(playerTransform) || hit.collider.CompareTag("Player")))
            {
                Debug.DrawLine(origin, hit.point, Color.green);
                return true;
            }
            // si ya un obstacle :
            Debug.DrawLine(origin, hit.point, Color.red);
            return false;
        }

        Debug.DrawLine(origin, targetPos, Color.red);
        return false;
    }

    public void StartChase()
    {
        if (isChasing) return;

        if (_chaseCoroutine != null) return;

        PlayStartChaseSound();

        if (animator != null)
        {
            animator.ResetTrigger("StartChase");
            animator.SetTrigger("StartChase");
        }

        if (agent != null)
            agent.isStopped = true;

        _chaseCoroutine = StartCoroutine(WaitAndBeginChase(1.2f));
    }

    public void ChasePlayer()
    {
        if (playerTransform == null) return;
        agent.SetDestination(playerTransform.position);
        Debug.Log("ChasePlayer");
    }

    public void StopChase(bool pauseAgent = false)
    {
        if (_chaseCoroutine != null)
        {
            StopCoroutine(_chaseCoroutine);
            _chaseCoroutine = null;
        }

        isChasing = false;

        if (animator != null)
        {
            animator.SetBool("IsChasing", false);
            animator.ResetTrigger("StartChase");
        }

        if (agent != null)
            agent.isStopped = pauseAgent;
        Debug.Log("StopChase");
    }

    public void PlayWalk(bool walking)
    {
        animator.SetBool("IsWalking", walking);
        Debug.Log("PlayWalk");
    }

    public void PlayIdle()
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsChasing", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsSleeping", false);
        Debug.Log("PlayIdle");
    }

    public void StartWakeUp(float waitSeconds = 1.0f)
    {
        if (isChasing) return;

        if (_wakeCoroutine != null) return;

        PlayStartChaseSound();

        if (animator != null)
        {
            animator.ResetTrigger("WakeUp");
            animator.SetTrigger("WakeUp");

            animator.SetBool("IsSleeping", true);
        }

        if (agent != null)
            agent.isStopped = true;

        _wakeCoroutine = StartCoroutine(WaitAndBeginWake(waitSeconds));
    }

    public void PlayRun(bool running)
    {
        if (animator != null)
        {
            animator.SetBool("IsRunning", running);

            if (running)
            {
                animator.SetBool("IsIdle", false);
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsChasing", false);
                animator.SetBool("IsSleeping", false);
            }
        }
        Debug.Log("PlayRun: " + running);
    }

    public void PlaySleep()
    {
        if (animator != null)
        {
            animator.ResetTrigger("WakeUp");
            animator.SetBool("IsSleeping", true);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsChasing", false);
        }

        if (agent != null) agent.isStopped = true;
        Debug.Log("PlaySleep");
    }

    private IEnumerator WaitAndBeginChase(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);

        isChasing = true;

        if (animator != null)
            animator.SetBool("IsChasing", true);

        if (agent != null)
        {
            agent.isStopped = false;
            if (playerTransform != null)
                agent.SetDestination(playerTransform.position);
        }

        _chaseCoroutine = null;
    }

    private IEnumerator WaitAndBeginWake(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);

        isChasing = true;

        if (animator != null)
        {
            animator.SetBool("IsSleeping", false);
            //animator.SetBool("IsIdle", true);
        }

        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRunning", true);
        }

        if (agent != null)
        {
            agent.isStopped = false;
            if (playerTransform != null)
                agent.SetDestination(playerTransform.position);
        }

        _wakeCoroutine = null;
    }

    private void OnDestroy()
    {
        if (breathingAudioSource != null && breathingAudioSource.isPlaying)
        {
            breathingAudioSource.Stop();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (eyesTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyesTransform.position, 0.05f);

        // Cone de vision
        Vector3 forward = eyesTransform.forward;
        Quaternion leftRot = Quaternion.Euler(0, -viewAngle * 0.5f, 0);
        Quaternion rightRot = Quaternion.Euler(0, viewAngle * 0.5f, 0);
        Vector3 leftDir = leftRot * forward * viewDistance;
        Vector3 rightDir = rightRot * forward * viewDistance;

        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawLine(eyesTransform.position, eyesTransform.position + leftDir);
        Gizmos.DrawLine(eyesTransform.position, eyesTransform.position + rightDir);

        if (chaseAudioSource != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, chaseMaxDistance);
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, chaseMinDistance);
        }

        if (footstepAudioSource != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, footstepMaxDistance);
        }

        if (breathingAudioSource != null)
        {
            Gizmos.color = new Color(0, 0, 1, 0.2f);
            Gizmos.DrawWireSphere(transform.position, breathingMaxDistance);
        }
    }
}