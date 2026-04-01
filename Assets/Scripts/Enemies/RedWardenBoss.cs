using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedWardenBoss : BaseBoss
{
    [Header("Ground Pound")]
    [SerializeField] private float groundPoundJumpForce = 10f;
    [SerializeField] private float telegraphDuration = 0.5f;
    [SerializeField] private GameObject lavaPrefab;
    [SerializeField] private int lavaCount = 3;
    [SerializeField] private float lavaSpacing = 2f;

    [Header("Charge Attack")]
    [SerializeField] private float chargeSpeed = 20f;
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float chargeTelegraphDuration = 0.7f;

    [Header("AI Decision Making")]
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float attackRange = 6f;

    [Header("Stun")]
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float wobbleAmount = 0.3f;

    private float currentSpeed;
    private bool isAttacking = false;
    private bool canCharge = false;
    private bool isActive = false;
    private bool isStunned = false;
    private float lastAttackTime;
    private GameObject player;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private readonly List<GameObject> spawnedLava = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
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
    }

    protected override void FixedUpdate()
    {
        if (!isActive || isStunned) return;

        if (!isAttacking)
        {
            AdjustSpeedBasedOnHealth();
            MoveTowardPlayer();
            DecideNextAttack();
        }
    }

    private void MoveTowardPlayer()
    {
        if (isAttacking || player == null) return;

        bool playerOnRight = player.transform.position.x > transform.position.x;

        if (playerOnRight && !movingRight)
        {
            Turn();
        }
        else if (!playerOnRight && movingRight)
        {
            Turn();
        }

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);

        CheckForTurn();
    }

    private void AdjustSpeedBasedOnHealth()
    {
        float healthPercent = health.CurrentHealth / health.MaxHealth;

        canCharge = healthPercent < 0.66f;

        if (healthPercent < 0.33f)
        {
            currentSpeed = moveSpeed * 2f;
        }
        else if (healthPercent < 0.66f)
        {
            currentSpeed = moveSpeed * 1.5f;
        }
        else
        {
            currentSpeed = moveSpeed;
        }
    }

    private void DecideNextAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= attackRange)
        {
            if (canCharge && Random.value > 0.6f)
            {
                StartCoroutine(PerformCharge());
            }
            else
            {
                StartCoroutine(PerformGroundPound());
            }
        }
    }

    private IEnumerator PerformGroundPound()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(Telegraph());

        rb.AddForce(Vector2.up * groundPoundJumpForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(1f);

        SpawnLava();

        isAttacking = false;
    }

    private IEnumerator PerformCharge()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(ChargeTelegraph());

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

    private IEnumerator Telegraph()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 crouchScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 0.7f, originalScale.z);

        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / telegraphDuration;
            transform.localScale = Vector3.Lerp(originalScale, crouchScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator ChargeTelegraph()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 leanBackScale = new Vector3(originalScale.x * 0.8f, originalScale.y * 1.2f, originalScale.z);

        Color originalColor = sprite.color;

        float elapsed = 0f;
        while (elapsed < chargeTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeTelegraphDuration;

            transform.localScale = Vector3.Lerp(originalScale, leanBackScale, t);
            sprite.color = Color.Lerp(originalColor, Color.red, Mathf.PingPong(t * 4f, 1f));

            yield return null;
        }

        transform.localScale = originalScale;
        sprite.color = originalColor;
    }

    private void SpawnLava()
    {
        if (lavaPrefab == null) return;

        Collider2D bossCollider = GetComponent<Collider2D>();
        float groundY = bossCollider.bounds.min.y;

        for (int i = 0; i < lavaCount; i++)
        {
            float offset = (i - (lavaCount - 1) / 2f) * lavaSpacing;
            Vector2 lavaPos = new Vector2(transform.position.x + offset, groundY);

            GameObject lava = Instantiate(lavaPrefab, lavaPos, Quaternion.identity);
            spawnedLava.Add(lava);
        }
    }

    public void StunBoss()
    {
        if (isStunned) return;

        StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        isAttacking = true;

        while (Mathf.Abs(rb.linearVelocity.y) > 0.5f)
        {
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        health.MakeVulnerable(stunDuration);

        float elapsed = 0f;
        Quaternion originalRotation = transform.rotation;

        while (elapsed < stunDuration)
        {
            elapsed += Time.deltaTime;
            float wobble = Mathf.Sin(elapsed * 20f) * wobbleAmount;
            transform.rotation = Quaternion.Euler(0, 0, wobble);

            yield return null;
        }

        transform.rotation = originalRotation;
        rb.gravityScale = originalGravity;
        isStunned = false;
        isAttacking = false;
    }

    public void ResetBoss()
    {
        StopAllCoroutines();

        ResetCombatState();
        RestoreTransform();
        ResetHealthState();
        ClearSpawnedLava();
        StopMovement();
    }

    private void ResetCombatState()
    {
        isActive = false;
        isAttacking = false;
        isStunned = false;
        canCharge = false;
        currentSpeed = moveSpeed;
        lastAttackTime = -999f;
    }

    private void RestoreTransform()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;
        movingRight = initialScale.x >= 0f;
    }

    private void ResetHealthState()
    {
        if (health != null)
        {
            health.ResetBossState();
        }
    }

    private void ClearSpawnedLava()
    {
        for (int i = spawnedLava.Count - 1; i >= 0; i--)
        {
            if (spawnedLava[i] != null)
            {
                Destroy(spawnedLava[i]);
            }
        }

        spawnedLava.Clear();
    }

    private void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}