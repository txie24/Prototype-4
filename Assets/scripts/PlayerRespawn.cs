using UnityEngine;

public class WaterRespawn : MonoBehaviour
{
    public Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var cc = other.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        other.transform.position = respawnPoint.position;
        other.transform.rotation = respawnPoint.rotation;

        if (cc != null) cc.enabled = true;
    }
}
