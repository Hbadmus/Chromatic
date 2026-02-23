using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorTransition : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 3f;
    [SerializeField] private float damagePerSecond = 1f;

    private Renderer rend;
    private Color startColor;
    private BossGateBlock gateBlock;

    private bool burning = false;
    private List<Health> touching = new List<Health>();

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        startColor = rend.material.color;
        gateBlock = GetComponent<BossGateBlock>();
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
        StartCoroutine(FadeToColor(targetColor));
    }

    public void StopBurning()
    {
        burning = false;
        touching.Clear();
    }

    private IEnumerator FadeToColor(Color targetColor)
    {
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            rend.material.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        rend.material.color = targetColor;

        if (IsRed(targetColor))
        {
            burning = true;
            if (gateBlock != null) gateBlock.OnBecameRed();
        }
    }

    private bool IsRed(Color c)
    {
        return Mathf.Abs(c.r - 1f) < 0.1f && c.g < 0.1f && c.b < 0.1f;
    }
}