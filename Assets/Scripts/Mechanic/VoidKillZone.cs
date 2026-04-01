using UnityEngine;
using Chromatic.Environment;

[RequireComponent(typeof(BoxCollider2D))]
public class VoidKillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Platform"))
        {
            ColorObject colorObject = other.GetComponentInParent<ColorObject>();
            if (colorObject != null)
            {
                colorObject.ForceResetToInitialState();
            }
            return;
        }

        if (!other.CompareTag("Player")) return;
        Debug.Log("Player in void");

        var ph = other.GetComponent<PlayerHealth>();
        if (ph)
        {
            ph.Kill();
            return;
        }

        RespawnManager.Instance.RespawnPlayer(other.gameObject, other.transform.position);
    }
}
