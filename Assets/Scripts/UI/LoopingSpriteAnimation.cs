using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LoopingSpriteAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1)] private float fps = 8f;

    private SpriteRenderer sr;
    private Coroutine loop;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    private void OnEnable() => Play();

    private void OnDisable() => Stop();

    public void Play()
    {
        Stop();
        if (frames == null || frames.Length == 0) return;
        loop = StartCoroutine(Animate());
    }

    public void Stop()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private IEnumerator Animate()
    {
        float interval = 1f / fps;
        int i = 0;
        while (true)
        {
            sr.sprite = frames[i % frames.Length];
            i++;
            yield return new WaitForSeconds(interval);
        }
    }
}
