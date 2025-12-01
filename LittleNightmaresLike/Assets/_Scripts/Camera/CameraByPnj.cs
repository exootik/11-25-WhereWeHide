using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraByPnj : MonoBehaviour
{
    [SerializeField] CinemachineCamera vcam;
    [SerializeField] int overridePriority = 20;

    [Header("Input lock")]
    [SerializeField] PlayerInput playerInput;
    [SerializeField] bool disablePlayerInputSimple = true;
    [SerializeField] string actionMapOnCut = "UI";
    [SerializeField] string actionMapGameplay = "Player";

    int prevPriority;
    Coroutine autoCoroutine;
    bool active = false;

    public void Activate(float seconds = 30f, bool disableInput = true)
    {
        if (vcam == null)
        {
            Debug.LogWarning("NPCConversationCamera : vcam non assignée", this);
            return;
        }

        if (active)
        {
            if (autoCoroutine != null)
            {
                StopCoroutine(autoCoroutine);
                autoCoroutine = null;
            }
        }
        else
        {
            prevPriority = vcam.Priority;
            vcam.Priority = overridePriority;
            active = true;
        }

        if (disableInput && playerInput != null)
        {
            if (disablePlayerInputSimple) playerInput.enabled = false;
            else playerInput.SwitchCurrentActionMap(actionMapOnCut);
        }

        if (seconds > 0f)
        {
            autoCoroutine = StartCoroutine(AutoDeactivateAfter(seconds, disableInput));
        }
    }

    IEnumerator AutoDeactivateAfter(float sec, bool restoreInput)
    {
        yield return new WaitForSeconds(sec);
        Deactivate(restoreInput);
    }

    public void Deactivate(bool restoreInput = true)
    {
        if (!active) return;

        if (autoCoroutine != null) { StopCoroutine(autoCoroutine); autoCoroutine = null; }

        vcam.Priority = prevPriority;
        active = false;

        if (restoreInput && playerInput != null)
        {
            if (disablePlayerInputSimple) playerInput.enabled = true;
            else playerInput.SwitchCurrentActionMap(actionMapGameplay);
        }
    }
}
