using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private Health bossHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject root;

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

        Refresh();
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
    }

    private void HandleDied()
    {
        if (root != null) root.SetActive(false);
    }
}
