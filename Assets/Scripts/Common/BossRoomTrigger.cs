using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private ShutDoor door;
    [SerializeField] private RedWardenBoss boss;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;

        if (collision.CompareTag("Player"))
        {
            triggered = true;

            if (door != null)
            {
                door.StartClosing();
            }

            if (boss != null)
            {
                boss.ActivateBoss();
            }
        }
    }
}