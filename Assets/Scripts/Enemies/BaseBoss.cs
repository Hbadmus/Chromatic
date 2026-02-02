using UnityEngine;

public abstract class BaseBoss : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float edgeCheckDistance = 0.5f;
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected LayerMask doorLayer;


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
        Vector2 edgeCheckPos = transform.position;
        edgeCheckPos.x += movingRight ? 0.5f : -0.5f;

        Collider2D collider = GetComponent<Collider2D>();
        edgeCheckPos.y = collider.bounds.min.y;

        RaycastHit2D groundCheck = Physics2D.Raycast(edgeCheckPos, Vector2.down, 3f, groundLayer);
        Vector2 doorCheckDirection = movingRight ? Vector2.right : Vector2.left;
        RaycastHit2D doorCheck = Physics2D.Raycast(transform.position, doorCheckDirection, 2.5f, doorLayer);

        if (!groundCheck.collider || doorCheck.collider)
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