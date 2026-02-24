using UnityEngine;
using UnityEngine.UI;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private ShutDoor door;
    [SerializeField] private BaseBoss boss;
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
                if (boss is RedWardenBoss redBoss)
                {
                    redBoss.ActivateBoss();
                }
                else if (boss is GreenSentinelBoss greenBoss)
                {
                    greenBoss.ActivateBoss();
                }
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
                if (boss is RedWardenBoss redBoss)
                {
                    redBoss.ResetBoss();
                }
            }

            if (bossHealthBar != null)
            {
                bossHealthBar.SetActive(false);
            }
        }
    }
}