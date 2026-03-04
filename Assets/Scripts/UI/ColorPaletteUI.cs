using UnityEngine;
using UnityEngine.UI;

namespace Chromatic.UI
{
    public class ColorPaletteUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image[] colorSlots;

        [Header("Colors")]
        [SerializeField] private Color[] slotColors = new Color[]
        {
            Color.black,
            Color.red,
            Color.green,
            Color.blue
        };

        [Header("Settings")]
        [SerializeField] private Color lockedColor = Color.white;
        [SerializeField] private float normalScale = 0.3f;
        [SerializeField] private float selectedScale = 0.4f;

        [Header("Border")]
        [SerializeField] private Color borderColor = Color.yellow;
        [SerializeField] private float borderSize = 4f;

        private int currentSelection = 0;
        private Outline[] slotOutlines;

        private void Start()
        {
            // 给每个槽位自动添加 Outline 组件
            slotOutlines = new Outline[colorSlots.Length];
            for (int i = 0; i < colorSlots.Length; i++)
            {
                if (colorSlots[i] == null) continue;
                Outline outline = colorSlots[i].GetComponent<Outline>();
                if (outline == null)
                    outline = colorSlots[i].gameObject.AddComponent<Outline>();
                outline.effectColor = borderColor;
                outline.effectDistance = new Vector2(borderSize, borderSize);
                outline.enabled = false;
                slotOutlines[i] = outline;
            }
            RefreshAll();
        }

        public void UpdateSelection(int index)
        {
            currentSelection = index;
            RefreshAll();
        }

        // 刷新所有槽位的颜色和状态
        public void RefreshAll()
        {
            for (int i = 0; i < colorSlots.Length; i++)
            {
                if (colorSlots[i] == null) continue;

                bool unlocked = ColorUnlockManager.Instance != null 
                    && ColorUnlockManager.Instance.IsColorUnlocked(i);

                // 未解锁显示白色，解锁显示实际颜色
                Color displayColor = unlocked ? slotColors[i] : lockedColor;

                if (i == currentSelection && unlocked)
                {
                    colorSlots[i].transform.localScale = Vector3.one * selectedScale;
                    displayColor.a = 1f;
                }
                else
                {
                    colorSlots[i].transform.localScale = Vector3.one * normalScale;
                    displayColor.a = unlocked ? 0.5f : 0.3f;
                }

                colorSlots[i].color = displayColor;

                // 边框：只有选中且解锁的槽位显示
                if (slotOutlines[i] != null)
                    slotOutlines[i].enabled = (i == currentSelection && unlocked);
            }
        }
    }
}