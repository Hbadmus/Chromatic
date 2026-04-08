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

    [Header("Trap Vines")]
    [SerializeField] private float trapVineOffset = 3f;
    [SerializeField] private float trapVineInterval = 10f;

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
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private readonly List<GameObject> spawnedTemporaryObjects = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
        moveSpeed = 1.5f;
        currentSpeed = moveSpeed;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
    }

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void ActivateBoss()
    {
        if (isActive) return;

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
        StartCoroutine(TrapVineRoutine());
        StartCoroutine(RegenerationRoutine());
    }

    public void ResetBoss()
    {
        StopAllCoroutines();
        ResetCombatState();
        RestoreTransform();
        CleanupSpawnedObjects();
        ResetHealthState();
        StopMovement();
    }

    private void ResetCombatState()
    {
        isActive = false;
        isAttacking = false;
        isIngraining = false;

        lastBulletSeedTime = -999f;
        lastVineSlamTime = -999f;
        lastPokeTime = -999f;
        bulletSeedCooldown = 8f;
        vineSlamCooldown = 12f;
        currentPhase = 1;
        hasUsedIngrain50 = false;
        hasUsedIngrain15 = false;
        currentSpeed = moveSpeed;
    }

    private void RestoreTransform()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;
        movingRight = initialScale.x >= 0f;
    }

    private void CleanupSpawnedObjects()
    {
        CleanupVines();

        for (int i = spawnedTemporaryObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedTemporaryObjects[i] != null)
                Destroy(spawnedTemporaryObjects[i]);
        }

        spawnedTemporaryObjects.Clear();
    }

    private void ResetHealthState()
    {
        if (health != null)
            health.ResetBossState();
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
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
        currentSpeed = IsNearAnyVine() ? moveSpeed : moveSpeed * isolatedSpeedMultiplier;
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
                return true;
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
                count++;
        }

        return count;
    }

    // Returns true if there is a vine on the far side of the player from the boss
    private bool IsPlayerTrapped()
    {
        if (player == null) return false;

        activeVines.RemoveAll(v => v == null);

        Vector2 awayFromBoss = ((Vector2)(player.transform.position - transform.position)).normalized;
        Vector2 behindPlayer = (Vector2)player.transform.position + awayFromBoss * trapVineOffset;

        foreach (GameObject vine in activeVines)
        {
            if (Vector2.Distance(vine.transform.position, behindPlayer) <= vineProximity)
                return true;
        }

        return false;
    }

    private void MakeSmartDecision()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        int nearbyVines = CountVinesNearby();
        int totalVines = activeVines.Count;

        if (dist >= pokeDistance && Time.time - lastPokeTime >= pokeCooldown)
        {
            StartCoroutine(PerformPoke());
            return;
        }

        // Player is trapped behind a vine — punish with vine slam if in range
        if (IsPlayerTrapped() && dist <= vineSlamLength && Time.time - lastVineSlamTime >= vineSlamCooldown)
        {
            StartCoroutine(PerformVineSlam());
            return;
        }

        if (Time.time - lastBulletSeedTime >= bulletSeedCooldown && Time.time - lastVineSlamTime >= vineSlamCooldown)
        {
            if (currentPhase == 3 && totalVines < lowVineThreshold)
                StartCoroutine(PerformSeedBarrage());
            else if (nearbyVines == 0 && totalVines < lowVineThreshold)
                StartCoroutine(PerformSeedBarrage());
            else if (dist <= vineSlamLength)
                StartCoroutine(PerformVineSlam());
            else
                StartCoroutine(PerformBulletSeed());
        }
        else if (Time.time - lastBulletSeedTime >= bulletSeedCooldown)
        {
            if (currentPhase == 3 && totalVines < lowVineThreshold && Random.value > 0.5f)
                StartCoroutine(PerformSeedBarrage());
            else
                StartCoroutine(PerformBulletSeed());
        }
        else if (Time.time - lastVineSlamTime >= vineSlamCooldown && dist <= vineSlamLength)
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
                SpawnVineNearPlayer();
        }
    }

    // Periodically drops a vine behind the player to trap them
    private IEnumerator TrapVineRoutine()
    {
        yield return new WaitForSeconds(trapVineInterval);

        while (true)
        {
            float interval = currentPhase == 1 ? trapVineInterval : trapVineInterval * 0.6f;
            yield return new WaitForSeconds(interval);

            if (activeVines.Count < maxVines && player != null && !isIngraining && !IsPlayerTrapped())
                SpawnTrapVine();
        }
    }

    private void SpawnTrapVine()
    {
        if (vinePrefab == null || player == null) return;

        Vector2 awayFromBoss = ((Vector2)(player.transform.position - transform.position)).normalized;
        Vector2 spawnPos = (Vector2)player.transform.position + awayFromBoss * trapVineOffset;

        PlaceVineAtGroundPosition(spawnPos, activeVines);
    }

    private void SpawnVineNearPlayer()
    {
        if (vinePrefab == null || player == null) return;

        Vector2 playerPos = player.transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * vineSpawnRadius;

        PlaceVineAtGroundPosition(playerPos + randomOffset, activeVines);
    }

    private void PlaceVineAtGroundPosition(Vector2 pos, List<GameObject> targetList, GameObject prefabOverride = null)
    {
        GameObject prefab = prefabOverride != null ? prefabOverride : vinePrefab;
        if (prefab == null) return;

        RaycastHit2D groundCheck = Physics2D.Raycast(pos, Vector2.down, 20f, groundLayer);
        if (groundCheck.collider == null) return;

        GameObject vine = Instantiate(prefab, groundCheck.point, Quaternion.identity);

        Collider2D vineCollider = vine.GetComponent<Collider2D>();
        float offset = Mathf.Abs(vineCollider.bounds.min.y - vine.transform.position.y);

        Vector3 finalPos = vine.transform.position;
        finalPos.y = groundCheck.point.y + offset;
        vine.transform.position = finalPos;

        targetList.Add(vine);

        Vine vineScript = vine.GetComponent<Vine>();
        if (vineScript != null)
            vineScript.SetBoss(this);
    }

    private IEnumerator RegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenTickRate);

            if (!health.IsDead)
            {
                if (isIngraining)
                    health.Heal(ingrainRegenRate);
                else if (IsNearAnyVine())
                    health.Heal(regenAmount);
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
            StartCoroutine(ShootBulletSeed(player.transform.position));

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

                StartCoroutine(ShootBulletSeed((Vector2)transform.position + direction * 10f));

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
            poke = Instantiate(seedVisualPrefab, transform.position, Quaternion.identity);

        Vector2 direction = (player.transform.position - transform.position).normalized;
        Vector2 startPos = transform.position;
        float travelTime = 0f;

        while (travelTime < 2f)
        {
            travelTime += Time.deltaTime;
            Vector2 newPos = startPos + direction * pokeSpeed * travelTime;

            if (poke != null) poke.transform.position = newPos;

            if (Vector2.Distance(startPos, newPos) > 35f) break;

            Collider2D hit = Physics2D.OverlapCircle(newPos, 0.3f);
            if (hit != null)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<PlayerHealth>()?.TakeHazardDamage(pokeDamage);
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

            Vector2 newPos = startPos + direction * (totalDistance * t);
            newPos.y += bulletSeedArcHeight * Mathf.Sin(Mathf.PI * t);

            if (seed != null) seed.transform.position = newPos;

            Collider2D hit = Physics2D.OverlapCircle(newPos, 0.3f);
            if (hit != null)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<PlayerHealth>()?.TakeHazardDamage(bulletSeedDamage);
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
            PlaceVineAtGroundPosition(position, activeVines);
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
            yield return StartCoroutine(ExecuteVineSlam(direction));
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
        spawnedTemporaryObjects.Add(slamVine);

        SpriteRenderer slamSprite = slamVine.AddComponent<SpriteRenderer>();
        slamSprite.sprite = GetComponent<SpriteRenderer>().sprite;
        slamSprite.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);

        BoxCollider2D slamCollider = slamVine.AddComponent<BoxCollider2D>();
        slamCollider.isTrigger = true;
        slamCollider.enabled = false;

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float sign = direction.x >= 0 ? 1f : -1f;

        // Vine raises up diagonally before slamming down
        float startAngle = baseAngle + sign * 60f;
        float endAngle = baseAngle - sign * 40f;

        float width = 0.4f;
        float halfExtend = vineSlamExpandDuration * 0.5f;

        // Phase 1: extend at raised angle
        float elapsed = 0f;
        while (elapsed < halfExtend)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfExtend;
            float currentLen = vineSlamLength * t;

            float rad = startAngle * Mathf.Deg2Rad;
            Vector2 vineDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // Bottom tip stays at boss, center offset by half length
            slamVine.transform.position = (Vector2)transform.position + vineDir * (currentLen / 2f);
            slamVine.transform.rotation = Quaternion.Euler(0, 0, startAngle - 90f);
            slamVine.transform.localScale = new Vector3(width, currentLen, 1f);

            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        // Phase 2: slam down — eased so it accelerates into the slam
        elapsed = 0f;
        while (elapsed < halfExtend)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfExtend;
            float easedT = t * t; // ease in — slow start, fast finish
            float currentAngle = Mathf.Lerp(startAngle, endAngle, easedT);

            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 vineDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            slamVine.transform.position = (Vector2)transform.position + vineDir * (vineSlamLength / 2f);
            slamVine.transform.rotation = Quaternion.Euler(0, 0, currentAngle - 90f);
            slamVine.transform.localScale = new Vector3(width, vineSlamLength, 1f);

            yield return null;
        }

        // Phase 3: hold at slammed position and deal damage
        slamSprite.color = Color.green;
        slamCollider.enabled = true;
        CheckSlamHit(slamVine);

        yield return new WaitForSeconds(vineSlamPauseDuration);

        // Phase 4: retract
        elapsed = 0f;
        Vector3 finalScale = slamVine.transform.localScale;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            slamVine.transform.localScale = Vector3.Lerp(finalScale, Vector3.zero, elapsed / 0.3f);
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
            player.GetComponent<PlayerHealth>()?.TakeHazardDamage(vineSlamDamage);
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
        Color originalColor = sprite.color;
        float ingrainStartTime = Time.time;

        while (Time.time - ingrainStartTime < ingrainDuration)
        {
            ingrainVines.RemoveAll(v => v == null);

            if (ingrainVines.Count == 0) break;

            // Obvious heal pulse — bigger scale and green glow so player knows to break vines
            float t = Mathf.PingPong(Time.time * 2.5f, 1f);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.3f, t);
            sprite.color = Color.Lerp(originalColor, Color.green, t);

            yield return null;
        }

        transform.localScale = originalScale;
        sprite.color = originalColor;

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
            if (groundCheck.collider == null) continue;

            GameObject vine = Instantiate(ingrainVinePrefab, groundCheck.point, Quaternion.identity);

            Collider2D vineCollider = vine.GetComponent<Collider2D>();
            float offset = Mathf.Abs(vineCollider.bounds.min.y - vine.transform.position.y);

            Vector3 finalPos = vine.transform.position;
            finalPos.y = groundCheck.point.y + offset;
            vine.transform.position = finalPos;

            ingrainVines.Add(vine);

            IngrainVine vineScript = vine.GetComponent<IngrainVine>();
            if (vineScript != null)
                vineScript.SetBoss(this);
        }
    }

    private void CleanupIngrainVines()
    {
        foreach (GameObject vine in ingrainVines)
        {
            if (vine != null) Destroy(vine);
        }

        ingrainVines.Clear();
    }

    public void RemoveVine(GameObject vine) => activeVines.Remove(vine);

    public void RemoveIngrainVine(GameObject vine) => ingrainVines.Remove(vine);

    public void CleanupVines()
    {
        Vine[] allVines = FindObjectsByType<Vine>(FindObjectsSortMode.None);
        foreach (Vine vine in allVines) Destroy(vine.gameObject);

        IngrainVine[] allIngrainVines = FindObjectsByType<IngrainVine>(FindObjectsSortMode.None);
        foreach (IngrainVine vine in allIngrainVines) Destroy(vine.gameObject);

        GameObject[] seeds = GameObject.FindGameObjectsWithTag("BulletSeed");
        foreach (GameObject seed in seeds) Destroy(seed);

        activeVines.Clear();
        ingrainVines.Clear();
    }
}