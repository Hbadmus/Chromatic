using UnityEngine;
using Cinemachine;

public class CameraSwitchZone : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera areaCamera;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        areaCamera.Priority = activePriority;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        areaCamera.Priority = inactivePriority;
    }
}