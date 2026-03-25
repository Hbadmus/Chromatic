using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorTransition : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 3f;
    [SerializeField] private float damagePerSecond = 1f;

    private SpriteRenderer spriteRend;
    private Renderer rend;
    private Coroutine transitionCoroutine;

    private bool burning = false;
    private List<Health> touching = new List<Health>();

    private void Awake()
    {
        spriteRend = GetComponent<SpriteRenderer>();
        rend = GetComponent<Renderer>();

        if (spriteRend == null && rend == null)
        {
            Debug.LogWarning($"{name}: ColorTransition requires SpriteRenderer or Renderer.");
        }
    }

    private void Update()
    {
        if (!burning) return;
        touching.RemoveAll(h => h == null);
        float dmg = damagePerSecond * Time.deltaTime;
        foreach (Health h in touching)
        {
            h.TakeDamage(dmg);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!burning) return;
        Health h = other.GetComponent<Health>();
        if (h != null && !touching.Contains(h))
            touching.Add(h);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Health h = other.GetComponent<Health>();
        if (h != null)
            touching.Remove(h);
    }

    public void StartTransition(Color targetColor)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(FadeToColor(targetColor));
    }

    public void StopBurning()
    {
        burning = false;
        touching.Clear();
    }

    private void SetColor(Color color)
    {
        if (spriteRend != null)
        {
            spriteRend.color = color;
            return;
        }

        if (rend != null)
        {
            rend.material.color = color;
        }
    }

    private Color GetCurrentColor()
    {
        if (spriteRend != null)
            return spriteRend.color;

        if (rend != null)
            return rend.material.color;

        return Color.white;
    }

    private IEnumerator FadeToColor(Color targetColor)
    {
        Color startColor = GetCurrentColor();
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            SetColor(Color.Lerp(startColor, targetColor, t));
            yield return null;
        }

        SetColor(targetColor);

        if (IsRed(targetColor))
        {
            burning = true;
        }
        else
        {
            burning = false;
            touching.Clear();
        }

        transitionCoroutine = null;
    }

    private bool IsRed(Color c)
    {
        return Mathf.Abs(c.r - 1f) < 0.1f && c.g < 0.1f && c.b < 0.1f;
    }
}