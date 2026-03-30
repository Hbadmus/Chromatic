using System.Collections;
using UnityEngine;
using Chromatic.Combat;

public class Vine : MonoBehaviour, IInteractiveTarget
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 30f;

    [Header("Whip Attack")]
    [SerializeField] private float whipRange = 3f;
    [SerializeField] private float whipDamage = 8f;
    [SerializeField] private float whipCooldown = 1f;
    [SerializeField] private float whipDuration = 0.3f;
    [SerializeField] private float warningRange = 4f;

    [Header("Growth")]
    [SerializeField] private float growthDuration = 0.5f;

    private float currentHealth;
    private object boss;
    private GameObject player;
    private float lastWhipTime;
    private bool isWhipping = false;
    private bool isGrowing = true;
    private SpriteRenderer[] allSprites;

    private void Awake()
    {
        currentHealth = maxHealth;
        allSprites = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(GrowVine());
    }

    private IEnumerator GrowVine()
    {
        Vector3 targetScale = transform.localScale;
        transform.localScale = new Vector3(targetScale.x, 0, targetScale.z);

        float elapsed = 0f;

        while (elapsed < growthDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growthDuration;

            transform.localScale = Vector3.Lerp(new Vector3(targetScale.x, 0, targetScale.z), targetScale, t);

            yield return null;
        }

        transform.localScale = targetScale;
        isGrowing = false;
    }

    private void Update()
    {
        if (player == null || isGrowing) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance <= warningRange && distance > whipRange && !isWhipping)
        {
            float pulseSpeed = distance < whipRange * 1.5f ? 3f : 1.5f;
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            Color pulseColor = Color.Lerp(Color.gray, Color.green, t * 0.3f);

            foreach (SpriteRenderer sprite in allSprites)
            {
                sprite.color = pulseColor;
            }

            Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), Time.deltaTime * 2f);
        }

        if (!isWhipping && distance <= whipRange && Time.time - lastWhipTime >= whipCooldown)
        {
            StartCoroutine(WhipAttack());
        }
    }

    public void SetBoss(GreenSentinelBoss bossRef)
    {
        boss = bossRef;
    }

    public void SetBossForFinalBoss(VoidTyrantBoss bossRef)
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

        float elapsed = 0f;
        while (elapsed < whipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / whipDuration;

            transform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(0, 0, angle), t);
            transform.localScale = Vector3.Lerp(originalScale, new Vector3(originalScale.x * 2f, originalScale.y, originalScale.z), t);

            foreach (SpriteRenderer sprite in allSprites)
            {
                sprite.color = Color.green;
            }

            yield return null;
        }

        CheckWhipHit();

        yield return new WaitForSeconds(0.2f);

        transform.rotation = Quaternion.Euler(0, 0, 0);
        transform.localScale = originalScale;

        foreach (SpriteRenderer sprite in allSprites)
        {
            sprite.color = Color.gray;
        }

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
        if (boss is GreenSentinelBoss greenBoss)
        {
            greenBoss.RemoveVine(gameObject);
        }
        else if (boss is VoidTyrantBoss voidBoss)
        {
            voidBoss.RemoveVine(gameObject);
        }

        Destroy(gameObject);
    }
}