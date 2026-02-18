using System.Collections;
using UnityEngine;

public class LavaHazard : MonoBehaviour
{
    [SerializeField] private float damage = .1f;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float tickRate = 2f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth player = collision.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damage);
                StartCoroutine(DamageOverTime(player));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StopAllCoroutines();
        }
    }

    private IEnumerator DamageOverTime(PlayerHealth player)
    {
        while (true)
        {
            yield return new WaitForSeconds(tickRate);

            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
    }
}