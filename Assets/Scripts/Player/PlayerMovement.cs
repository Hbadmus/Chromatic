using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Chromatic.Environment;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float groundNormalThreshold = 0.45f;
    [SerializeField] private float groundContactGraceTime = 0.06f;

    [Header("SFX")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private float footstepInterval = 0.35f;

    private float knockbackEndTime;
    private float slowEndTime;
    private float slowMultiplier = 1f;
    private float nextFootstepTime;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private float lastGroundedTime;
    private float lastGroundContactTime = -999f;
    private bool isGrounded;

    // Track each collider providing ground support independently so that
    // exiting a wall or adjacent platform doesn't falsely clear grounded state.
    private readonly HashSet<int> groundColliderIds = new();
    private ColorObject groundColorObj;

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
        bool withinGroundGrace = (Time.time - lastGroundContactTime) <= groundContactGraceTime && rb.linearVelocity.y <= 0.2f;

        if (groundColliderIds.Count > 0 || withinGroundGrace)
        {
            lastGroundedTime = Time.time;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        UpdateAnimations();
        UpdateFootstep();
    }

    private void FixedUpdate()
    {
        if (DialogueManager.IsActive)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

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

    private void UpdateFootstep()
    {
        if (!isGrounded || Mathf.Abs(moveInput.x) < 0.1f || Time.time < nextFootstepTime) return;
        nextFootstepTime = Time.time + footstepInterval;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(footstepClip);
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
        if (DialogueManager.IsActive) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (DialogueManager.IsActive) return;
        if (context.performed && Time.time - lastGroundedTime < coyoteTime)
        {
            float force = (groundColorObj != null && groundColorObj.IsGreenBounceActive)
                ? groundColorObj.GreenBounceForce
                : jumpForce;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            lastGroundedTime = -999f;
            animator.SetTrigger("Jump");
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(jumpClip);
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGroundContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckGroundContact(collision);
    }

    private void CheckGroundContact(Collision2D collision)
    {
        int id = collision.collider.GetInstanceID();
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > groundNormalThreshold)
            {
                groundColliderIds.Add(id);
                lastGroundContactTime = Time.time;

                ColorObject co = collision.gameObject.GetComponent<ColorObject>();
                if (co != null)
                {
                    groundColorObj = co;
                }
                return;
            }
        }
        // No upward contact from this collider — remove it from ground set.
        groundColliderIds.Remove(id);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        int id = collision.collider.GetInstanceID();
        groundColliderIds.Remove(id);

        if (groundColliderIds.Count == 0)
        {
            ColorObject co = collision.gameObject.GetComponent<ColorObject>();
            if (co != null && co == groundColorObj)
            {
                groundColorObj = null;
            }
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
