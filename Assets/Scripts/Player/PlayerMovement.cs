using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float coyoteTime = 0.1f;

    private float knockbackEndTime;
    private float slowEndTime;
    private float slowMultiplier = 1f;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private float lastGroundedTime;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

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
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (Time.time < knockbackEndTime)
        {
            return;
        }

        if (Time.time >= slowEndTime)
        {
            slowMultiplier = 1f;
        }

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed * slowMultiplier, rb.linearVelocity.y);
    }

    private void UpdateAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(moveInput.x));

        animator.SetBool("IsGrounded", isGrounded);

        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);

        if (moveInput.x > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (moveInput.x < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
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
            animator.SetTrigger("Jump");
        }
    }

    public void ApplyKnockback(float duration = 0.3f)
    {
        knockbackEndTime = Time.time + duration;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowEndTime = Time.time + duration;
    }
}