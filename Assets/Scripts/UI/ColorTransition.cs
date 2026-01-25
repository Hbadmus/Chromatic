using System.Collections;
using UnityEngine;

public class ColorTransition : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 3f;

    private Renderer rend;
    private Color startColor;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        startColor = rend.material.color; 
    }

    public void StartTransition()
    {
        StartCoroutine(FadeToRed());
    }

    private IEnumerator FadeToRed()
    {
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            rend.material.color = Color.Lerp(startColor, Color.red, t);

            yield return null;
        }

        rend.material.color = Color.red;
    }
}