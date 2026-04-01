using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private Health bossHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject root;
    [SerializeField] private Image fillImage;

    private Image resolvedFillImage;
    private bool isRegistered = false;

    private void OnEnable()
    {
        Debug.Log($"BossHealthUI enabled. Boss: {bossHealth != null}, Slider: {slider != null}");

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += Refresh;
            bossHealth.OnDied += HandleDied;
            Debug.Log($"Max Health: {bossHealth.MaxHealth}, Current: {bossHealth.CurrentHealth}");
        }

        if (slider != null)
        {
            slider.maxValue = bossHealth.MaxHealth;
            Debug.Log($"Slider max set to: {slider.maxValue}");
        }

        // Resolve the fill Image (allow override via inspector)
        if (fillImage != null)
            resolvedFillImage = fillImage;
        else if (slider != null && slider.fillRect != null)
            resolvedFillImage = slider.fillRect.GetComponent<Image>();

        // Register with ColorUnlockManager to update fill color when Red unlocks
        StartCoroutine(EnsureManagerThenRegister());

        Refresh();
    }

    private IEnumerator EnsureManagerThenRegister()
    {
        while (ColorUnlockManager.Instance == null)
            yield return null;

        ColorUnlockManager.Instance.RegisterSubscriber(OnColorUnlocked);
        isRegistered = true;

        // Immediately apply color if Red already unlocked
        if (resolvedFillImage != null && ColorUnlockManager.Instance.IsColorUnlocked(ColorUnlockManager.ColorType.Red))
        {
            resolvedFillImage.color = Color.red;
        }
    }

    private void Refresh()
    {
        if (bossHealth == null || slider == null) return;

        Debug.Log($"Refreshing health bar: {bossHealth.CurrentHealth} / {bossHealth.MaxHealth}");
        slider.value = bossHealth.CurrentHealth;

        if (root != null)
            root.SetActive(!bossHealth.IsDead);
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged -= Refresh;

        if (bossHealth != null)
            bossHealth.OnDied -= HandleDied;

        if (isRegistered && ColorUnlockManager.Instance != null)
        {
            ColorUnlockManager.Instance.UnregisterSubscriber(OnColorUnlocked);
            isRegistered = false;
        }
    }

    private void OnColorUnlocked(ColorUnlockManager.ColorType colorType)
    {
        if (resolvedFillImage == null) return;

        if (colorType == ColorUnlockManager.ColorType.Red)
            resolvedFillImage.color = Color.red;
    }

    private void HandleDied()
    {
        if (root != null) root.SetActive(false);
    }
}
