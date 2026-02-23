using System.Collections;
using UnityEngine;
using Chromatic.Combat;

public class Vine : MonoBehaviour, IInteractiveTarget
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 15f;
 
    [Header("Whip Attack")]
    [SerializeField] private float whipRange = 2f;
    [SerializeField] private float whipDamage = 8f;
    [SerializeField] private float whipCooldown = 1.5f;
    [SerializeField] private float whipDuration = 0.3f;

    private float currentHealth;
    private GreenSentinelBoss boss;
    private GameObject player;
    private float lastWhipTime;
    private bool isWhipping = false;
    private SpriteRenderer sprite;

    private void Awake()
    {
        currentHealth = maxHealth;
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (!isWhipping && player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= whipRange && Time.time - lastWhipTime >= whipCooldown)
            {
                StartCoroutine(WhipAttack());
            }
        }
    }

    public void SetBoss(GreenSentinelBoss bossRef)
    {
        boss = bossRef;
    }

    public void OnHit(float damage, Color color)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            DestroyVine();
        }
    }

    private IEnumerator WhipAttack()
    {
        isWhipping = true;
        lastWhipTime = Time.time;

        Vector3 originalScale = transform.localScale;
        Quaternion originalRotation = transform.rotation;

        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        Color originalColor = sprite.color;

        float elapsed = 0f;
        while (elapsed < whipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / whipDuration;

            transform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(0, 0, angle), t);
            transform.localScale = Vector3.Lerp(originalScale, new Vector3(originalScale.x * 2f, originalScale.y, originalScale.z), t);
            sprite.color = Color.green;

            yield return null;
        }

        CheckWhipHit();

        yield return new WaitForSeconds(0.2f);

        transform.rotation = originalRotation;
        transform.localScale = originalScale;
        sprite.color = originalColor;

        isWhipping = false;
    }

    private void CheckWhipHit()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance <= whipRange * 2f)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeHazardDamage(whipDamage);
            }
        }
    }

    private void DestroyVine()
    {
        if (boss != null)
        {
            boss.RemoveVine(gameObject);
        }

        Destroy(gameObject);
    }
}