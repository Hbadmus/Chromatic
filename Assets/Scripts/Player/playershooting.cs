using UnityEngine;
using UnityEngine.InputSystem;
using Chromatic.UI;
using Chromatic.Combat;

namespace Chromatic.Player
{
    public class PlayerShooting : MonoBehaviour
    {
        [Header("Shooting Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private ColorPaletteUI paletteUI;

        private Camera mainCamera;
        private Animator animator;

        [Header("Color Settings")]
        [SerializeField]
        private Color[] availableColors = new Color[]
        {
            Color.black,
            Color.red,
            Color.green,
            Color.blue
        };
        private int currentColorIndex = 0;

        private void Start()
        {
            mainCamera = Camera.main;
            animator = GetComponent<Animator>();
            if (paletteUI != null)
                paletteUI.UpdateSelection(currentColorIndex);
        }

        private void Update()
        {
            if (IsGameplayInputBlocked()) return;

            AimAtMouse();
            HandleColorSwitching();
        }

        private void HandleColorSwitching()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) TrySetColor(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) TrySetColor(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) TrySetColor(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) TrySetColor(3);
        }

        private void TrySetColor(int index)
        {
            if (index < 0 || index >= availableColors.Length) return;

            if (!ColorUnlockManager.Instance.IsColorUnlocked(index)) return;

            currentColorIndex = index;

            if (paletteUI != null)
            {
                paletteUI.UpdateSelection(currentColorIndex);
            }
        }

        private void AimAtMouse()
        {
            if (firePoint == null) return;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            mouseWorldPosition.z = 0f;

            Vector3 direction = mouseWorldPosition - firePoint.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            firePoint.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (IsGameplayInputBlocked()) return;

            if (context.performed)
            {
                Shoot();
            }
        }

        private void Shoot()
        {
            if (IsGameplayInputBlocked()) return;

            animator.SetTrigger("Shoot");

            if (bulletPrefab != null && firePoint != null)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

                Projectile projectile = bulletObj.GetComponent<Projectile>();

                if (projectile != null)
                {
                    Color colorToSend = availableColors[currentColorIndex];
                    projectile.Initialize(colorToSend, firePoint.right);
                }
            }
        }

        private bool IsGameplayInputBlocked()
        {
            return Time.timeScale <= 0f;
        }
    }
}