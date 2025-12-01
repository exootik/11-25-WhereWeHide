using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider))]
public class CameraByTrigger : MonoBehaviour
{
    [SerializeField] CinemachineCamera vcam;
    [SerializeField] float durationOfActivation = 3f;
    [SerializeField] bool restoreOnExit = true;
    [SerializeField] bool isAlreadyActivated = false;

    [SerializeField] int overridePriority = 20;

    [Header("Option Input Lock")]
    [SerializeField] PlayerInput playerInput;
    [SerializeField] bool disablePlayerInputSimple = true;
    [SerializeField] string actionMapOnCut = "UI";
    [SerializeField] string actionMapGameplay = "Player";

    int prevPriority;
    Coroutine autoCoroutine;
    bool triggered = false;
    bool currentlyActive = false;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isAlreadyActivated && triggered) return;
        if (!other.CompareTag("Player")) return;
        if (vcam == null)
        {
            Debug.LogWarning("CameraTriggerActivate : vcam non assignée", this);
            return;
        }

        triggered = true;
        Activate();
    }

    void OnTriggerExit(Collider other)
    {
        if (!restoreOnExit) return;
        if (!other.CompareTag("Player")) return;
        if (!currentlyActive) return;
        Restore();
    }

    void Activate()
    {
        if (currentlyActive) return;

        isAlreadyActivated = true;
        prevPriority = vcam.Priority;
        vcam.Priority = overridePriority;
        currentlyActive = true;

        if (playerInput != null)
        {
            if (disablePlayerInputSimple) playerInput.enabled = false;
            else playerInput.SwitchCurrentActionMap(actionMapOnCut);
        }

        if (durationOfActivation > 0f)
        {
            if (autoCoroutine != null) StopCoroutine(autoCoroutine);
            autoCoroutine = StartCoroutine(AutoRestoreAfter(durationOfActivation));
        }
    }

    IEnumerator AutoRestoreAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        Restore();
    }

    public void Restore()
    {
        if (!currentlyActive) return;

        if (autoCoroutine != null) { StopCoroutine(autoCoroutine); autoCoroutine = null; }

        vcam.Priority = prevPriority;
        currentlyActive = false;

        if (playerInput != null)
        {
            if (disablePlayerInputSimple) playerInput.enabled = true;
            else playerInput.SwitchCurrentActionMap(actionMapGameplay);
        }
    }

    public void ActivateFromCode(float seconds = -1f)
    {
        if (seconds > 0f) durationOfActivation = seconds;
        Activate();
    }
}
