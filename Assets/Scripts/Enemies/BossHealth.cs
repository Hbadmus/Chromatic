using UnityEngine;

public class BossHealth : EnemyHealth
{
    [SerializeField] private GameObject[] redEnvironment;
    [SerializeField] private SpriteRenderer auraSprite;

    public bool IsVulnerable { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        IsVulnerable = false;
        UpdateAura();
    }

    public void MakeVulnerable(float duration)
    {
        IsVulnerable = true;
        UpdateAura();
        Invoke(nameof(MakeInvulnerable), duration);
    }

    private void MakeInvulnerable()
    {
        IsVulnerable = false;
        UpdateAura();
    }

    private void UpdateAura()
    {
        if (auraSprite != null)
        {
            auraSprite.enabled = !IsVulnerable;
        }
    }

    public override void TakeDamage(float damage)
    {
        if (!IsVulnerable) return;

        base.TakeDamage(damage);
    }

    protected override void Die()
    {
        foreach (GameObject obj in redEnvironment)
        {
            ColorTransition transition = obj.GetComponent<ColorTransition>();
            if (transition != null)
            {
                transition.StartTransition();
            }
        }

        base.Die();
    }
}