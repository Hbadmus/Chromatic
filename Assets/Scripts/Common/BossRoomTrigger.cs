using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private ShutDoor door;
    [SerializeField] private BaseBoss boss;
    [SerializeField] private GameObject bossHealthBar;
    [SerializeField] private bool keepBossInsideRoom = true;

    private bool triggered = false;
    private Vector3 bossInitialPosition;

    private void Awake()
    {
        if (boss != null)
        {
            bossInitialPosition = boss.transform.position;
        }
    }

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
                bossInitialPosition = boss.transform.position;

                if (boss is RedWardenBoss redBoss)
                {
                    redBoss.ActivateBoss();
                }
                else if (boss is GreenSentinelBoss greenBoss)
                {
                    greenBoss.ActivateBoss();
                }
                else if (boss is VoidTyrantBoss voidBoss)
                {
                    voidBoss.ActivateBoss();
                }
            }

            if (bossHealthBar != null)
            {
                bossHealthBar.SetActive(true);
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBossRoomMusic();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (keepBossInsideRoom && triggered && IsBossCollider(collision))
        {
            TeleportBossBack();
            return;
        }

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
                else if (boss is GreenSentinelBoss greenBoss)
                {
                    greenBoss.ResetBoss();
                }
                else if (boss is VoidTyrantBoss voidBoss)
                {
                    voidBoss.ResetBoss();
                }
            }

            if (bossHealthBar != null)
            {
                bossHealthBar.SetActive(false);
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayDefaultMusic();
            }
        }
    }

    private bool IsBossCollider(Collider2D collision)
    {
        if (boss == null || collision == null)
        {
            return false;
        }

        if (collision.gameObject == boss.gameObject)
        {
            return true;
        }

        return collision.attachedRigidbody != null && collision.attachedRigidbody.gameObject == boss.gameObject;
    }

    private void TeleportBossBack()
    {
        if (boss == null)
        {
            return;
        }

        boss.transform.position = bossInitialPosition;

        Rigidbody2D bossRb = boss.GetComponent<Rigidbody2D>();
        if (bossRb != null)
        {
            bossRb.linearVelocity = Vector2.zero;
            bossRb.angularVelocity = 0f;
        }
    }
}