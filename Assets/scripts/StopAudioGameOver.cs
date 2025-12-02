using UnityEngine;
using FMODUnity; 

public class StopAudioGameOver : MonoBehaviour
{
    void Start()
    {

        FMOD.Studio.Bus masterBus = RuntimeManager.GetBus("bus:/");

        masterBus.stopAllEvents(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}