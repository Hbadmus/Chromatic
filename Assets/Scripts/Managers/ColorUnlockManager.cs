using UnityEngine;
using System;

public class ColorUnlockManager : MonoBehaviour
{
    public static ColorUnlockManager Instance;

    [SerializeField] private bool redUnlocked = false;
    [SerializeField] private bool blueUnlocked = false;
    [SerializeField] private bool greenUnlocked = false;

    // Color unlock event
    public static event Action<ColorType> OnColorUnlocked;

    public enum ColorType { Red, Green, Blue }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UnlockRed()
    {
        if (!redUnlocked)
        {
            redUnlocked = true;
            OnColorUnlocked?.Invoke(ColorType.Red);
        }
    }

    public void UnlockBlue()
    {
        if (!blueUnlocked)
        {
            blueUnlocked = true;
            OnColorUnlocked?.Invoke(ColorType.Blue);
        }
    }

    public void UnlockGreen()
    {
        if (!greenUnlocked)
        {
            greenUnlocked = true;
            OnColorUnlocked?.Invoke(ColorType.Green);
        }
    }

    public bool IsColorUnlocked(ColorType colorType)
    {
        switch (colorType)
        {
            case ColorType.Red: return redUnlocked;
            case ColorType.Green: return greenUnlocked;
            case ColorType.Blue: return blueUnlocked;
            default: return false;
        }
    }

    public bool IsColorUnlocked(int colorIndex)
    {
        switch (colorIndex)
        {
            case 0: return true;
            case 1: return redUnlocked;
            case 2: return greenUnlocked;
            case 3: return blueUnlocked;
            default: return false;
        }
    }

    public bool AreAllColorsUnlocked(params ColorType[] colorTypes)
    {
        foreach (ColorType colorType in colorTypes)
        {
            if (!IsColorUnlocked(colorType)) return false;
        }
        return true;
    }
}