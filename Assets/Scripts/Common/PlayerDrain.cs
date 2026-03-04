using UnityEngine;
using UnityEngine.InputSystem;

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
        if (Mouse.current == null) return;
        
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryDrain();
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
}