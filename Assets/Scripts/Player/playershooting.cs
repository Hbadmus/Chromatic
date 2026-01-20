using UnityEngine;
using UnityEngine.InputSystem;

namespace Chromatic.Player
{
    public class PlayerShooting : MonoBehaviour
    {
        [Header("shooting setting")]
        [SerializeField] private Transform firePoint;     
        [SerializeField] private GameObject bulletPrefab; 

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