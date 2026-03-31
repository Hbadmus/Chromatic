using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Image))]
public class TutorialKeyImageAnimator : MonoBehaviour
{
    [SerializeField] private Image keyImage;
    [SerializeField] private Sprite keyPressedSprite;
    [SerializeField] private Sprite keyReleasedSprite;
    [SerializeField] private InputAction targetAction;

    private void Awake()
    {
        if (!keyImage)
        {
            keyImage = GetComponent<Image>();
        }

        if (targetAction != null)
        {
            targetAction.Enable();
        }
    }

    private void OnEnable()
    {
        SetReleasedState();
        if (targetAction != null)
        {
            targetAction.Enable();
        }
    }

    private void Update()
    {
        if (!keyImage || !keyPressedSprite || !keyReleasedSprite || targetAction == null)
        {
            return;
        }

        if (targetAction.IsPressed())
        {
            keyImage.sprite = keyPressedSprite;
        }
        else
        {
            keyImage.sprite = keyReleasedSprite;
        }
    }

    private void OnDisable()
    {
        SetReleasedState();
        if (targetAction != null)
        {
            targetAction.Disable();
        }
    }

    private void SetReleasedState()
    {
        if (keyImage && keyReleasedSprite)
        {
            keyImage.sprite = keyReleasedSprite;
        }
    }
}
