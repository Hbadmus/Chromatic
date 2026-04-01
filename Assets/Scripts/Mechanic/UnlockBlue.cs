using UnityEngine;
using Chromatic.UI;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class BlueUnlockPoint : MonoBehaviour
{
    private bool hasTriggered = false;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (ColorUnlockManager.Instance == null) return;

        hasTriggered = true;

        if (!ColorUnlockManager.Instance.IsColorUnlocked(ColorUnlockManager.ColorType.Blue))
        {
            ColorUnlockManager.Instance.UnlockBlue();
        }

        ColorPaletteUI palette = FindFirstObjectByType<ColorPaletteUI>();
        if (palette != null)
        {
            palette.RefreshAll();
        }
    }
}