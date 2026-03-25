using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles color transitions, unlock event responses, and damage effects.
/// All objects subscribe to color unlock events through ColorUnlockConfig.
/// </summary>
public class ColorTransition : MonoBehaviour
{
    [System.Serializable]
    public class ColorUnlockConfig
    {
        [Tooltip("Color types required for this object (leave empty to show immediately at start)")]
        public ColorUnlockManager.ColorType[] requiredColors = new ColorUnlockManager.ColorType[0];
        
        [Tooltip("Color to display when all required colors are unlocked")]
        public Color displayColor = Color.white;
    }

    [SerializeField] private ColorUnlockConfig colorUnlockConfig;
    [SerializeField] private float transitionDuration = 3f;
    [SerializeField] private float damagePerSecond = 1f;

    private SpriteRenderer spriteRend;
    private Renderer rend;
    private bool burning = false;
    private List<Health> touching = new List<Health>();
    private Coroutine transitionCoroutine;

    private void OnEnable()
    {
        ColorUnlockManager.OnColorUnlocked += OnColorUnlocked;
    }

    private void OnDisable()
    {
        ColorUnlockManager.OnColorUnlocked -= OnColorUnlocked;
    }

    private void Awake()
    {
        spriteRend = GetComponent<SpriteRenderer>();
        rend = GetComponent<Renderer>();

        if (spriteRend == null && rend == null)
        {
            Debug.LogWarning($"{name}: ColorTransition requires SpriteRenderer or Renderer.");
        }

        // Initialize: check if all required colors are unlocked
        CheckAndUpdateColor();
    }

    private void Update()
    {
        if (!burning) return;
        touching.RemoveAll(h => h == null);
        float dmg = damagePerSecond * Time.deltaTime;
        foreach (Health h in touching)
        {
            h.TakeDamage(dmg);
        }
    }

    private void OnColorUnlocked(ColorUnlockManager.ColorType colorType)
    {
        // Check if this unlocked color is in our required colors list
        if (HasRequiredColor(colorType))
        {
            CheckAndUpdateColor();
        }
    }

    private bool HasRequiredColor(ColorUnlockManager.ColorType colorType)
    {
        if (colorUnlockConfig.requiredColors == null || colorUnlockConfig.requiredColors.Length == 0)
            return false;

        foreach (ColorUnlockManager.ColorType requiredColor in colorUnlockConfig.requiredColors)
        {
            if (requiredColor == colorType)
                return true;
        }
        return false;
    }

    private void CheckAndUpdateColor()
    {
        if (ColorUnlockManager.Instance == null) return;

        // Check if all required colors are unlocked
        if (ColorUnlockManager.Instance.AreAllColorsUnlocked(colorUnlockConfig.requiredColors))
        {
            // Display the color
            if (transitionDuration > 0)
            {
                TransitionToColor(colorUnlockConfig.displayColor);
            }
            else
            {
                SetColor(colorUnlockConfig.displayColor);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!burning) return;
        Health h = other.GetComponent<Health>();
        if (h != null && !touching.Contains(h))
            touching.Add(h);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Health h = other.GetComponent<Health>();
        if (h != null)
            touching.Remove(h);
    }

    public void StartTransition(Color targetColor)
    {
        TransitionToColor(targetColor);
    }

    public void StopBurning()
    {
        burning = false;
        touching.Clear();
    }

    private void TransitionToColor(Color targetColor)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(FadeToColor(targetColor));
    }

    private void SetColor(Color color)
    {
        if (spriteRend != null)
        {
            spriteRend.color = color;
            return;
        }

        if (rend != null)
        {
            rend.material.color = color;
        }
    }

    private Color GetCurrentColor()
    {
        if (spriteRend != null)
            return spriteRend.color;

        if (rend != null)
            return rend.material.color;

        return Color.white;
    }

    private IEnumerator FadeToColor(Color targetColor)
    {
        Color startColor = GetCurrentColor();
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            SetColor(Color.Lerp(startColor, targetColor, t));
            yield return null;
        }

        SetColor(targetColor);

        if (IsRed(targetColor))
        {
            burning = true;
        }
        else
        {
            burning = false;
            touching.Clear();
        }

        transitionCoroutine = null;
    }

    private bool IsRed(Color c)
    {
        return Mathf.Abs(c.r - 1f) < 0.1f && c.g < 0.1f && c.b < 0.1f;
    }

    // Debug helper to show current status in Inspector
    [ContextMenu("Show Current Status")]
    public void DebugShowStatus()
    {
        if (ColorUnlockManager.Instance == null)
        {
            Debug.LogWarning("ColorUnlockManager not found!");
            return;
        }

        bool allUnlocked = ColorUnlockManager.Instance.AreAllColorsUnlocked(colorUnlockConfig.requiredColors);
        Debug.Log($"{gameObject.name} - All colors unlocked: {allUnlocked}");
        
        foreach (var color in colorUnlockConfig.requiredColors)
        {
            Debug.Log($"  {color}: {ColorUnlockManager.Instance.IsColorUnlocked(color)}");
        }
    }
}