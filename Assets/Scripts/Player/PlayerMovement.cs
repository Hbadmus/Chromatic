using UnityEngine;
using UnityEngine.InputSystem;

namespace Chromatic.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
        [SerializeField] private LayerMask groundLayer;
        #endregion

        #region Private Fields
        private Rigidbody2D rb;
        private Vector2 moveInput;
        private bool isGrounded;
        private float targetSpeed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            // Configure rigidbody for smooth 2D platformer physics
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            CheckGroundStatus();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleBetterJump();
        }
        #endregion

        #region Input Callbacks
        // Called by Input System when player moves (WASD/arrows)
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        // Called by Input System when player presses jump
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed && isGrounded)
            {
                Jump();
            }
        }
        #endregion

        #region Movement Methods
        // Smoothly accelerates/decelerates player to target speed
        private void HandleMovement()
        {
            targetSpeed = moveInput.x * moveSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
        }

        // Applies upward force for jump
        private void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Variable jump height - hold jump = higher, tap = lower
        private void HandleBetterJump()
        {
            if (rb.linearVelocity.y < 0)
            {
        // Falling - extra gravity for snappier descent
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (rb.linearVelocity.y > 0 && moveInput.y <= 0)
            {
        // Released jump early - cut it short
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
        #endregion

        #region Ground Detection
        // Checks if player is touching ground using box overlap
        private void CheckGroundStatus()
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }
        #endregion

        #region Gizmos
        // Visualizes ground check in Scene view for debugging
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
            }
        }
        #endregion
    }
}