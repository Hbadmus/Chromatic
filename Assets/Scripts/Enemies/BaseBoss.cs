using System.Collections;
using UnityEngine;

public abstract class BaseBoss : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float edgeCheckDistance = 0.5f;
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected LayerMask wallLayer;

    [Header("Combat")]
    [SerializeField] protected float contactDamage = 10f;
    [SerializeField] protected float minYPosition = 0f;
    [SerializeField] protected float knockbackForce = 15f;

    protected Rigidbody2D rb;
    protected BossHealth health;
    protected bool movingRight = true;
    protected SpriteRenderer sprite;
    protected SpriteRenderer auraSprite;
    private bool isFlashing = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<BossHealth>();
        sprite = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        BossHealth bossHealth = GetComponent<BossHealth>();
        if (bossHealth != null)
        {
            auraSprite = bossHealth.GetAuraSprite();
        }
    }

    protected virtual void FixedUpdate()
    {
        Move();
        CheckForTurn();
    }

    private void LateUpdate()
    {
        if (transform.position.y < minYPosition)
        {
            transform.position = new Vector3(transform.position.x, minYPosition, transform.position.z);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }

    protected virtual void Move()
    {
        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    protected void CheckForTurn()
    {
        Vector2 edgeCheckPos = transform.position;
        edgeCheckPos.x += movingRight ? 0.5f : -0.5f;

        Collider2D collider = GetComponent<Collider2D>();
        edgeCheckPos.y = collider.bounds.min.y;

        RaycastHit2D groundCheck = Physics2D.Raycast(edgeCheckPos, Vector2.down, 3f, groundLayer);
        Vector2 wallCheckDirection = movingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, wallCheckDirection, 1.5f, wallLayer);

        if (!groundCheck.collider || wallCheck.collider)
        {
            Turn();
        }
    }

    protected void Turn()
    {
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public IEnumerator FlashColor(Color flashColor)
    {
        if (isFlashing) yield break;

        isFlashing = true;

        if (sprite == null)
        {
            isFlashing = false;
            yield break;
        }

        Color originalColor = sprite.color;
        Color originalAuraColor = auraSprite != null ? auraSprite.color : Color.white;

        for (int i = 0; i < 3; i++)
        {
            sprite.color = flashColor;
            if (auraSprite != null) auraSprite.color = flashColor;

            yield return new WaitForSeconds(0.1f);

            sprite.color = originalColor;
            if (auraSprite != null) auraSprite.color = originalAuraColor;

            yield return new WaitForSeconds(0.1f);
        }

        isFlashing = false;
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();

            if (player != null)
            {
                player.TakeContactDamage(contactDamage);
            }

            if (playerRb != null && playerMovement != null)
            {
                Vector2 pushDirection = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDirection * knockbackForce, ForceMode2D.Impulse);
                playerMovement.ApplyKnockback(0.2f);
            }
        }
    }
}