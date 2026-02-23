using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenSentinelBoss : BaseBoss
{
    [Header("Tank Stats")]
    [SerializeField] private float bossMaxHealth = 150f;

    [Header("Vine Spawning")]
    [SerializeField] private GameObject vinePrefab;
    [SerializeField] private float phase1VineInterval = 15f;
    [SerializeField] private float phase2VineInterval = 5f;
    [SerializeField] private int maxVines = 10;
    [SerializeField] private float vineSpawnRadius = 3f;

    [Header("Regeneration")]
    [SerializeField] private float vineProximity = 3f;
    [SerializeField] private float regenAmount = 1f;
    [SerializeField] private float regenTickRate = 2f;

    [Header("Bullet Seed Attack")]
    [SerializeField] private float bulletSeedSpeed = 8f;
    [SerializeField] private float bulletSeedArcHeight = 3f;
    [SerializeField] private float bulletSeedDamage = 15f;
    [SerializeField] private float bulletSeedCooldown = 8f;
    [SerializeField] private GameObject seedVisualPrefab;

    [Header("Vine Slam Attack")]
    [SerializeField] private float vineSlamLength = 10f;
    [SerializeField] private float vineSlamExpandDuration = 0.5f;
    [SerializeField] private float vineSlamPauseDuration = 0.5f;
    [SerializeField] private float vineSlamDamage = 20f;
    [SerializeField] private float vineSlamCooldown = 12f;
    [SerializeField] private float slamTelegraphDuration = 0.7f;

    private List<GameObject> activeVines = new List<GameObject>();
    private GameObject player;
    private bool isActive = false;
    private bool isAttacking = false;
    private float lastBulletSeedTime;
    private float lastVineSlamTime;
    private int currentPhase = 1;

    protected override void Awake()
    {
        base.Awake();
        moveSpeed = 1.5f;
    }

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player");
        ActivateBoss(); //DELETE LATER
    }

    public void ActivateBoss()
    {
        isActive = true;
        StartCoroutine(VineSpawnRoutine());
        StartCoroutine(RegenerationRoutine());
    }

    protected override void FixedUpdate()
    {
        if (!isActive) return;

        if (!isAttacking)
        {
            UpdatePhase();
            base.FixedUpdate();
            DecideNextAttack();
        }
    }

    private void UpdatePhase()
    {
        float healthPercent = health.CurrentHealth / health.MaxHealth;

        if (healthPercent > 0.66f)
        {
            currentPhase = 1;
        }
        else if (healthPercent > 0.33f)
        {
            currentPhase = 2;
        }
    }

    public bool CanTakeDamage()
    {
        if (currentPhase == 1) return true;

        return !IsNearAnyVine();
    }

    private bool IsNearAnyVine()
    {
        activeVines.RemoveAll(v => v == null);

        foreach (GameObject vine in activeVines)
        {
            if (Vector2.Distance(transform.position, vine.transform.position) <= vineProximity)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator VineSpawnRoutine()
    {
        while (true)
        {
            float interval = currentPhase == 1 ? phase1VineInterval : phase2VineInterval;
            yield return new WaitForSeconds(interval);

            if (activeVines.Count < maxVines && player != null)
            {
                SpawnVineNearPlayer();
            }
        }
    }

    private void SpawnVineNearPlayer()
    {
        if (vinePrefab == null || player == null) return;

        Vector2 playerPos = player.transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * vineSpawnRadius;
        Vector2 spawnPos = playerPos + randomOffset;

        GameObject vine = Instantiate(vinePrefab, spawnPos, Quaternion.identity);
        activeVines.Add(vine);

        Vine vineScript = vine.GetComponent<Vine>();
        if (vineScript != null)
        {
            vineScript.SetBoss(this);
        }
    }

    private IEnumerator RegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenTickRate);

            if (IsNearAnyVine() && !health.IsDead)
            {
                health.Heal(regenAmount);
            }
        }
    }

    private void DecideNextAttack()
    {
        if (player == null) return;

        if (Time.time - lastBulletSeedTime >= bulletSeedCooldown)
        {
            StartCoroutine(PerformBulletSeed());
        }
        else if (Time.time - lastVineSlamTime >= vineSlamCooldown)
        {
            StartCoroutine(PerformVineSlam());
        }
    }

    private IEnumerator PerformBulletSeed()
    {
        isAttacking = true;
        lastBulletSeedTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);

        if (player != null)
        {
            Vector2 targetPos = player.transform.position;
            StartCoroutine(ShootBulletSeed(targetPos));
        }

        isAttacking = false;
    }

    private IEnumerator ShootBulletSeed(Vector2 targetPos)
    {
        GameObject seed = null;

        if (seedVisualPrefab != null)
        {
            seed = Instantiate(seedVisualPrefab, transform.position, Quaternion.identity);
        }

        Vector2 startPos = transform.position;
        Vector2 direction = (targetPos - startPos).normalized;
        float totalDistance = Vector2.Distance(startPos, targetPos);

        float travelTime = 0f;
        float duration = totalDistance / bulletSeedSpeed;

        while (travelTime < duration)
        {
            travelTime += Time.deltaTime;
            float t = travelTime / duration;

            float horizontalDist = totalDistance * t;
            float verticalOffset = bulletSeedArcHeight * Mathf.Sin(Mathf.PI * t);

            Vector2 newPos = startPos + direction * horizontalDist;
            newPos.y += verticalOffset;

            if (seed != null)
            {
                seed.transform.position = newPos;
            }

            Collider2D hit = Physics2D.OverlapCircle(newPos, 0.3f);
            if (hit != null)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeHazardDamage(bulletSeedDamage);
                    }

                    if (seed != null) Destroy(seed);
                    yield break;
                }
                else if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    SpawnVineAtPosition(newPos);
                    if (seed != null) Destroy(seed);
                    yield break;
                }
            }

            yield return null;
        }

        SpawnVineAtPosition(seed != null ? (Vector2)seed.transform.position : targetPos);
        if (seed != null) Destroy(seed);
    }

    private void SpawnVineAtPosition(Vector2 position)
    {
        if (vinePrefab != null && activeVines.Count < maxVines)
        {
            GameObject vine = Instantiate(vinePrefab, position, Quaternion.identity);
            activeVines.Add(vine);

            Vine vineScript = vine.GetComponent<Vine>();
            if (vineScript != null)
            {
                vineScript.SetBoss(this);
            }
        }
    }

    private IEnumerator PerformVineSlam()
    {
        isAttacking = true;
        lastVineSlamTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(VineSlamTelegraph());

        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            StartCoroutine(ExecuteVineSlam(direction));
        }

        isAttacking = false;
    }

    private IEnumerator VineSlamTelegraph()
    {
        Color originalColor = sprite.color;

        float elapsed = 0f;
        while (elapsed < slamTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slamTelegraphDuration;

            sprite.color = Color.Lerp(originalColor, Color.green, Mathf.PingPong(t * 4f, 1f));

            yield return null;
        }

        sprite.color = originalColor;
    }

    private IEnumerator ExecuteVineSlam(Vector2 direction)
    {
        GameObject slamVine = new GameObject("VineSlam");
        slamVine.transform.position = transform.position;

        SpriteRenderer slamSprite = slamVine.AddComponent<SpriteRenderer>();
        slamSprite.sprite = GetComponent<SpriteRenderer>().sprite;
        slamSprite.color = Color.gray;

        BoxCollider2D slamCollider = slamVine.AddComponent<BoxCollider2D>();
        slamCollider.enabled = false;
        slamCollider.isTrigger = true;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        slamVine.transform.rotation = Quaternion.Euler(0, 0, angle - 90);

        Vector3 baseScale = new Vector3(0.5f, 0f, 1f);

        float elapsed = 0f;
        while (elapsed < vineSlamExpandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vineSlamExpandDuration;

            slamVine.transform.localScale = new Vector3(baseScale.x, vineSlamLength * t, baseScale.z);

            yield return null;
        }

        slamSprite.color = Color.green;
        slamCollider.enabled = true;

        CheckSlamHit(slamVine);

        yield return new WaitForSeconds(vineSlamPauseDuration);

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;

            slamVine.transform.localScale = Vector3.Lerp(new Vector3(baseScale.x, vineSlamLength, baseScale.z), Vector3.zero, t);

            yield return null;
        }

        Destroy(slamVine);
    }

    private void CheckSlamHit(GameObject slamVine)
    {
        if (slamVine == null || player == null) return;

        float distance = Vector2.Distance(slamVine.transform.position, player.transform.position);

        if (distance <= vineSlamLength)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"Before: {playerHealth.CurrentHealth} HP");
                playerHealth.TakeHazardDamage(vineSlamDamage);
                Debug.Log($"After: {playerHealth.CurrentHealth} HP (dealt {vineSlamDamage} damage)");
            }
        }
    }

    public void RemoveVine(GameObject vine)
    {
        activeVines.Remove(vine);
    }
}