using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private Health bossHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject root;

    private void OnEnable()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged += Refresh;

        if (bossHealth != null)
            bossHealth.OnDied += HandleDied;

        slider.maxValue = bossHealth.MaxHealth;

        Refresh();
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged -= Refresh;

        if (bossHealth != null)
            bossHealth.OnDied -= HandleDied;
    }

    private void Refresh()
    {
        if (bossHealth == null || slider == null) return;

        slider.value = bossHealth.CurrentHealth;

        if (root != null)
            root.SetActive(!bossHealth.IsDead);
    }

    private void HandleDied()
    {
        if (root != null) root.SetActive(false);
    }
}
