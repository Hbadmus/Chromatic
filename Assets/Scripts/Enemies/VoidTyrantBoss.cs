using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidTyrantBoss : BaseBoss
{
    [Header("Boss Stats")]
    [SerializeField] private float bossMaxHealth = 200f;

    [Header("Vine System")]
    [SerializeField] private GameObject vinePrefab;
    [SerializeField] private int maxVines = 12;
    [SerializeField] private float vineWhipDamage = 1.5f;

    [Header("Red Attacks")]
    [SerializeField] private float groundPoundJumpForce = 12f;
    [SerializeField] private float groundPoundKnockupForce = 18f;
    [SerializeField] private GameObject lavaPrefab;
    [SerializeField] private int lavaCount = 4;
    [SerializeField] private float lavaSpacing = 2f;
    [SerializeField] private float chargeSpeed = 25f;
    [SerializeField] private float chargeDuration = 1.5f;

    [Header("Green Attacks")]
    [SerializeField] private float bulletSeedSpeed = 9f;
    [SerializeField] private float bulletSeedArcHeight = 3f;
    [SerializeField] private float bulletSeedDamage = 2f;
    [SerializeField] private GameObject seedVisualPrefab;
    [SerializeField] private float vineSlamLength = 12f;
    [SerializeField] private float vineSlamDamage = 2.5f;

    [Header("Blue Blizzard Attack")]
    [SerializeField] private float blizzardSpeed = 5f;
    [SerializeField] private float blizzardDamage = 1.5f;
    [SerializeField] private float blizzardSlowDuration = 3f;
    [SerializeField] private float blizzardCooldown = 6f;
    [SerializeField] private GameObject blizzardPrefab;

    [Header("Movement AI")]
    [SerializeField] private float preferredDistance = 7f;
    [SerializeField] private float retreatDistance = 15f;
    [SerializeField] private float pursuitSpeedMultiplier = 1.5f;

    [Header("Phase Timings")]
    [SerializeField] private float phase1AttackCooldown = 5f;
    [SerializeField] private float phase2AttackCooldown = 3.5f;
    [SerializeField] private float phase3AttackCooldown = 2f;
    [SerializeField] private float telegraphDuration = 0.5f;

    private enum BossState { Tracking, Pursuing, Retreating, Cover }

    private List<GameObject> activeVines = new List<GameObject>();
    private GameObject player;
    private PlayerMovement playerMovement;
    private bool isActive = false;
    private bool isAttacking = false;
    private BossState currentState = BossState.Tracking;

    private float lastAttackTime = -999f;
    private float lastBlizzardTime = -999f;
    private float lastDamageTaken = 0f;
    private float recentDamageAmount = 0f;
    private float timeAtRange = 0f;
    private float currentAttackCooldown;

    private int currentPhase = 1;
    private int vinesDestroyedRecently = 0;
    private bool playerIsSlowed = false;
    private bool hasUsedCover66 = false;
    private bool hasUsedCover33 = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private readonly List<GameObject> spawnedLava = new List<GameObject>();
    private readonly List<GameObject> spawnedTemporaryObjects = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
        moveSpeed = 2.5f;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
    }

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        currentAttackCooldown = phase1AttackCooldown;
    }

    public void ActivateBoss()
    {
        if (isActive) return;

        isActive = true;
        StartCoroutine(PassiveVineSpawning());
        StartCoroutine(TrackRecentDamage());
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
        currentState = BossState.Tracking;

        lastAttackTime = -999f;
        lastBlizzardTime = -999f;
        lastDamageTaken = 0f;
        recentDamageAmount = 0f;
        timeAtRange = 0f;
        currentAttackCooldown = phase1AttackCooldown;
        currentPhase = 1;
        vinesDestroyedRecently = 0;
        playerIsSlowed = false;
        hasUsedCover66 = false;
        hasUsedCover33 = false;
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

        for (int i = spawnedLava.Count - 1; i >= 0; i--)
        {
            if (spawnedLava[i] != null)
            {
                Destroy(spawnedLava[i]);
            }
        }
        spawnedLava.Clear();

        for (int i = spawnedTemporaryObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedTemporaryObjects[i] != null)
            {
                Destroy(spawnedTemporaryObjects[i]);
            }
        }
        spawnedTemporaryObjects.Clear();
    }

    private void ResetHealthState()
    {
        if (health != null)
        {
            health.ResetBossState();
        }
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
        if (!isActive)
        {
            Debug.Log("Boss not active!");
            return;
        }

        Debug.Log($"IsAttacking: {isAttacking}, State: {currentState}");

        if (!isAttacking && currentState != BossState.Cover)
        {
            UpdatePhase();
            CheckCoverTrigger();
            UpdateMovementState();
            ExecuteMovement();
            DecideNextAttack();
        }
    }

    protected override void Move()
    {

    }

    private void ExecuteMovement()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        float targetSpeed = moveSpeed;
        bool shouldMoveTowardPlayer = false;

        Debug.Log($"Execute Movement - State: {currentState}, Distance: {distanceToPlayer}");

        switch (currentState)
        {
            case BossState.Tracking:
                if (distanceToPlayer > preferredDistance)
                {
                    shouldMoveTowardPlayer = true;
                }
                else if (distanceToPlayer < preferredDistance - 2f)
                {
                    shouldMoveTowardPlayer = false;
                }
                else
                {
                    return;
                }
                break;

            case BossState.Pursuing:
                shouldMoveTowardPlayer = true;
                targetSpeed = moveSpeed * pursuitSpeedMultiplier;
                break;

            case BossState.Retreating:
                shouldMoveTowardPlayer = false;
                targetSpeed = moveSpeed * 1.2f;
                break;
        }

        float directionToPlayer = player.transform.position.x > transform.position.x ? 1f : -1f;
        float moveDirection = shouldMoveTowardPlayer ? directionToPlayer : -directionToPlayer;

        movingRight = moveDirection > 0;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (movingRight ? 1 : -1);
        transform.localScale = scale;

        rb.linearVelocity = new Vector2(moveDirection * targetSpeed, rb.linearVelocity.y);

        CheckForTurn();
    }

    private void UpdateMovementState()
    {
        if (playerIsSlowed || vinesDestroyedRecently >= 3)
        {
            currentState = BossState.Pursuing;
            return;
        }

        float healthPercent = health.CurrentHealth / health.MaxHealth;
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        int nearbyVines = CountVinesNearby();

        if (healthPercent < 0.25f && nearbyVines == 0)
        {
            currentState = BossState.Retreating;
            return;
        }

        if (recentDamageAmount > 30f)
        {
            currentState = BossState.Retreating;
            return;
        }

        if (distanceToPlayer < 5f && Time.time - lastBlizzardTime < blizzardCooldown * 0.5f)
        {
            currentState = BossState.Retreating;
            return;
        }

        currentState = BossState.Tracking;
    }

    private int CountVinesNearby()
    {
        activeVines.RemoveAll(v => v == null);
        int count = 0;

        foreach (GameObject vine in activeVines)
        {
            if (Vector2.Distance(transform.position, vine.transform.position) <= 5f)
            {
                count++;
            }
        }

        return count;
    }

    private void UpdatePhase()
    {
        float healthPercent = health.CurrentHealth / health.MaxHealth;

        if (healthPercent > 0.66f)
        {
            currentPhase = 1;
            currentAttackCooldown = phase1AttackCooldown;
        }
        else if (healthPercent > 0.33f)
        {
            currentPhase = 2;
            currentAttackCooldown = phase2AttackCooldown;
        }
        else
        {
            currentPhase = 3;
            currentAttackCooldown = phase3AttackCooldown;
        }
    }

    private void CheckCoverTrigger()
    {
        float healthPercent = health.CurrentHealth / health.MaxHealth;

        if (healthPercent <= 0.66f && healthPercent > 0.65f && !hasUsedCover66)
        {
            hasUsedCover66 = true;
            StartCoroutine(PerformCover(4));
        }
        else if (healthPercent <= 0.33f && healthPercent > 0.32f && !hasUsedCover33)
        {
            hasUsedCover33 = true;
            StartCoroutine(PerformCover(6));
        }
    }

    private IEnumerator PerformCover(int vineCount)
    {
        currentState = BossState.Cover;
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(BlueTelegraph());

        for (int i = 0; i < vineCount; i++)
        {
            float angle = (360f / vineCount) * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector2 offset = new Vector2(
                Mathf.Cos(radians) * 4f,
                Mathf.Sin(radians) * 4f
            );

            Vector2 vinePos = (Vector2)transform.position + offset;
            SpawnVineAtPosition(vinePos);
        }

        yield return new WaitForSeconds(1f);

        if (vineCount == 4)
        {
            StartCoroutine(PerformBlizzard());
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                Vector2 targetPos = (Vector2)transform.position + randomDir * 10f;
                StartCoroutine(ShootBulletSeed(targetPos));
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(2f);

        currentState = BossState.Tracking;
        isAttacking = false;
    }

    private void DecideNextAttack()
    {
        if (Time.time - lastAttackTime < currentAttackCooldown) return;
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (distanceToPlayer > 12f)
        {
            timeAtRange += Time.fixedDeltaTime;
        }
        else
        {
            timeAtRange = 0f;
        }

        if (timeAtRange > 5f)
        {
            StartCoroutine(PerformBlizzard());
            timeAtRange = 0f;
            return;
        }

        if (currentPhase == 1)
        {
            DecidePhase1Attack(distanceToPlayer);
        }
        else if (currentPhase == 2)
        {
            DecidePhase2Attack(distanceToPlayer);
        }
        else
        {
            DecidePhase3Attack(distanceToPlayer);
        }
    }

    private void DecidePhase1Attack(float distance)
    {
        if (distance > 8f && Time.time - lastBlizzardTime > blizzardCooldown)
        {
            StartCoroutine(SetupBlizzard());
        }
        else if (distance <= 5f)
        {
            StartCoroutine(PerformGroundPound());
        }
        else if (distance > 6f)
        {
            StartCoroutine(PerformBulletSeed());
        }
        else
        {
            StartCoroutine(PerformVineSlam());
        }
    }

    private void DecidePhase2Attack(float distance)
    {
        if (playerIsSlowed)
        {
            StartCoroutine(SetupCharge());
            return;
        }

        if (distance > 8f && Time.time - lastBlizzardTime > blizzardCooldown)
        {
            StartCoroutine(SetupBlizzard());
        }
        else if (distance <= 6f)
        {
            int rand = Random.Range(0, 2);
            if (rand == 0) StartCoroutine(PerformGroundPound());
            else StartCoroutine(PerformVineSlam());
        }
        else
        {
            int rand = Random.Range(0, 3);
            if (rand == 0) StartCoroutine(PerformBulletSeed());
            else if (rand == 1) StartCoroutine(PerformVineSlam());
            else StartCoroutine(SetupCharge());
        }
    }

    private void DecidePhase3Attack(float distance)
    {
        if (playerIsSlowed)
        {
            StartCoroutine(SetupCharge());
            return;
        }

        if (distance > 8f && Time.time - lastBlizzardTime > blizzardCooldown)
        {
            StartCoroutine(SetupBlizzard());
        }
        else if (distance <= 5f)
        {
            int rand = Random.Range(0, 2);
            if (rand == 0) StartCoroutine(PerformGroundPound());
            else StartCoroutine(PerformVineSlam());
        }
        else
        {
            int rand = Random.Range(0, 4);
            if (rand == 0) StartCoroutine(PerformBulletSeed());
            else if (rand == 1) StartCoroutine(SetupCharge());
            else if (rand == 2) StartCoroutine(PerformVineSlam());
            else StartCoroutine(SetupBlizzard());
        }
    }

    private IEnumerator SetupBlizzard()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance < 8f)
        {
            isAttacking = true;
            currentState = BossState.Retreating;

            float retreatTime = 0f;
            while (retreatTime < 1.5f && Vector2.Distance(transform.position, player.transform.position) < retreatDistance)
            {
                retreatTime += Time.deltaTime;
                yield return null;
            }
        }

        currentState = BossState.Tracking;
        yield return StartCoroutine(PerformBlizzard());
    }

    private IEnumerator SetupCharge()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance > 10f)
        {
            isAttacking = true;
            currentState = BossState.Pursuing;

            float pursuitTime = 0f;
            while (pursuitTime < 1.5f && Vector2.Distance(transform.position, player.transform.position) > 8f)
            {
                pursuitTime += Time.deltaTime;
                yield return null;
            }
        }

        currentState = BossState.Tracking;
        yield return StartCoroutine(PerformCharge());
    }

    private IEnumerator PassiveVineSpawning()
    {
        while (true)
        {
            float interval = currentPhase == 1 ? 10f : (currentPhase == 2 ? 6f : 3f);

            int spawnCount = currentPhase == 3 ? 2 : 1;

            for (int i = 0; i < spawnCount; i++)
            {
                if (activeVines.Count < maxVines && player != null)
                {
                    SpawnVineNearPlayer();
                }
            }

            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnVineNearPlayer()
    {
        if (vinePrefab == null || player == null) return;

        Vector2 playerPos = player.transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * 5f;
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
                vineScript.SetBossForFinalBoss(this);
            }
        }
    }

    private IEnumerator TrackRecentDamage()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            recentDamageAmount = 0f;
        }
    }

    public void OnDamageTaken(float damage)
    {
        recentDamageAmount += damage;
    }

    private IEnumerator PerformGroundPound()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(RedTelegraph());

        rb.AddForce(Vector2.up * groundPoundJumpForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(1f);

        ApplyGroundPoundKnockup();
        SpawnLava();

        if (currentPhase == 3)
        {
            SpawnVinesAtImpact();
        }

        isAttacking = false;
    }

    private void ApplyGroundPoundKnockup()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance <= 6f)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 pushDirection = (player.transform.position - transform.position).normalized;
                pushDirection.y = 1f;
                pushDirection.Normalize();

                playerRb.AddForce(pushDirection * groundPoundKnockupForce, ForceMode2D.Impulse);

                if (playerMovement != null)
                {
                    playerMovement.ApplyKnockback(0.5f);
                }
            }
        }
    }

    private void SpawnLava()
    {
        if (lavaPrefab == null) return;

        Collider2D bossCollider = GetComponent<Collider2D>();
        float bossBottom = bossCollider.bounds.min.y;

        for (int i = 0; i < lavaCount; i++)
        {
            float offset = (i - (lavaCount - 1) / 2f) * lavaSpacing;
            Vector2 lavaPos = new Vector2(transform.position.x + offset, bossBottom);

            GameObject lava = Instantiate(lavaPrefab, lavaPos, Quaternion.identity);
            spawnedLava.Add(lava);
        }
    }

    private void SpawnVinesAtImpact()
    {
        for (int i = 0; i < 2; i++)
        {
            float xOffset = (i == 0) ? -3f : 3f;
            Vector2 vinePos = new Vector2(transform.position.x + xOffset, transform.position.y);
            SpawnVineAtPosition(vinePos);
        }
    }

    private IEnumerator PerformCharge()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(RedTelegraph());

        float chargeEndTime = Time.time + chargeDuration;

        while (Time.time < chargeEndTime)
        {
            CheckForTurn();

            float direction = movingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * chargeSpeed, rb.linearVelocity.y);

            yield return new WaitForFixedUpdate();
        }

        playerIsSlowed = false;
        isAttacking = false;
    }

    private IEnumerator PerformBulletSeed()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(GreenTelegraph());

        if (player != null)
        {
            Vector2 targetPos = player.transform.position;
            StartCoroutine(ShootBulletSeed(targetPos));
        }

        isAttacking = false;
    }

    private IEnumerator PerformBlizzard()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        lastBlizzardTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(BlueTelegraph());

        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            StartCoroutine(ShootBlizzardProjectile(direction));
        }

        isAttacking = false;
    }

    private IEnumerator ShootBlizzardProjectile(Vector2 direction)
    {
        if (blizzardPrefab == null) yield break;

        GameObject blizzard = Instantiate(blizzardPrefab, transform.position, Quaternion.identity);
        spawnedTemporaryObjects.Add(blizzard);
        blizzard.tag = "NotInteractable";

        Vector2 startPos = transform.position;
        float travelTime = 0f;
        bool hasHit = false;

        BoxCollider2D blizzardCollider = blizzard.GetComponent<BoxCollider2D>();

        while (travelTime < 20f && !hasHit)
        {
            travelTime += Time.deltaTime;

            Vector2 newPos = startPos + direction * blizzardSpeed * travelTime;
            blizzard.transform.position = newPos;

            Collider2D[] hits = Physics2D.OverlapBoxAll(blizzard.transform.position, blizzardCollider.size, 0);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player") && !hasHit)
                {
                    PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeHazardDamage(blizzardDamage);
                    }

                    if (playerMovement != null)
                    {
                        playerMovement.ApplySlow(0.5f, blizzardSlowDuration);
                        playerIsSlowed = true;
                        StartCoroutine(ResetSlowFlag());
                    }

                    hasHit = true;
                    Destroy(blizzard);
                    yield break;
                }
            }

            if (hits.Length > 0)
            {
                foreach (Collider2D hit in hits)
                {
                    if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
                    {
                        Destroy(blizzard);
                        yield break;
                    }
                }
            }

            yield return null;
        }

        Destroy(blizzard);
    }

    private IEnumerator ResetSlowFlag()
    {
        yield return new WaitForSeconds(blizzardSlowDuration);
        playerIsSlowed = false;
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
                    vineScript.SetBossForFinalBoss(this);
                }
            }
        }
    }

    private IEnumerator PerformVineSlam()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(GreenTelegraph());

        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            StartCoroutine(ExecuteVineSlam(direction));
        }

        isAttacking = false;
    }

    private IEnumerator ExecuteVineSlam(Vector2 direction)
    {
        GameObject slamVine = new GameObject("VineSlam");
        spawnedTemporaryObjects.Add(slamVine);
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
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.5f;

            slamVine.transform.localScale = new Vector3(baseScale.x, vineSlamLength * t, baseScale.z);

            yield return null;
        }

        slamSprite.color = Color.green;
        slamCollider.enabled = true;

        CheckSlamHit(slamVine);

        yield return new WaitForSeconds(0.5f);

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

    private IEnumerator RedTelegraph()
    {
        Color originalColor = sprite.color;

        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / telegraphDuration;

            sprite.color = Color.Lerp(originalColor, Color.red, Mathf.PingPong(t * 4f, 1f));

            yield return null;
        }

        sprite.color = originalColor;
    }

    private IEnumerator GreenTelegraph()
    {
        Color originalColor = sprite.color;

        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / telegraphDuration;

            sprite.color = Color.Lerp(originalColor, Color.green, Mathf.PingPong(t * 4f, 1f));

            yield return null;
        }

        sprite.color = originalColor;
    }

    private IEnumerator BlueTelegraph()
    {
        Color originalColor = sprite.color;

        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / telegraphDuration;

            sprite.color = Color.Lerp(originalColor, Color.blue, Mathf.PingPong(t * 4f, 1f));

            yield return null;
        }

        sprite.color = originalColor;
    }

    public void RemoveVine(GameObject vine)
    {
        activeVines.Remove(vine);
        vinesDestroyedRecently++;
        StartCoroutine(ResetVineDestroyCount());
    }

    private IEnumerator ResetVineDestroyCount()
    {
        yield return new WaitForSeconds(5f);
        vinesDestroyedRecently = 0;
    }

    public void CleanupVines()
    {
        Vine[] allVines = FindObjectsByType<Vine>(FindObjectsSortMode.None);
        foreach (Vine vine in allVines)
        {
            Destroy(vine.gameObject);
        }

        GameObject[] seeds = GameObject.FindGameObjectsWithTag("BulletSeed");
        foreach (GameObject seed in seeds)
        {
            Destroy(seed);
        }

        activeVines.Clear();
    }
}