using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(clickClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(hoverClip);
    }
}
