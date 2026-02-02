using System.Collections;
using UnityEngine;

public class RedWardenBoss : BaseBoss
{
    [Header("Ground Pound")]
    [SerializeField] private float groundPoundInterval = 5f;
    [SerializeField] private float groundPoundJumpForce = 10f;
    [SerializeField] private float shockwaveRadius = 7f;
    [SerializeField] private float shockwaveForce = 30f;
    [SerializeField] private float telegraphDuration = 0.5f;

    [Header("Charge Attack")]
    [SerializeField] private float chargeInterval = 7f;
    [SerializeField] private float chargeSpeed = 20f;
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float chargeTelegraphDuration = 0.7f;

    private float currentSpeed;
    private bool isAttacking = false;
    private bool canCharge = false;

    private bool isActive = false;

    [Header("Stun")]
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float wobbleAmount = 0.3f;

    private bool isStunned = false;

    [SerializeField] private float minYPosition = 0f;


    public void StunBoss()
    {
        if (isStunned) return;

        StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        isAttacking = true;
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

    protected override void Awake()
    {
        base.Awake();
        currentSpeed = moveSpeed;
    }

    public void ActivateBoss()
    {
        isActive = true;
        StartCoroutine(GroundPoundRoutine());
        StartCoroutine(ChargeRoutine());
    }

    private void LateUpdate()
    {
        if (transform.position.y < minYPosition)
        {
            transform.position = new Vector3(transform.position.x, minYPosition, transform.position.z);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }

    protected override void FixedUpdate()
    {
        if (!isActive || isStunned) return;

        if (!isAttacking)
        {
            AdjustSpeedBasedOnHealth();
            base.FixedUpdate();
        }
    }

    protected override void Move()
    {
        if (isAttacking) return;

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);
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

    private IEnumerator GroundPoundRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(groundPoundInterval);

            if (!isAttacking)
            {
                yield return StartCoroutine(PerformGroundPound());
            }
        }
    }

    private IEnumerator ChargeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(chargeInterval);

            if (canCharge && !isAttacking)
            {
                yield return StartCoroutine(PerformCharge());
            }
        }
    }

    private IEnumerator PerformGroundPound()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(Telegraph());

        rb.AddForce(Vector2.up * groundPoundJumpForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(1f);

        CreateShockwave();

        isAttacking = false;
    }

    private IEnumerator PerformCharge()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(ChargeTelegraph());

        float direction = movingRight ? 1f : -1f;
        float chargeEndTime = Time.time + chargeDuration;

        while (Time.time < chargeEndTime)
        {
            CheckForTurn();

            direction = movingRight ? 1f : -1f;
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

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
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

    private void CreateShockwave()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance <= shockwaveRadius)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

            if (playerRb != null)
            {
                float pushDirection = player.transform.position.x > transform.position.x ? 1f : -1f;
                float forceMult = 1f - (distance / shockwaveRadius);

                if (playerMovement != null)
                {
                    playerMovement.ApplyKnockback(0.3f);
                }

                playerRb.AddForce(new Vector2(pushDirection * shockwaveForce * forceMult, 0), ForceMode2D.Impulse);
            }

            if (playerHealth != null)
            {
                playerHealth.TakeShockwaveDamage(1f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}