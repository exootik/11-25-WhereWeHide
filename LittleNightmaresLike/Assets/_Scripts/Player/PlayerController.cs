using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    public enum MovementState
    {
        Idle,
        Walking,
        Running,
        Crouching,
        Jumping,
        Climbing,
        ClimbingRope,
        Hiding,
        Flying
    }

    [Header("Déplacement")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 4.2f;
    [SerializeField] private float crouchSpeed = 1.6f;
    [SerializeField] private float pushSpeed = 1.2f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float flySpeed = 5f;

    [Header("Saut / Sol")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float extraAirGravity = 10f;
    [SerializeField] private float groundedCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Fly mode")]
    [SerializeField] private bool isFlying = false;
    [Tooltip("Accéleration en vol")]
    [SerializeField] private float flyAcceleration = 25f;
    [Tooltip("Gravité en vol plus c'est bas plus tu planes")]
    [SerializeField] private float flyGravity = 0.5f;
    [Tooltip("Vitesse de déplacement horizontal en vol")]
    [SerializeField] private float flyForwardSpeed = 6f;
    [Tooltip("Vitesse de rotation pendant le vol")]
    [SerializeField] private float flyRotationSpeed = 8f;
    [Tooltip("Vol automatique ou controlé")]
    [SerializeField] private bool allowFlyDirectionControl = true;
    [Tooltip("Prefab de l'avion qui apparaît pendant le vol")]
    [SerializeField] private GameObject airplanePrefab;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchingHeight = 1.0f;
    [SerializeField] private float heightLerpSpeed = 12f;

    [Header("Input System (Asset)")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Waking Up")]
    [Tooltip("Durée animation wakingUp")]
    [SerializeField] private float wakingUpDuration = 2f;

    [Header("Climb")]
    [Tooltip("Speed quand on auto climb")]
    [SerializeField] private float climbUpSpeed = 2.2f;
    [Tooltip("Horizontal alignement vitesse")]
    [SerializeField] private float climbAlignSpeed = 10f;
    [Tooltip("Mask")]
    [SerializeField] private LayerMask climbableMask;
    [Tooltip("Distance maximum à check")]
    [SerializeField] private float forwardClimbProbe = 0.6f;

    [Header("Camera")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] CinemachineCamera vcamTopDown;
    [SerializeField] int priority = 10;

    [Header("Sounds")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip footSteps;
    [SerializeField] private AudioClip wallClimb;
    [SerializeField] private AudioClip ropeClimb;
    [Tooltip("Intervalle entre les pas (secondes)")]
    [SerializeField] private float footstepInterval = 0.5f;
    [Tooltip("Intervalle entre les sons de climb (secondes)")]
    [SerializeField] private float climbSoundInterval = 0.8f;

    private AudioSource ad;
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private MovementState currentState = MovementState.Idle;
    [SerializeField] private bool isGrounded;
    private bool isCrouching;
    private bool isPushing;
    private bool isWakingUp;

    public bool IsPushing => isPushing;

    private bool isClimbing;
    private int climbMode;
    private Climbable currentClimbable;
    private Coroutine climbRoutine;

    //intervalle climb et footstep
    private float footstepTimer;
    private float climbSoundTimer;

    private InputAction _iaMove, _iaRun, _iaCrouch, _iaJump, _iaFly, _iaTest1, _iaTest2, _iaTest3;

    private Vector2 moveVector;
    private bool runHeld;
    private bool jumpQueued;
    private bool flyQueued;
    private bool test1;
    private bool test2;
    private bool test3;

    public MovementState CurrentState => currentState;
    public bool IsGrounded => isGrounded;
    public bool IsClimbing => isClimbing;
    public bool IsFlying => isFlying;

    private void Awake()
    {
        ad = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        ResolveActions();

        isCrouching = false;
        isPushing = false;
        isClimbing = false;
        climbMode = 0;
        isFlying = false;
        isWakingUp = true;

        footstepTimer = 0f;
        climbSoundTimer = 0f;

        if (airplanePrefab != null)
            airplanePrefab.SetActive(false);

        if (cameraPivot == null)
        {
            var found = transform.Find("CameraPivot");
            if (found != null) cameraPivot = found;
        }
    }

    private void Start()
    {
        StartCoroutine(WakingUpRoutine());
    }

    private IEnumerator WakingUpRoutine()
    {
        vcamTopDown.Priority = priority;
        isWakingUp = true;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetTrigger("WakingUp");
        }

        yield return new WaitForSeconds(wakingUpDuration);

        rb.isKinematic = false;
        rb.useGravity = true;
        isWakingUp = false;
    }
        
    private void OnEnable()
    {
        EnableActions(true);

        if (_iaMove == null && playerInput != null) ResolveActions();

        if (_iaCrouch != null)
        {
            _iaCrouch.performed -= OnCrouchToggle; // defensive remove
            _iaCrouch.performed += OnCrouchToggle;
        }
    }

    private void OnDisable()
    {
        if (_iaCrouch != null)
            _iaCrouch.performed -= OnCrouchToggle;

        EnableActions(false);
    }

    private void OnDestroy()
    {
        if (_iaCrouch != null)
            _iaCrouch.performed -= OnCrouchToggle;

        if (climbRoutine != null) StopCoroutine(climbRoutine);
    }

    private void OnCrouchToggle(InputAction.CallbackContext ctx)
    {
        if (isGrounded && !isClimbing && !isWakingUp)
            isCrouching = !isCrouching;
    }

    private void Update()
    {
        if (isWakingUp)
        {
            UpdateAnimator();
            return;
        }

        PollInputs();
        UpdateCrouch();
        UpdateStateMachine();
        UpdateAnimator();
        UpdateTestStates();
        UpdateAirplaneVisibility();
        UpdateSounds();
    }

    private void FixedUpdate()
    {
        GroundCheck();

        if (isWakingUp)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (!isClimbing)
        {
            if (isFlying)
            {
                HandleFlying();
            }
            else
            {
                HandleMovement();
                HandleJump();
                ApplyExtraGravity();
            }

            HandleFlyActivation();
        }

        jumpQueued = false;
        flyQueued = false;
    }

    private void UpdateAirplaneVisibility()
    {
        if (airplanePrefab != null)
        {
            airplanePrefab.SetActive(isFlying);
        }
    }

    private void UpdateTestStates()
    {
        if (test2)
        {
            isClimbing = true;
            climbMode = 1;
        }
        else if (test3)
        {
            isClimbing = true;
            climbMode = 2;
        }
        else if (currentClimbable == null)
        {
            isClimbing = false;
            climbMode = 0;
        }
    }

    private void UpdateSounds()
    {
        if (isGrounded && !isClimbing && !isWakingUp)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float speed = horizontalVel.magnitude;

            if (speed > 0.1f)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    PlaySound(footSteps);
                    footstepTimer = footstepInterval;
                }
            }
            else
            {
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        if (isClimbing)
        {
            climbSoundTimer -= Time.deltaTime;
            if (climbSoundTimer <= 0f)
            {
                if (climbMode == 1) 
                {
                    PlaySound(wallClimb);
                }
                else if (climbMode == 2) 
                {
                    PlaySound(ropeClimb);
                }
                climbSoundTimer = climbSoundInterval;
            }
        }
        else
        {
            climbSoundTimer = 0f;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (ad != null && clip != null)
        {
            ad.PlayOneShot(clip);
        }
    }

    private void ResolveActions()
    {
        if (_iaCrouch != null)
            _iaCrouch.performed -= OnCrouchToggle;

        if (playerInput && playerInput.actions)
        {
            _iaMove = playerInput.actions.FindAction("Move");
            _iaRun = playerInput.actions.FindAction("Sprint");
            _iaCrouch = playerInput.actions.FindAction("Crouch");
            _iaJump = playerInput.actions.FindAction("Jump");
            _iaFly = playerInput.actions.FindAction("Fly");
            _iaTest1 = playerInput.actions.FindAction("Test1");
            _iaTest2 = playerInput.actions.FindAction("Test2");
            _iaTest3 = playerInput.actions.FindAction("Test3");
        }
        else
        {
            _iaMove = _iaRun = _iaCrouch = _iaJump = _iaFly = _iaTest1 = _iaTest2 = _iaTest3 = null;
        }
    }

    private void EnableActions(bool enable)
    {
        if (_iaMove != null) { if (enable) _iaMove.Enable(); else _iaMove.Disable(); }
        if (_iaRun != null) { if (enable) _iaRun.Enable(); else _iaRun.Disable(); }
        if (_iaCrouch != null) { if (enable) _iaCrouch.Enable(); else _iaCrouch.Disable(); }
        if (_iaJump != null) { if (enable) _iaJump.Enable(); else _iaJump.Disable(); }
        if (_iaFly != null) { if (enable) _iaFly.Enable(); else _iaFly.Disable(); }
        if (_iaTest1 != null) { if (enable) _iaTest1.Enable(); else _iaTest1.Disable(); }
        if (_iaTest2 != null) { if (enable) _iaTest2.Enable(); else _iaTest2.Disable(); }
        if (_iaTest3 != null) { if (enable) _iaTest3.Enable(); else _iaTest3.Disable(); }
    }

    private void PollInputs()
    {
        moveVector = _iaMove != null ? _iaMove.ReadValue<Vector2>() : Vector2.zero;
        runHeld = _iaRun != null && _iaRun.IsPressed();
        if (_iaJump != null && _iaJump.triggered)
            jumpQueued = true;
        if (_iaFly != null && _iaFly.triggered)
            flyQueued = true;
        test1 = _iaTest1 != null && _iaTest1.IsPressed();
        test2 = _iaTest2 != null && _iaTest2.IsPressed();
        test3 = _iaTest3 != null && _iaTest3.IsPressed();
    }

    private void GroundCheck()
    {
        Vector3 checkPos;
        if (capsule)
        {
            float bottomY = transform.position.y;
            checkPos = new Vector3(transform.position.x, bottomY, transform.position.z);
        }
        else
        {
            checkPos = transform.position;
        }
        isGrounded = Physics.CheckSphere(checkPos, groundedCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded && isFlying)
        {
            isFlying = false;
            rb.useGravity = true;
        }
    }

    public void SetPushingState(bool pushing)
    {
        isPushing = pushing;
    }

    private void UpdateCrouch()
    {
        if (isClimbing || isFlying || isWakingUp) return;

        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        float newHeight = Mathf.Lerp(capsule.height, targetHeight, Time.deltaTime * heightLerpSpeed);
        capsule.height = newHeight;
        capsule.center = new Vector3(0, newHeight * 0.5f, 0);
    }

    private void HandleFlyActivation()
    {
        if (flyQueued && !isGrounded && rb.linearVelocity.y > 0f && !isFlying && !isClimbing && !isWakingUp)
        {
            isFlying = true;
            rb.useGravity = false;
        }
    }

    private void HandleFlying()
    {
        Vector3 currentVel = rb.linearVelocity;
        Vector3 targetVelocity = currentVel;

        Vector3 inputDir = new Vector3(moveVector.x, 0, moveVector.y);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        // Choix Cam
        Transform camT = null;
        if (cameraPivot != null && !cameraPivot.IsChildOf(transform))
            camT = cameraPivot;
        else if (Camera.main != null)
            camT = Camera.main.transform;
        else
            camT = transform;

        Vector3 camForward = camT.forward;
        Vector3 camRight = camT.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        if (allowFlyDirectionControl && moveVector.sqrMagnitude > 0.01f)
        {
            Vector3 desiredDir = camForward * inputDir.z + camRight * inputDir.x;
            if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();
            Vector3 flyDirection = desiredDir * flyForwardSpeed;

            Vector3 horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
            Vector3 desiredHorizontalVel = Vector3.MoveTowards(
                horizontalVel,
                flyDirection,
                flyAcceleration * Time.fixedDeltaTime
            );

            targetVelocity.x = desiredHorizontalVel.x;
            targetVelocity.z = desiredHorizontalVel.z;

            if (desiredHorizontalVel.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredHorizontalVel.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * flyRotationSpeed);
            }
        }
        else
        {
            Vector3 forwardMovement = camForward * flyForwardSpeed;
            targetVelocity.x = Mathf.Lerp(currentVel.x, forwardMovement.x, flyAcceleration * Time.fixedDeltaTime);
            targetVelocity.z = Mathf.Lerp(currentVel.z, forwardMovement.z, flyAcceleration * Time.fixedDeltaTime);
        }

        targetVelocity.y = currentVel.y - (flyGravity * Time.fixedDeltaTime);

        rb.linearVelocity = targetVelocity;
    }

    private void HandleMovement()
    {
        if (isClimbing || isFlying || isWakingUp) return;

        Vector3 inputDir = new Vector3(moveVector.x, 0, moveVector.y);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        float baseSpeed = walkSpeed;
        if (runHeld && !isCrouching && !isPushing) baseSpeed = runSpeed;
        if (isCrouching) baseSpeed = crouchSpeed;
        if (isPushing) baseSpeed = pushSpeed;

        Transform camT = null;
        if (cameraPivot != null && !cameraPivot.IsChildOf(transform))
            camT = cameraPivot;
        else if (Camera.main != null)
            camT = Camera.main.transform;
        else
        {
            camT = transform;
        }


        Vector3 camForward = camT.forward;
        Vector3 camRight = camT.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveWorld = camForward * inputDir.z + camRight * inputDir.x;
        if (moveWorld.sqrMagnitude > 1f) moveWorld.Normalize();

        Vector3 targetHorizontalVel = moveWorld * baseSpeed;
        Vector3 currentVel = rb.linearVelocity;
        Vector3 desiredVel = Vector3.MoveTowards(
            new Vector3(currentVel.x, 0, currentVel.z),
            targetHorizontalVel,
            acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(desiredVel.x, currentVel.y, desiredVel.z);

        const float rotationThreshold = 0.05f;
        if (desiredVel.sqrMagnitude > rotationThreshold * rotationThreshold)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredVel.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 12f);
        }
    }


    private void HandleJump()
    {
        if (isClimbing || isFlying || isWakingUp) return;
        if (!jumpQueued) return;
        if (!isGrounded) return;
        if (isCrouching) return;
        if (isPushing) return;

        float jumpVel = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);
        Vector3 vel = rb.linearVelocity;
        vel.y = jumpVel;
        rb.linearVelocity = vel;
        currentState = MovementState.Jumping;

        // Play jump sound
        PlaySound(jumpSound);

        if (animator != null)
            animator.SetTrigger("Jump");
    }

    private void ApplyExtraGravity()
    {
        if (isClimbing || isFlying || isWakingUp) return;
        if (!isGrounded)
            rb.AddForce(Vector3.down * extraAirGravity, ForceMode.Acceleration);
    }

    private void UpdateStateMachine()
    {
        if (isWakingUp)
        {
            currentState = MovementState.Idle;
            return;
        }

        if (isFlying)
        {
            currentState = MovementState.Flying;
            return;
        }

        if (isClimbing)
        {
            currentState = (climbMode == 2) ? MovementState.ClimbingRope : MovementState.Climbing;
            return;
        }

        if (!isGrounded)
        {
            currentState = MovementState.Jumping;
            return;
        }

        Vector3 horizontal = rb.linearVelocity;
        horizontal.y = 0;
        float mag = horizontal.magnitude;

        if (mag < 0.05f)
            currentState = isCrouching ? MovementState.Crouching : MovementState.Idle;
        else
        {
            if (isCrouching) currentState = MovementState.Crouching;
            else if (runHeld && !isPushing) currentState = MovementState.Running;
            else currentState = MovementState.Walking;
        }
    }

    private void UpdateAnimator()
    {
        if (!animator) return;

        animator.SetBool("IsWakingUp", isWakingUp);
        animator.SetFloat("MoveSpeed", new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);

        animator.SetBool("IsPushing", isPushing);
        animator.SetBool("IsClimbing", isClimbing);
        animator.SetInteger("ClimbMode", climbMode);

        animator.SetBool("IsFlying", isFlying);

        bool isFalling = (!isGrounded) && (rb.linearVelocity.y < -0.1f) && !isFlying;
        animator.SetBool("IsFalling", isFalling);
    }

    public void BeginClimb(Climbable climbable)
    {
        if (isClimbing || climbable == null || isWakingUp) return;
        if (currentState != MovementState.Jumping && rb.linearVelocity.y <= 0f) return;

        if (isFlying)
        {
            isFlying = false;
            rb.useGravity = true;
        }

        currentClimbable = climbable;
        climbMode = climbable.ClimbMode;
        isClimbing = true;

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        if (climbRoutine != null) StopCoroutine(climbRoutine);
        climbRoutine = StartCoroutine(ClimbRoutine());
    }

    public void InterruptClimb()
    {
        if (!isClimbing) return;
        if (climbRoutine != null) StopCoroutine(climbRoutine);
        climbRoutine = null;
        rb.useGravity = true;
        isClimbing = false;
        currentClimbable = null;
        climbMode = 0;
    }

    private IEnumerator ClimbRoutine()
    {
        if (currentClimbable != null)
        {
            Vector3 targetXZ = currentClimbable.GetMountHorizontalPosition(transform.position);
            float horizontalTimer = 0f;
            while (horizontalTimer < 0.25f)
            {
                horizontalTimer += Time.deltaTime;
                Vector3 pos = transform.position;
                Vector3 desired = new Vector3(targetXZ.x, pos.y, targetXZ.z);
                transform.position = Vector3.MoveTowards(pos, desired, climbAlignSpeed * Time.deltaTime);
                yield return null;
            }

            Vector3 topPoint = currentClimbable.GetTopPoint();
            while ((transform.position - topPoint).sqrMagnitude > 0.01f)
            {
                Vector3 next = Vector3.MoveTowards(transform.position, topPoint, climbUpSpeed * Time.deltaTime);
                transform.position = next;
                yield return null;
            }
        }

        rb.useGravity = true;
        isClimbing = false;
        currentClimbable = null;
        climbMode = 0;
        climbRoutine = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isClimbing || isWakingUp) return;
        if (!collision.collider || !IsJumpingUpward()) return;

        if ((climbableMask.value & (1 << collision.collider.gameObject.layer)) != 0 ||
            collision.collider.CompareTag("Climbable"))
        {
            Climbable climbable = collision.collider.GetComponent<Climbable>();
            if (climbable != null)
                BeginClimb(climbable);
        }
    }

    private bool IsJumpingUpward()
    {
        return currentState == MovementState.Jumping && rb.linearVelocity.y > 0.05f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 checkPos = transform.position;
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap)
        {
            float bottomY = transform.position.y;
            checkPos = new Vector3(transform.position.x, bottomY, transform.position.z);
        }
        Gizmos.DrawWireSphere(checkPos, groundedCheckRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.9f,
                        transform.position + Vector3.up * 0.9f + transform.forward * forwardClimbProbe);
    }
#endif
}