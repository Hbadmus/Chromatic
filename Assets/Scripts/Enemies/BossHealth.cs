using UnityEngine;
using Chromatic.UI;

public class BossHealth : EnemyHealth
{
    [SerializeField] private SpriteRenderer auraSprite;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private ColorUnlockManager.ColorType colorToUnlock;
    [SerializeField] private BossGateBlock gateBlock;

    // Old system compatibility: if still using ColorTransition component
    [SerializeField] private GameObject[] colorEnvironment;

    public bool IsVulnerable { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        IsVulnerable = true;
        UpdateAura();
    }

    public SpriteRenderer GetAuraSprite()
    {
        return auraSprite;
    }

    public void MakeVulnerable(float duration)
    {
        IsVulnerable = true;
        UpdateAura();
        Invoke(nameof(MakeInvulnerable), duration);
    }

    public void MakeInvulnerable()
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

        GreenSentinelBoss greenBoss = GetComponent<GreenSentinelBoss>();
        if (greenBoss != null)
        {
            if (!greenBoss.CanTakeDamage())
            {
                return;
            }

            damage *= 0.5f;
        }

        VoidTyrantBoss voidBoss = GetComponent<VoidTyrantBoss>();
        if (voidBoss != null)
        {
            voidBoss.OnDamageTaken(damage);
        }

        base.TakeDamage(damage);

        BaseBoss boss = GetComponent<BaseBoss>();
        if (boss != null)
        {
            StartCoroutine(boss.FlashColor(flashColor));
        }
    }

    protected override void Die()
    {
        UnlockColor();

        GreenSentinelBoss greenBoss = GetComponent<GreenSentinelBoss>();
        if (greenBoss != null)
        {
            greenBoss.CleanupVines();
        }

        // Old system compatibility: trigger color transitions for environment objects
        if (colorEnvironment != null)
        {
            foreach (GameObject obj in colorEnvironment)
            {
                ColorTransition transition = obj.GetComponent<ColorTransition>();
                if (transition != null)
                {
                    transition.StartTransition(GetColorForType(colorToUnlock));
                }
            }
        }

        if (gateBlock != null) gateBlock.OnBossDefeated();

        base.Die();
    }

    private void UnlockColor()
    {
        if (ColorUnlockManager.Instance == null) return;

        switch (colorToUnlock)
        {
            case ColorUnlockManager.ColorType.Red:
                ColorUnlockManager.Instance.UnlockRed();
                break;
            case ColorUnlockManager.ColorType.Blue:
                ColorUnlockManager.Instance.UnlockBlue();
                break;
            case ColorUnlockManager.ColorType.Green:
                ColorUnlockManager.Instance.UnlockGreen();
                break;
        }

        ColorPaletteUI palette = FindFirstObjectByType<ColorPaletteUI>();
        if (palette != null) palette.RefreshAll();
    }

    private Color GetColorForType(ColorUnlockManager.ColorType colorType)
    {
        return colorType switch
        {
            ColorUnlockManager.ColorType.Red => Color.red,
            ColorUnlockManager.ColorType.Green => Color.green,
            ColorUnlockManager.ColorType.Blue => Color.blue,
            _ => Color.white
        };
    }
}