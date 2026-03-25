using System.Collections;
using UnityEngine;
using Chromatic.Combat;

public class IngrainVine : MonoBehaviour, IInteractiveTarget
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 75f;

    [Header("Growth")]
    [SerializeField] private float growthDuration = 0.5f;

    private float currentHealth;
    private GreenSentinelBoss boss;
    private SpriteRenderer[] allSprites;

    private void Awake()
    {
        currentHealth = maxHealth;
        allSprites = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(GrowVine());
    }

    private IEnumerator GrowVine()
    {
        Vector3 targetScale = transform.localScale;
        transform.localScale = new Vector3(targetScale.x, 0, targetScale.z);

        float elapsed = 0f;

        while (elapsed < growthDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growthDuration;

            transform.localScale = Vector3.Lerp(new Vector3(targetScale.x, 0, targetScale.z), targetScale, t);

            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void Update()
    {
        float t = Mathf.PingPong(Time.time * 0.5f, 1f);
        Color pulseColor = Color.Lerp(Color.gray, Color.green, t * 0.3f);

        foreach (SpriteRenderer sprite in allSprites)
        {
            sprite.color = pulseColor;
        }
    }

    public void SetBoss(GreenSentinelBoss bossRef)
    {
        boss = bossRef;
    }

    public void OnHit(float damage, Color color)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            DestroyVine();
        }
    }

    private void DestroyVine()
    {
        if (boss != null)
        {
            boss.RemoveIngrainVine(gameObject);
        }

        Destroy(gameObject);
    }
}