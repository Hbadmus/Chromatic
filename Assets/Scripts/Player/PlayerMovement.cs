using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float coyoteTime = 0.1f;

    private float knockbackEndTime;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private float lastGroundedTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        PhysicsMaterial2D noFriction = new PhysicsMaterial2D();
        noFriction.friction = 0;
        noFriction.bounciness = 0;
        GetComponent<Collider2D>().sharedMaterial = noFriction;
    }

    private void Update()
    {
        if (Mathf.Abs(rb.linearVelocity.y) < 0.1f && rb.linearVelocity.y <= 0)
        {
            lastGroundedTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time < knockbackEndTime)
        {
            return;
        }

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && Time.time - lastGroundedTime < coyoteTime)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public void ApplyKnockback(float duration = 0.3f)
    {
        knockbackEndTime = Time.time + duration;
    }
}