using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;

    private float knockbackEndTime;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CheckGrounded();
    }

    private void FixedUpdate()
    {
        if (Time.time < knockbackEndTime)
        {
            return;
        }

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckGrounded()
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        Vector2 bottom = new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.min.y);

        RaycastHit2D hit = Physics2D.Raycast(bottom, Vector2.down, 0.1f);
        isGrounded = hit.collider != null;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public void ApplyKnockback(float duration = 0.3f)
    {
        knockbackEndTime = Time.time + duration;
    }
}