using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.12f;

    private Animator animator;
    private Color defaultSpriteColor;
    private Coroutine flashRoutine;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int RespawnHash = Animator.StringToHash("Respawn");

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            defaultSpriteColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerHealth == null) return;

        playerHealth.OnDamaged += HandleDamaged;
        playerHealth.OnDied += HandleDied;
        // playerHealth.OnRespawned += HandleRespawned;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandleDamaged;
            playerHealth.OnDied -= HandleDied;
            // playerHealth.OnRespawned -= HandleRespawned;
        }

        StopFlash();
    }

    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    public void SetGrounded(bool grounded)
    {
        animator.SetBool(IsGroundedHash, grounded);
    }

    public void SetVerticalVelocity(float verticalVelocity)
    {
        animator.SetFloat(VerticalVelocityHash, verticalVelocity);
    }

    public void PlayJump()
    {
        animator.SetTrigger(JumpHash);
    }

    public void PlayDeath()
    {
        animator.SetTrigger(DieHash);
    }

    public void PlayRespawn()
    {
        animator.SetTrigger(RespawnHash);
    }

    private void HandleDamaged(float damage)
    {
        if (damage <= 0f || spriteRenderer == null) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashDamage());
    }

    private void HandleDied()
    {
        StopFlash();
        PlayDeath();
    }

    private void HandleRespawned()
    {
        StopFlash();
        PlayRespawn();
    }

    private IEnumerator FlashDamage()
    {
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = defaultSpriteColor;
        flashRoutine = null;
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultSpriteColor;
        }
    }
}