using UnityEngine;
using UnityEngine.InputSystem;
using Chromatic.Environment;

public class PlayerDrain : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask drainLayer; 
    
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (IsGameplayInputBlocked()) return;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryDrain();
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryQuickClear();
        }
    }

    private void TryDrain()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

        Collider2D hit = Physics2D.OverlapPoint(worldPos, drainLayer);

        if (hit != null)
        {
            IDrainable drainable = hit.GetComponent<IDrainable>();

            if (drainable != null && drainable.CanDrain)
            {
                drainable.OnDrain();
            }
        }
    }

    private void TryQuickClear()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

        Collider2D hit = Physics2D.OverlapPoint(worldPos, drainLayer);

        if (hit != null)
        {
            ColorObject colorObj = hit.GetComponent<ColorObject>();

            if (colorObj != null)
            {
                colorObj.ForceResetToInitialState();
            }
        }
    }

    private bool IsGameplayInputBlocked()
    {
        return Time.timeScale <= 0f;
    }
}