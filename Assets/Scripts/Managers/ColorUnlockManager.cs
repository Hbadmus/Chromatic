using UnityEngine;

public class ColorUnlockManager : MonoBehaviour
{
    public static ColorUnlockManager Instance;

    private bool redUnlocked = false;
    private bool blueUnlocked = false;
    private bool greenUnlocked = false;

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

    public void UnlockRed() => redUnlocked = true;
    public void UnlockBlue() => blueUnlocked = true;
    public void UnlockGreen() => greenUnlocked = true;

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
}