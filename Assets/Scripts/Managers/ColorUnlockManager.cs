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

    // Register a subscriber and immediately replay already-unlocked colors to it.
    public void RegisterSubscriber(Action<ColorType> subscriber)
    {
        OnColorUnlocked += subscriber;

        // Replay already unlocked colors so late subscribers catch up
        if (redUnlocked) subscriber?.Invoke(ColorType.Red);
        if (greenUnlocked) subscriber?.Invoke(ColorType.Green);
        if (blueUnlocked) subscriber?.Invoke(ColorType.Blue);
    }

    // Unregister a previously registered subscriber
    public void UnregisterSubscriber(Action<ColorType> subscriber)
    {
        OnColorUnlocked -= subscriber;
    }

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
            
            // Replace healthbar when red is unlocked
            HealthBarController healthBarController = FindFirstObjectByType<HealthBarController>();
            if (healthBarController != null)
            {
                healthBarController.RegenerateHearts();
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
                if (playerSprite != null)
                {
                    playerSprite.color = Color.Lerp(playerSprite.color, Color.white, 1f);
                    Debug.Log("Player color updated to reflect red unlock");
                }
            }
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