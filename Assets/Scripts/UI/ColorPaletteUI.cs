using UnityEngine;
using UnityEngine.UI;

namespace Chromatic.UI
{
    public class ColorPaletteUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image[] colorSlots; 
        
        [Header("Settings")]
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color normalColor = new Color(1, 1, 1, 0.5f);
        [SerializeField] private float normalScale = 0.3f;  
        [SerializeField] private float selectedScale = 0.4f;

        public void UpdateSelection(int index)
        {
            for (int i = 0; i < colorSlots.Length; i++)
            {
                if (colorSlots[i] == null) continue;

                if (i == index)
                {
                    colorSlots[i].transform.localScale = Vector3.one * selectedScale;
                    var c = colorSlots[i].color;
                    c.a = 1f; 
                    colorSlots[i].color = c;
                }
                else
                {
                    colorSlots[i].transform.localScale = Vector3.one * normalScale;
                    var c = colorSlots[i].color;
                    c.a = 0.3f;
                    colorSlots[i].color = c;
                }
            }
        }
    }
}