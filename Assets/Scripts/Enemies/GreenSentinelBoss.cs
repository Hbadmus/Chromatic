using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenSentinelBoss : BaseBoss
{
    [Header("Tank Stats")]
    [SerializeField] private float bossMaxHealth = 150f;

    [Header("Vine Spawning")]
    [SerializeField] private GameObject vinePrefab;
    [SerializeField] private GameObject ingrainVinePrefab;
    [SerializeField] private float phase1VineInterval = 15f;
    [SerializeField] private float phase2VineInterval = 5f;
    [SerializeField] private int maxVines = 10;
    [SerializeField] private float vineSpawnRadius = 3f;

    [Header("Regeneration")]
    [SerializeField] private float vineProximity = 3f;
    [SerializeField] private float regenAmount = 1f;
    [SerializeField] private float regenTickRate = 2f;
    [SerializeField] private float ingrainRegenRate = 5f;

    [Header("Bullet Seed Attack")]
    [SerializeField] private float bulletSeedSpeed = 8f;
    [SerializeField] private float bulletSeedArcHeight = 3f;
    [SerializeField] private float bulletSeedDamage = 15f;
    [SerializeField] private GameObject seedVisualPrefab;

    [Header("Seed Barrage")]
    [SerializeField] private int barrageCount = 4;
    [SerializeField] private float barrageSpreadAngle = 45f;

    [Header("Vine Slam Attack")]
    [SerializeField] private float vineSlamLength = 10f;
    [SerializeField] private float vineSlamExpandDuration = 0.5f;
    [SerializeField] private float vineSlamPauseDuration = 0.5f;
    [SerializeField] private float vineSlamDamage = 20f;

    [Header("Ranged Poke")]
    [SerializeField] private float pokeDistance = 10f;
    [SerializeField] private float pokeDamage = 8f;
    [SerializeField] private float pokeCooldown = 6f;
    [SerializeField] private float pokeSpeed = 15f;

    [Header("Ingrain Ability")]
    [SerializeField] private float ingrainDuration = 8f;
    [SerializeField] private float ingrainTelegraphDuration = 1f;
    [SerializeField] private int ingrainVineCount = 5;
    [SerializeField] private float ingrainVineRadius = 4f;

    [Header("AI Settings")]
    [SerializeField] private float closeRange = 4f;
    [SerializeField] private float farRange = 8f;
    [SerializeField] private float lowVineThreshold = 4f;
    [SerializeField] private float isolatedSpeedMultiplier = 1.5f;

    private List<GameObject> activeVines = new List<GameObject>();
    private List<GameObject> ingrainVines = new List<GameObject>();
    private GameObject player;
    private bool isActive = false;
    private bool isAttacking = false;
    private bool isIngraining = false;

    private float lastBulletSeedTime = -999f;
    private float lastVineSlamTime = -999f;
    private float lastPokeTime = -999f;

    private float bulletSeedCooldown = 8f;
    private float vineSlamCooldown = 12f;

    private int currentPhase = 1;
    private bool hasUsedIngrain50 = false;
    private bool hasUsedIngrain15 = false;
    private float currentSpeed;

    protected override void Awake()
    {
        base.Awake();
        moveSpeed = 1.5f;
        currentSpeed = moveSpeed;
    }

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void ActivateBoss()
    {
        isActive = true;

        if (player != null)
        {
            Vector2 directionToPlayer = player.transform.position - transform.position;
            movingRight = directionToPlayer.x > 0;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (movingRight ? 1 : -1);
            transform.localScale = scale;
        }

        StartCoroutine(VineSpawnRoutine());
        StartCoroutine(RegenerationRoutine());
    }

    protected override void FixedUpdate()
    {
        if (!isActive || isIngraining) return;

        if (!isAttacking)
        {
            UpdatePhase();
            CheckIngrainTrigger();
            UpdateSpeed();
            base.FixedUpdate();
            MakeSmartDecision();
        }
    }

    protected override void Move()
    {
        if (isAttacking) return;

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);
    }

    private void UpdateSpeed()
    {
        if (IsNearAnyVine())
        {
            currentSpeed = moveSpeed;
        }
        else
        {
            currentSpeed = moveSpeed * isolatedSpeedMultiplier;
        }
    }

    private void UpdatePhase()
    {
        float healthPercent = health.CurrentHealth / health.MaxHealth;

        if (healthPercent > 0.66f)
        {
            currentPhase = 1;
            bulletSeedCooldown = 8f;
            vineSlamCooldown = 12f;
        }
        else if (healthPercent > 0.33f)
        {
            currentPhase = 2;
            bulletSeedCooldown = 5f;
            vineSlamCooldown = 8f;
        }
        else
        {
            currentPhase = 3;
            bulletSeedCooldown = 3f;
            vineSlamCooldown = 5f;
        }
    }

    private void CheckIngrainTrigger()
    {
        float healthPercent = health.CurrentHealth / health.MaxHealth;

        if (healthPercent <= 0.5f && !hasUsedIngrain50)
        {
            hasUsedIngrain50 = true;
            StartCoroutine(PerformIngrain());
        }
        else if (healthPercent <= 0.15f && !hasUsedIngrain15)
        {
            hasUsedIngrain15 = true;
            StartCoroutine(PerformIngrain());
        }
    }

    public bool CanTakeDamage()
    {
        if (isIngraining && ingrainVines.Count > 0) return false;

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

    private int CountVinesNearby()
    {
        activeVines.RemoveAll(v => v == null);

        int count = 0;
        foreach (GameObject vine in activeVines)
        {
            if (Vector2.Distance(transform.position, vine.transform.position) <= vineProximity)
            {
                count++;
            }
        }

        return count;
    }

    private void MakeSmartDecision()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        int nearbyVines = CountVinesNearby();
        int totalVines = activeVines.Count;

        if (distanceToPlayer >= pokeDistance && Time.time - lastPokeTime >= pokeCooldown)
        {
            StartCoroutine(PerformPoke());
            return;
        }

        if (Time.time - lastBulletSeedTime >= bulletSeedCooldown && Time.time - lastVineSlamTime >= vineSlamCooldown)
        {
            if (currentPhase == 3 && totalVines < lowVineThreshold)
            {
                StartCoroutine(PerformSeedBarrage());
            }
            else if (nearbyVines == 0 && totalVines < lowVineThreshold)
            {
                StartCoroutine(PerformSeedBarrage());
            }
            else if (distanceToPlayer <= vineSlamLength)
            {
                StartCoroutine(PerformVineSlam());
            }
            else
            {
                StartCoroutine(PerformBulletSeed());
            }
        }
        else if (Time.time - lastBulletSeedTime >= bulletSeedCooldown)
        {
            if (currentPhase == 3 && totalVines < lowVineThreshold && Random.value > 0.5f)
            {
                StartCoroutine(PerformSeedBarrage());
            }
            else
            {
                StartCoroutine(PerformBulletSeed());
            }
        }
        else if (Time.time - lastVineSlamTime >= vineSlamCooldown && distanceToPlayer <= vineSlamLength)
        {
            StartCoroutine(PerformVineSlam());
        }
    }

    private IEnumerator VineSpawnRoutine()
    {
        while (true)
        {
            float interval = currentPhase == 1 ? phase1VineInterval : phase2VineInterval;
            yield return new WaitForSeconds(interval);

            if (activeVines.Count < maxVines && player != null && !isIngraining)
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

        RaycastHit2D groundCheck = Physics2D.Raycast(spawnPos, Vector2.down, 20f, groundLayer);

        if (groundCheck.collider != null)
        {
            GameObject vine = Instantiate(vinePrefab, groundCheck.point, Quaternion.identity);

            Collider2D vineCollider = vine.GetComponent<Collider2D>();
            float offset = Mathf.Abs(vineCollider.bounds.min.y - vine.transform.position.y);

            Vector3 finalPos = vine.transform.position;
            finalPos.y = groundCheck.point.y + offset;
            vine.transform.position = finalPos;

            activeVines.Add(vine);

            Vine vineScript = vine.GetComponent<Vine>();
            if (vineScript != null)
            {
                vineScript.SetBoss(this);
            }
        }
    }

    private IEnumerator RegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenTickRate);

            if (!health.IsDead)
            {
                if (isIngraining)
                {
                    health.Heal(ingrainRegenRate);
                }
                else if (IsNearAnyVine())
                {
                    health.Heal(regenAmount);
                }
            }
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

    private IEnumerator PerformSeedBarrage()
    {
        isAttacking = true;
        lastBulletSeedTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);

        if (player != null)
        {
            Vector2 baseDirection = (player.transform.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

            for (int i = 0; i < barrageCount; i++)
            {
                float spreadOffset = ((i - (barrageCount - 1) / 2f) / (barrageCount - 1)) * barrageSpreadAngle;
                float angle = baseAngle + spreadOffset;

                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                Vector2 targetPos = (Vector2)transform.position + direction * 10f;
                StartCoroutine(ShootBulletSeed(targetPos));

                yield return new WaitForSeconds(0.1f);
            }
        }

        isAttacking = false;
    }

    private IEnumerator PerformPoke()
    {
        isAttacking = true;
        lastPokeTime = Time.time;

        if (player == null)
        {
            isAttacking = false;
            yield break;
        }

        GameObject poke = null;

        if (seedVisualPrefab != null)
        {
            poke = Instantiate(seedVisualPrefab, transform.position, Quaternion.identity);
        }

        Vector2 direction = (player.transform.position - transform.position).normalized;
        Vector2 startPos = transform.position;

        float travelTime = 0f;
        float maxDistance = 15f;

        while (travelTime < 2f)
        {
            travelTime += Time.deltaTime;

            Vector2 newPos = startPos + direction * pokeSpeed * travelTime;

            if (poke != null)
            {
                poke.transform.position = newPos;
            }

            if (Vector2.Distance(startPos, newPos) > maxDistance)
            {
                break;
            }

            Collider2D hit = Physics2D.OverlapCircle(newPos, 0.3f);
            if (hit != null)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeHazardDamage(pokeDamage);
                    }

                    if (poke != null) Destroy(poke);
                    isAttacking = false;
                    yield break;
                }
                else if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    if (poke != null) Destroy(poke);
                    isAttacking = false;
                    yield break;
                }
            }

            yield return null;
        }

        if (poke != null) Destroy(poke);
        isAttacking = false;
    }

    private IEnumerator ShootBulletSeed(Vector2 targetPos)
    {
        GameObject seed = null;

        if (seedVisualPrefab != null)
        {
            seed = Instantiate(seedVisualPrefab, transform.position, Quaternion.identity);
            seed.tag = "BulletSeed";
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
            RaycastHit2D groundCheck = Physics2D.Raycast(position, Vector2.down, 20f, groundLayer);

            if (groundCheck.collider != null)
            {
                GameObject vine = Instantiate(vinePrefab, groundCheck.point, Quaternion.identity);

                Collider2D vineCollider = vine.GetComponent<Collider2D>();
                float offset = Mathf.Abs(vineCollider.bounds.min.y - vine.transform.position.y);

                Vector3 finalPos = vine.transform.position;
                finalPos.y = groundCheck.point.y + offset;
                vine.transform.position = finalPos;

                activeVines.Add(vine);

                Vine vineScript = vine.GetComponent<Vine>();
                if (vineScript != null)
                {
                    vineScript.SetBoss(this);
                }
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
        Vector3 originalScale = transform.localScale;
        Vector3 crouchScale = new Vector3(originalScale.x, originalScale.y * 0.8f, originalScale.z);

        Color originalColor = sprite.color;

        float elapsed = 0f;
        while (elapsed < ingrainTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ingrainTelegraphDuration;

            transform.localScale = Vector3.Lerp(originalScale, crouchScale, t);
            sprite.color = Color.Lerp(originalColor, Color.green, Mathf.PingPong(t * 4f, 1f));

            yield return null;
        }

        transform.localScale = originalScale;
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
                playerHealth.TakeHazardDamage(vineSlamDamage);
            }
        }
    }

    private IEnumerator PerformIngrain()
    {
        isIngraining = true;
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(IngrainTelegraph());

        SpawnIngrainVines();

        Vector3 originalScale = transform.localScale;
        float ingrainStartTime = Time.time;

        while (Time.time - ingrainStartTime < ingrainDuration)
        {
            ingrainVines.RemoveAll(v => v == null);

            if (ingrainVines.Count == 0)
            {
                break;
            }

            float t = Mathf.PingPong(Time.time * 0.5f, 1f);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.05f, t);

            yield return null;
        }

        transform.localScale = originalScale;

        CleanupIngrainVines();

        isIngraining = false;
        isAttacking = false;
    }

    private IEnumerator IngrainTelegraph()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 crouchScale = new Vector3(originalScale.x, originalScale.y * 0.7f, originalScale.z);

        float elapsed = 0f;
        while (elapsed < ingrainTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ingrainTelegraphDuration;

            transform.localScale = Vector3.Lerp(originalScale, crouchScale, t);

            float shake = Mathf.Sin(elapsed * 20f) * 0.2f;
            Vector3 shakePos = transform.position;
            shakePos.x += shake;
            transform.position = shakePos;

            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void SpawnIngrainVines()
    {
        if (ingrainVinePrefab == null || player == null) return;

        Vector2 bossPos = transform.position;
        Vector2 playerPos = player.transform.position;

        for (int i = 0; i < ingrainVineCount; i++)
        {
            float t = (i + 1) / (float)(ingrainVineCount + 1);

            Vector2 spawnPos = Vector2.Lerp(bossPos, playerPos, t);

            RaycastHit2D groundCheck = Physics2D.Raycast(spawnPos, Vector2.down, 20f, groundLayer);

            if (groundCheck.collider != null)
            {
                GameObject vine = Instantiate(ingrainVinePrefab, groundCheck.point, Quaternion.identity);

                Collider2D vineCollider = vine.GetComponent<Collider2D>();
                float offset = Mathf.Abs(vineCollider.bounds.min.y - vine.transform.position.y);

                Vector3 finalPos = vine.transform.position;
                finalPos.y = groundCheck.point.y + offset;
                vine.transform.position = finalPos;

                ingrainVines.Add(vine);

                IngrainVine vineScript = vine.GetComponent<IngrainVine>();
                if (vineScript != null)
                {
                    vineScript.SetBoss(this);
                }
            }
        }
    }

    private void CleanupIngrainVines()
    {
        foreach (GameObject vine in ingrainVines)
        {
            if (vine != null)
            {
                Destroy(vine);
            }
        }

        ingrainVines.Clear();
    }

    public void RemoveVine(GameObject vine)
    {
        activeVines.Remove(vine);
    }

    public void RemoveIngrainVine(GameObject vine)
    {
        ingrainVines.Remove(vine);
    }

    public void CleanupVines()
    {
        Vine[] allVines = FindObjectsByType<Vine>(FindObjectsSortMode.None);
        foreach (Vine vine in allVines)
        {
            Destroy(vine.gameObject);
        }

        IngrainVine[] allIngrainVines = FindObjectsByType<IngrainVine>(FindObjectsSortMode.None);
        foreach (IngrainVine vine in allIngrainVines)
        {
            Destroy(vine.gameObject);
        }

        GameObject[] seeds = GameObject.FindGameObjectsWithTag("BulletSeed");
        foreach (GameObject seed in seeds)
        {
            Destroy(seed);
        }

        activeVines.Clear();
        ingrainVines.Clear();
    }
}