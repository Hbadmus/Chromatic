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
            
            // 初始化 UI
            if (paletteUI != null)
                paletteUI.UpdateSelection(currentColorIndex);
        }

        private void Update()
        {
            // 1. 每一帧都更新枪口朝向 (这一步非常重要，没了它就只能往右射)
            AimAtMouse();
            
            // 2. 检测切枪按键
            HandleColorSwitching();
        }

        // --- 你的瞄准逻辑 (完美保留) ---
        private void AimAtMouse()
        {
            if (firePoint == null) return;

            // 获取鼠标位置
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            mouseWorldPosition.z = 0f;

            // 计算方向
            Vector3 direction = mouseWorldPosition - firePoint.position;

            // 计算角度并旋转 firePoint
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0, 0, angle);
        }

        // --- 切枪逻辑 ---
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

            // 检查解锁状态
            if (ColorUnlockManager.Instance != null && !ColorUnlockManager.Instance.IsColorUnlocked(index)) 
                return;

            currentColorIndex = index;

            if (paletteUI != null)
            {
                paletteUI.UpdateSelection(currentColorIndex);
            }
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Shoot();
            }
        }

        // --- 射击逻辑 ---
        private void Shoot()
        {
            if (bulletPrefab != null && firePoint != null)
            {
                // 1. 生成子弹 (继承 firePoint 的旋转角度，所以子弹图片也是朝向鼠标的)
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

                // 2. 获取子弹脚本
                Projectile projectile = bulletObj.GetComponent<Projectile>();

                if (projectile != null)
                {
                    Color colorToSend = availableColors[currentColorIndex];
                    
                    // 3. 初始化子弹
                    // 关键点：firePoint.right 代表的是 firePoint 当前旋转后的“红色X轴”方向
                    // 因为 AimAtMouse 已经把 X 轴转得对准鼠标了，所以这里传 firePoint.right 是完全正确的！
                    projectile.Initialize(colorToSend, firePoint.right);
                }
            }
        }
    }
}