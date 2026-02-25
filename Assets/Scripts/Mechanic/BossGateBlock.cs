using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private float colorTransitionDuration = 2f;

    [Header("Burn Damage")]
    [SerializeField] private float damagePerSecond = 10f;

    private SpriteRenderer sr;
    private List<Health> touching = new List<Health>();
    private bool burning = false;

    private enum GateState { SmallBlack, Growing, LargeBlack, TurningRed, LargeRed, Draining }
    private GateState state = GateState.SmallBlack;
    private Coroutine activeCoroutine;

    public bool CanDrain => state == GateState.LargeRed;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        transform.localScale = smallScale;
        sr.color = blackColor;
        activeCoroutine = StartCoroutine(GrowRoutine());
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

    // Boss死后调用
    public void OnBossDefeated()
    {
        if (state != GateState.LargeBlack) return;
        activeCoroutine = StartCoroutine(TurnRedRoutine());
    }

    public void OnDrain()
    {
        if (!CanDrain) return;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);

        burning = false;
        touching.Clear();
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

    private IEnumerator TurnRedRoutine()
    {
        state = GateState.TurningRed;
        Color startColor = sr.color;
        float time = 0f;
        while (time < colorTransitionDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / colorTransitionDuration));
            sr.color = Color.Lerp(startColor, redColor, t);
            yield return null;
        }
        sr.color = redColor;
        state = GateState.LargeRed;
        burning = true;
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