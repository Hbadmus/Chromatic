using UnityEngine;
using Chromatic.UI;

public class BossHealth : EnemyHealth
{
    [SerializeField] private GameObject[] colorEnvironment;
    [SerializeField] private SpriteRenderer auraSprite;
    [SerializeField] private Color unlockColor = Color.red;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private ColorType colorToUnlock;

    public enum ColorType { Red, Blue, Green }

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

        GreenSentinelBoss greenBoss = GetComponent<GreenSentinelBoss>();
        if (greenBoss != null)
        {
            if (!greenBoss.CanTakeDamage())
            {
                return;
            }

            damage *= 0.5f;
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

        foreach (GameObject obj in colorEnvironment)
        {
            ColorTransition transition = obj.GetComponent<ColorTransition>();
            if (transition != null)
            {
                transition.StartTransition(unlockColor);
            }
        }

        base.Die();
    }
    private void UnlockColor()
    {
        if (ColorUnlockManager.Instance == null) return;

        switch (colorToUnlock)
        {
            case ColorType.Red:
                ColorUnlockManager.Instance.UnlockRed();
                break;
            case ColorType.Blue:
                ColorUnlockManager.Instance.UnlockBlue();
                break;
            case ColorType.Green:
                ColorUnlockManager.Instance.UnlockGreen();
                break;
        }

        ColorPaletteUI palette = FindObjectOfType<ColorPaletteUI>();
        if (palette != null) palette.RefreshAll();
    }
}