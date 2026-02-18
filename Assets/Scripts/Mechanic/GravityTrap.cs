using UnityEngine;
using System.Collections;
using Chromatic.Combat;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class GravityTrap : MonoBehaviour, IInteractiveTarget
{
    [SerializeField] private int maxHitNumber = 3;
    [SerializeField] private float resetTime = 5f;
    [SerializeField] private Color initialColor = Color.white;
    [SerializeField] private Color finalColor = Color.black;
    [SerializeField] private bool isInBossRoom = false;
    [SerializeField] private float respawnDelay = 10f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 positionWhenFell;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private bool isReacting = false;
    private int hitNumber = 0;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        sr.color = initialColor;
    }

    public void OnHit(float damage)
    {
        if (isReacting) return;
        ChangeColor();
        if (hitNumber >= maxHitNumber)
        {
            StartCoroutine(ProcessGravityReaction());
        }
    }

    private IEnumerator ProcessGravityReaction()
    {
        isReacting = true;
        sr.color = finalColor;
        positionWhenFell = transform.position;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.mass = 100000f;

        yield return new WaitForSeconds(resetTime);

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        yield return StartCoroutine(SmoothReturn(originalPosition, originalRotation, 3f));

        ResetProgress();
    }

    public void ChangeColor()
    {
        if (maxHitNumber <= 0) return;

        hitNumber++;

        float t = (hitNumber / (float)maxHitNumber) * 0.5f;
        t = Mathf.Clamp01(t);

        ApplyColor(Color.Lerp(sr.color, finalColor, t));
    }

    private void ApplyColor(Color c)
    {
        if (sr != null) sr.color = c;
    }

    public void ResetProgress()
    {
        hitNumber = 0;
        isReacting = false;
        ApplyColor(initialColor);
    }

    private IEnumerator SmoothReturn(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Color startColor = sr.color;

        if (duration <= 0f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, easedT);
            ApplyColor(Color.Lerp(startColor, initialColor, easedT));

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb.bodyType == RigidbodyType2D.Dynamic && isInBossRoom)
        {
            float distanceFallen = positionWhenFell.y - transform.position.y;

            if (distanceFallen < 0.5f) return;

            if (collision.gameObject.CompareTag("Boss"))
            {
                RedWardenBoss boss = collision.gameObject.GetComponent<RedWardenBoss>();

                if (boss != null)
                {
                    boss.StunBoss();
                }

                StopAllCoroutines();
                StartCoroutine(RespawnPlatform());
            }
            else if (collision.gameObject.CompareTag("Ground"))
            {
                StopAllCoroutines();
                StartCoroutine(RespawnPlatform());
            }
        }
    }

    private IEnumerator RespawnPlatform()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        sr.enabled = false;
        platformCollider.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        sr.enabled = true;
        platformCollider.enabled = true;
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        ResetProgress();
    }
}