using UnityEngine;

public class ShutDoor : MonoBehaviour
{
    [SerializeField] private float slideSpeed = 3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isClosed = false;
    private bool canClose = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void StartClosing()
    {
        canClose = true;
    }

    void FixedUpdate()
    {
        if (canClose && !isClosed)
        {
            Collider2D doorCollider = GetComponent<Collider2D>();
            Vector2 bottomCenter = new Vector2(doorCollider.bounds.center.x, doorCollider.bounds.min.y);

            RaycastHit2D hit = Physics2D.Raycast(bottomCenter, Vector2.down, 0.1f, groundLayer);

            if (hit.collider != null && hit.collider != doorCollider)
            {
                isClosed = true;
                return;
            }

            Vector2 newPosition = rb.position + Vector2.down * slideSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }
}