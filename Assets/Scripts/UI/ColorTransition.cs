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
    private bool burning = false;
    private List<Health> touching = new List<Health>();
    private Coroutine transitionCoroutine;

    private void OnEnable()
    {
        Debug.Log($"ColorTransition ON for {gameObject.name} (enabled). Required: { (colorUnlockConfig == null ? "null" : colorUnlockConfig.requiredColors.Length.ToString()) }");

        // Register with manager (and get immediate replay of already-unlocked colors).
        StartCoroutine(EnsureManagerThenRegister());
    }

    private void OnDisable()
    {
        // Unregister if we previously registered with the manager
        if (isRegistered && ColorUnlockManager.Instance != null)
        {
            ColorUnlockManager.Instance.UnregisterSubscriber(OnColorUnlocked);
            isRegistered = false;
        }

        Debug.Log($"ColorTransition OFF for {gameObject.name} (disabled)");
    }

    private void Awake()
    {
        spriteRend = GetComponent<SpriteRenderer>();
        if (spriteRend == null)
        {
            Debug.LogWarning($"{name}: ColorTransition requires SpriteRenderer.");
        }

        Debug.Log($"ColorTransition Awake on {name}: spriteRend={(spriteRend!=null)} activeSelf={gameObject.activeSelf}");

        // Initialize: attempt initial check (may early-return if manager not yet initialized)
        CheckAndUpdateColor();
    }

    private System.Collections.IEnumerator EnsureManagerThenCheck()
    {
        // Wait up to a few frames for ColorUnlockManager to initialize
        int attempts = 0;
        while (ColorUnlockManager.Instance == null && attempts < 5)
        {
            attempts++;
            yield return null;
        }

        // Now perform the color check/update
        CheckAndUpdateColor();
    }

    private bool isRegistered = false;

    private System.Collections.IEnumerator EnsureManagerThenRegister()
    {
        int attempts = 0;
        while (ColorUnlockManager.Instance == null && attempts < 5)
        {
            attempts++;
            yield return null;
        }

        if (ColorUnlockManager.Instance != null)
        {
            ColorUnlockManager.Instance.RegisterSubscriber(OnColorUnlocked);
            isRegistered = true;
        }

        // After registering, force a check in case some colors were already unlocked
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
            Debug.Log($"{name}: OnColorUnlocked event received for {colorType}");
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

        ColorUnlockManager.ColorType[] required = colorUnlockConfig.requiredColors;

        Debug.Log($"{name}: Checking colors. Required count={ (required==null?0:required.Length) }");
        if (required != null)
        {
            foreach (var c in required)
            {
                bool unlocked = ColorUnlockManager.Instance.IsColorUnlocked(c);
                Debug.Log($"{name}: required {c} unlocked={unlocked}");
            }
        }

        if (required == null || required.Length == 0)
        {
            ApplyColor(colorUnlockConfig.displayColor);
            return;
        }

        // Check if all required colors are unlocked
        bool allUnlocked = ColorUnlockManager.Instance.AreAllColorsUnlocked(required);
        Debug.Log($"{name}: allUnlocked={allUnlocked}");
        if (allUnlocked)
        {
            ApplyColor(colorUnlockConfig.displayColor);
            return;
        }

        Color blendedUnlockedColor = GetBlendedUnlockedRequiredColor(required, out int unlockedCount);
        if (unlockedCount > 0)
        {
            Debug.Log($"{name}: Applying blended color from {unlockedCount} unlocked");
            ApplyColor(blendedUnlockedColor);
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

    private void ApplyColor(Color color)
    {
        if (transitionDuration > 0)
        {
            TransitionToColor(color);
        }
        else
        {
            SetColor(color);
            UpdateBurningState(color);
        }
    }

    private Color GetBlendedUnlockedRequiredColor(ColorUnlockManager.ColorType[] required, out int unlockedCount)
    {
        Color colorSum = Color.black;
        unlockedCount = 0;

        foreach (ColorUnlockManager.ColorType colorType in required)
        {
            if (!ColorUnlockManager.Instance.IsColorUnlocked(colorType))
                continue;

            colorSum += GetColorForType(colorType);
            unlockedCount++;
        }

        if (unlockedCount == 0)
            return GetCurrentColor();

        colorSum.a = 1f;
        return colorSum / unlockedCount;
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

    private void SetColor(Color color)
    {
        if (spriteRend != null)
        {
            Debug.Log($"{name}: SetColor -> {color}");
            spriteRend.color = color;
        }
    }

    private Color GetCurrentColor()
    {
        if (spriteRend != null)
            return spriteRend.color;

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
        UpdateBurningState(targetColor);

        transitionCoroutine = null;
    }

    private void UpdateBurningState(Color targetColor)
    {
        if (IsRed(targetColor))
        {
            burning = true;
        }
        else
        {
            burning = false;
            touching.Clear();
        }
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