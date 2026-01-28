using UnityEngine;
using UnityEngine.InputSystem;

namespace Chromatic.Player
{
    public class PlayerShooting : MonoBehaviour
    {
        [Header("Shooting Settings")]
        [SerializeField] private Transform firePoint;     
        [SerializeField] private GameObject bulletPrefab; 

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            AimAtMouse();
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
            if (context.performed)
            {
                Shoot();
            }
        }

        private void Shoot()
        {
            if (bulletPrefab != null && firePoint != null)
            {
                Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            }
        }
    }
}