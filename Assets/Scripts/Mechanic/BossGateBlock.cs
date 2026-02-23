using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class BossGateBlock : MonoBehaviour, IDrainable
{
    [Header("Size")]
    [SerializeField] private Vector3 smallScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 largeScale = new Vector3(4f, 4f, 1f);
    [SerializeField] private float growDuration = 2f;
    [SerializeField] private float shrinkDuration = 2f;

    [Header("Colors")]
    [SerializeField] private Color blackColor = Color.black;

    private SpriteRenderer sr;
    private ColorTransition colorTransition;

    private enum GateState { SmallBlack, Growing, LargeBlack, LargeRed, Draining }
    private GateState state = GateState.SmallBlack;
    private Coroutine activeCoroutine;

    public bool CanDrain => state == GateState.LargeRed;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        colorTransition = GetComponent<ColorTransition>();
    }

    private void Start()
    {
        transform.localScale = smallScale;
        sr.color = blackColor;
        activeCoroutine = StartCoroutine(GrowRoutine());
    }

    public void OnBecameRed()
    {
        state = GateState.LargeRed;
    }

    public void OnDrain()
    {
        if (!CanDrain) return;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);

        // 让 ColorTransition 停止烧血
        if (colorTransition != null) colorTransition.StopBurning();

        state = GateState.Draining;
        activeCoroutine = StartCoroutine(ShrinkRoutine());
    }

    private IEnumerator GrowRoutine()
    {
        state = GateState.Growing;
        float time = 0f;
        while (time < growDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / growDuration));
            transform.localScale = Vector3.Lerp(smallScale, largeScale, t);
            yield return null;
        }
        transform.localScale = largeScale;
        state = GateState.LargeBlack;
        activeCoroutine = null;
    }

    private IEnumerator ShrinkRoutine()
    {
        Vector3 startScale = transform.localScale;
        Color startColor = sr.color;
        float time = 0f;
        while (time < shrinkDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / shrinkDuration));
            transform.localScale = Vector3.Lerp(startScale, smallScale, t);
            sr.color = Color.Lerp(startColor, blackColor, t);
            yield return null;
        }
        transform.localScale = smallScale;
        sr.color = blackColor;
        state = GateState.SmallBlack;
        activeCoroutine = null;
    }
}