using UnityEngine;

public abstract class BaseBoss : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float edgeCheckDistance = 0.5f;
    [SerializeField] protected LayerMask groundLayer;
    
    [Header("Combat")]
    [SerializeField] protected float contactDamage = 0.5f;
    
    protected Rigidbody2D rb;
    protected BossHealth health;
    protected bool movingRight = true;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<BossHealth>();
    }

    protected virtual void FixedUpdate()
    {
        Move();
        CheckForTurn();
    }

    protected virtual void Move()
    {
        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    protected void CheckForTurn()
    {
        // Start raycast from bottom of collider
        Collider2D collider = GetComponent<Collider2D>();
        Vector2 edgeCheckPos = collider.bounds.center;
        edgeCheckPos.y = collider.bounds.min.y; // Bottom of collider
        edgeCheckPos.x += movingRight ? 0.5f : -0.5f;

        RaycastHit2D groundCheck = Physics2D.Raycast(edgeCheckPos, Vector2.down, 0.5f, groundLayer);

        Vector2 wallCheckDirection = movingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, wallCheckDirection, 0.5f, groundLayer);

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

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeContactDamage(contactDamage);
            }
        }
    }
}