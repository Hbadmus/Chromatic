using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidTyrantBoss : BaseBoss
{
    [Header("Boss Stats")]
    [SerializeField] private float bossMaxHealth = 200f;

    [Header("Vine System")]
    [SerializeField] private GameObject vinePrefab;
    [SerializeField] private int maxVines = 6;
    [SerializeField] private float vineWhipDamage = 12f;

    [Header("Red Attacks")]
    [SerializeField] private float groundPoundJumpForce = 12f;
    [SerializeField] private GameObject lavaPrefab;
    [SerializeField] private int lavaCount = 4;
    [SerializeField] private float lavaSpacing = 2f;
    [SerializeField] private float chargeSpeed = 22f;
    [SerializeField] private float chargeDuration = 1.5f;

    [Header("Green Attacks")]
    [SerializeField] private float bulletSeedSpeed = 9f;
    [SerializeField] private float bulletSeedArcHeight = 3f;
    [SerializeField] private float bulletSeedDamage = 15f;
    [SerializeField] private GameObject seedVisualPrefab;
    [SerializeField] private float vineSlamLength = 12f;
    [SerializeField] private float vineSlamDamage = 20f;

    [Header("Blue Attacks")]
    [SerializeField] private float aoeSlowRadius = 5f;
    [SerializeField] private float aoeSlowDuration = 3f;
    [SerializeField] private float aoeSlowDamagePerSec = 0.5f;
    [SerializeField] private float teleportBulletSpeed = 12f;
    [SerializeField] private GameObject teleportBulletPrefab;

    [Header("Phase Timings")]
    [SerializeField] private float phase1AttackCooldown = 7f;
    [SerializeField] private float phase2AttackCooldown = 5f;
    [SerializeField] private float phase3AttackCooldown = 3.5f;
    [SerializeField] private float telegraphDuration = 0.7f;

    private List<GameObject> activeVines = new List<GameObject>();
    private GameObject player;
    private PlayerMovement playerMovement;
    private bool isActive = false;
    private bool isAttacking = false;

    private float lastAttackTime = -999f;
    private float lastTeleportTime = -999f;
    private float currentAttackCooldown;

    private int currentPhase = 1;
    private bool playerIsSlowed = false;
    private bool justTeleportedPlayer = false;

    protected override void Awake()
    {
        base.Awake();
        moveSpeed = 2f;
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

        ActivateBoss();
    }

    public void ActivateBoss()
    {
        isActive = true;
    }

    protected override void FixedUpdate()
    {
        if (!isActive) return;

        Debug.Log($"IsAttacking: {isAttacking}");

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

    private void DecideNextAttack()
    {
        if (Time.time - lastAttackTime < currentAttackCooldown) return;
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

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
        if (distance > 12f && Time.time - lastTeleportTime > 12f)
        {
            StartCoroutine(PerformTeleportBullet());
        }
        else if (distance <= 5f)
        {
            StartCoroutine(PerformGroundPound());
        }
        else if (distance > 10f)
        {
            StartCoroutine(PerformBulletSeed());
        }
        else if (Random.value > 0.5f)
        {
            StartCoroutine(PerformAOESlow());
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
            StartCoroutine(PerformCharge());
            return;
        }

        if (justTeleportedPlayer)
        {
            if (Random.value > 0.5f)
            {
                StartCoroutine(PerformVineSlam());
            }
            else
            {
                StartCoroutine(PerformGroundPound());
            }
            return;
        }

        if (distance > 12f && Time.time - lastTeleportTime > 12f)
        {
            StartCoroutine(PerformTeleportBullet());
        }
        else if (distance <= 6f && activeVines.Count < 3)
        {
            StartCoroutine(PerformGroundPound());
        }
        else if (distance > 8f)
        {
            StartCoroutine(PerformAOESlow());
        }
        else
        {
            int rand = Random.Range(0, 3);
            if (rand == 0) StartCoroutine(PerformBulletSeed());
            else if (rand == 1) StartCoroutine(PerformVineSlam());
            else StartCoroutine(PerformCharge());
        }
    }

    private void DecidePhase3Attack(float distance)
    {
        if (justTeleportedPlayer && !playerIsSlowed)
        {
            StartCoroutine(PerformTripleCombo());
            return;
        }

        if (playerIsSlowed)
        {
            if (Random.value > 0.5f)
            {
                StartCoroutine(PerformCharge());
            }
            else
            {
                StartCoroutine(PerformSeedBarrage());
            }
            return;
        }

        if (distance > 12f && Time.time - lastTeleportTime > 10f)
        {
            StartCoroutine(PerformTeleportBullet());
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
            if (rand == 0) StartCoroutine(PerformAOESlow());
            else if (rand == 1) StartCoroutine(PerformBulletSeed());
            else if (rand == 2) StartCoroutine(PerformCharge());
            else StartCoroutine(PerformVineSlam());
        }
    }

    private IEnumerator PerformTripleCombo()
    {
        yield return StartCoroutine(PerformAOESlow());
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(PerformCharge());
    }

    private IEnumerator PerformGroundPound()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(RedTelegraph());

        rb.AddForce(Vector2.up * groundPoundJumpForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(1f);

        SpawnLava();

        if (currentPhase == 3)
        {
            SpawnVinesAtImpact();
        }

        isAttacking = false;
    }

    private void SpawnLava()
    {
        if (lavaPrefab == null) return;

        Vector2 spawnPos = transform.position;

        for (int i = 0; i < lavaCount; i++)
        {
            float offset = (i - (lavaCount - 1) / 2f) * lavaSpacing;
            Vector2 lavaPos = new Vector2(spawnPos.x + offset, spawnPos.y);

            Instantiate(lavaPrefab, lavaPos, Quaternion.identity);
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

    private IEnumerator PerformSeedBarrage()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(GreenTelegraph());

        if (player != null)
        {
            Vector2 baseDirection = (player.transform.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

            for (int i = 0; i < 3; i++)
            {
                float spreadOffset = ((i - 1f) / 2f) * 30f;
                float angle = baseAngle + spreadOffset;

                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                Vector2 targetPos = (Vector2)transform.position + direction * 10f;
                StartCoroutine(ShootBulletSeed(targetPos));

                yield return new WaitForSeconds(0.15f);
            }
        }

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

    private IEnumerator PerformAOESlow()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(BlueTelegraph());

        StartCoroutine(ExecuteAOESlow());

        isAttacking = false;
    }

    private IEnumerator ExecuteAOESlow()
    {
        GameObject slowZone = new GameObject("SlowZone");
        slowZone.transform.position = transform.position;

        SpriteRenderer zoneSprite = slowZone.AddComponent<SpriteRenderer>();
        zoneSprite.sprite = Resources.Load<Sprite>("Circle");
        if (zoneSprite.sprite == null)
        {
            GameObject tempCircle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            zoneSprite.sprite = tempCircle.GetComponent<SpriteRenderer>().sprite;
            Destroy(tempCircle);
        }
        zoneSprite.color = new Color(0, 0, 1, 0.3f);

        CircleCollider2D zoneCollider = slowZone.AddComponent<CircleCollider2D>();
        zoneCollider.isTrigger = true;
        zoneCollider.radius = aoeSlowRadius;

        slowZone.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.5f;

            slowZone.transform.localScale = Vector3.one * aoeSlowRadius * 2f * t;

            yield return null;
        }

        StartCoroutine(ApplySlowEffect(slowZone));

        yield return new WaitForSeconds(aoeSlowDuration);

        Destroy(slowZone);
    }

    private IEnumerator ApplySlowEffect(GameObject slowZone)
    {
        CircleCollider2D zoneCollider = slowZone.GetComponent<CircleCollider2D>();
        float startTime = Time.time;

        while (Time.time - startTime < aoeSlowDuration && slowZone != null)
        {
            if (player != null)
            {
                float distance = Vector2.Distance(slowZone.transform.position, player.transform.position);

                if (distance <= aoeSlowRadius)
                {
                    if (playerMovement != null)
                    {
                        playerMovement.ApplySlow(0.5f, 0.1f);
                    }

                    playerIsSlowed = true;

                    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeHazardDamage(aoeSlowDamagePerSec * 0.1f);
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        playerIsSlowed = false;
    }

    private IEnumerator PerformTeleportBullet()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        lastTeleportTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(BlueTelegraph());

        if (player != null)
        {
            StartCoroutine(ShootTeleportBullet());
        }

        isAttacking = false;
    }

    private IEnumerator ShootTeleportBullet()
    {
        GameObject bullet = null;

        if (teleportBulletPrefab != null)
        {
            bullet = Instantiate(teleportBulletPrefab, transform.position, Quaternion.identity);
        }
        else if (seedVisualPrefab != null)
        {
            bullet = Instantiate(seedVisualPrefab, transform.position, Quaternion.identity);
            SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.blue;
        }

        Vector2 direction = (player.transform.position - transform.position).normalized;
        Vector2 startPos = transform.position;

        float travelTime = 0f;
        float rotationSpeed = 360f;

        while (travelTime < 3f)
        {
            travelTime += Time.deltaTime;

            Vector2 newPos = startPos + direction * teleportBulletSpeed * travelTime;

            if (bullet != null)
            {
                bullet.transform.position = newPos;
                bullet.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }

            Collider2D hit = Physics2D.OverlapCircle(newPos, 0.3f);
            if (hit != null && hit.CompareTag("Player"))
            {
                TeleportPlayerToBoss();

                if (bullet != null) Destroy(bullet);
                yield break;
            }

            if (hit != null && hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (bullet != null) Destroy(bullet);
                yield break;
            }

            yield return null;
        }

        if (bullet != null) Destroy(bullet);
    }
    private void TeleportPlayerToBoss()
    {
        if (player == null) return;

        Vector2 teleportPos = transform.position;
        teleportPos.x += movingRight ? -2f : 2f;

        player.transform.position = teleportPos;

        if (playerMovement != null)
        {
            playerMovement.ApplyKnockback(0.3f);
        }

        justTeleportedPlayer = true;
        StartCoroutine(ResetTeleportFlag());
    }

    private IEnumerator ResetTeleportFlag()
    {
        yield return new WaitForSeconds(1f);
        justTeleportedPlayer = false;
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