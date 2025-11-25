using UnityEngine;
using FMODUnity;
using StarterAssets;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference footstepEvent;
    public EventReference landEvent;

    [Header("References")]
    public StarterAssetsInputs input;

    public void PlayFootstep()
    {
        FMOD.Studio.EventInstance stepInstance = RuntimeManager.CreateInstance(footstepEvent);
        RuntimeManager.AttachInstanceToGameObject(stepInstance, transform);

        float sprintValue = (input != null && input.sprint) ? 1f : 0f;
        stepInstance.setParameterByName("IsSprinting", sprintValue);

        stepInstance.start();
        stepInstance.release();
    }

    public void PlayLand()
    {
        RuntimeManager.PlayOneShot(landEvent, transform.position);
    }
}