using System.Collections;
using UnityEngine;

public class HurtFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private int flashCount = 3;

    private Color[] baseColors;
    private Coroutine flashRoutine;
    private bool initialized = false;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        baseColors = new Color[renderers.Length];
        SaveBaseColors();
        initialized = true;
    }

    private void SaveBaseColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                baseColors[i] = renderers[i].color;
            }
        }
    }

    public void PlayFlash()
    {
        if (!initialized) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreBaseColor();
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void RefreshBaseColors()
    {
        SaveBaseColors();
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            SetColor(flashColor);
            yield return new WaitForSeconds(flashInterval);

            RestoreBaseColor();
            yield return new WaitForSeconds(flashInterval);
        }

        RestoreBaseColor();
        flashRoutine = null;
    }

    private void SetColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = color;
            }
        }
    }

    private void RestoreBaseColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = baseColors[i];
            }
        }
    }
}