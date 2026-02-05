using UnityEngine;
using UnityEngine.UI;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private ShutDoor door;
    [SerializeField] private RedWardenBoss boss;
    [SerializeField] private GameObject bossHealthBar;

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

            if (bossHealthBar != null)
            {
                bossHealthBar.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (boss == null) triggered = true; 
            else triggered = false;

            if (door != null)
            {
                door.OpenDoor();
            }

            if (boss != null)
            {
                boss.ResetBoss();
            }

            if (bossHealthBar != null)
            {
                bossHealthBar.SetActive(false);
            }
        }
    }
}

