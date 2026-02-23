using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Chromatic.Combat;

namespace Chromatic.Environment
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class ColorObject : MonoBehaviour, IInteractiveTarget, IDrainable
    {
        private enum ObjectState
        {
            Neutral,
            BlackGravity,
            RedGrowth,
            GreenFloat,
            BlueFreeze
        }

        // 记录每一层颜色激活时的快照
        private struct ColorSnapshot
        {
            public ObjectState state;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Color color;
        }

        [Header("Common Settings")]
        [SerializeField] private int maxHitNumber = 3;
        [SerializeField] private float returnDuration = 3f;
        [SerializeField] private Color initialColor = Color.white;

        [Header("Black (Gravity)")]
        [SerializeField] private Color blackColor = Color.black;

        [Header("Red (Growth)")]
        [SerializeField] private Color redColor = Color.red;
        [SerializeField] private Vector3 targetScale = new Vector3(2f, 2f, 1f);
        [SerializeField] private float redDamagePerSecond = 10f;

        private List<Health> touchingEntities = new List<Health>();

        [Header("Green (Float)")]
        [SerializeField] private Color greenColor = Color.green;

        [Header("Blue (Platform)")]
        [SerializeField] private Color blueColor = Color.blue;

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;

        private SpriteRenderer sr;
        private Rigidbody2D rb;

        private ObjectState currentState = ObjectState.Neutral;
        private bool isReacting = false;
        private int hitNumber = 0;
        private Coroutine activeCoroutine;

        // 颜色历史栈
        private Stack<ColorSnapshot> colorStack = new Stack<ColorSnapshot>();

        public bool CanDrain => colorStack.Count > 0 || hitNumber > 0;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
            sr.color = initialColor;
        }

        // =====================================================
        // 红色伤害区域
        // =====================================================
        private void Update()
        {
            if (currentState != ObjectState.RedGrowth || !isReacting) return;

            // 清理已销毁的对象
            touchingEntities.RemoveAll(h => h == null);

            float dmg = redDamagePerSecond * Time.deltaTime;
            foreach (Health h in touchingEntities)
            {
                h.TakeDamage(dmg);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (currentState != ObjectState.RedGrowth || !isReacting) return;

            Health h = other.GetComponent<Health>();
            if (h != null && !touchingEntities.Contains(h))
            {
                touchingEntities.Add(h);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Health h = other.GetComponent<Health>();
            if (h != null)
            {
                touchingEntities.Remove(h);
            }
        }

        // =====================================================
        // 射击判定
        // =====================================================
        public void OnHit(float damage, Color bulletColor)
        {
            ObjectState newState = ObjectState.Neutral;

            if (IsColorSimilar(bulletColor, blackColor)) newState = ObjectState.BlackGravity;
            else if (IsColorSimilar(bulletColor, redColor)) newState = ObjectState.RedGrowth;
            else if (IsColorSimilar(bulletColor, greenColor)) newState = ObjectState.GreenFloat;
            else if (IsColorSimilar(bulletColor, blueColor)) newState = ObjectState.BlueFreeze;
            else return;

            // 同颜色且已激活，不重复处理
            if (currentState == newState && isReacting) return;

            // 切换到不同颜色时，保存当前状态到栈，重置进度
            if (currentState != newState)
            {
                if (activeCoroutine != null) StopCoroutine(activeCoroutine);

                // 如果当前有激活的颜色，存进栈
                if (currentState != ObjectState.Neutral && isReacting)
                {
                    colorStack.Push(new ColorSnapshot
                    {
                        state = currentState,
                        position = transform.position,
                        rotation = transform.rotation,
                        scale = transform.localScale,
                        color = sr.color
                    });
                }

                // 从红色切走时清空伤害列表
                if (currentState == ObjectState.RedGrowth)
                {
                    touchingEntities.Clear();
                }

                hitNumber = 0;
                isReacting = false;
                currentState = newState;
            }

            UpdateProgress();

            if (hitNumber >= maxHitNumber)
            {
                ActivateFinalState();
            }
        }

        // =====================================================
        // Drain：弹出最顶层颜色
        // =====================================================
        public void OnDrain()
        {
            if (!CanDrain) return;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);

            // 如果正在打但还没激活（中途换色），直接清掉当前进度
            if (!isReacting && hitNumber > 0)
            {
                activeCoroutine = StartCoroutine(DrainCurrentProgress());
                return;
            }

            // 激活状态下，根据当前颜色执行对应drain
            ObjectState drainState = currentState;

            // drain完成后要恢复到的目标
            ColorSnapshot? target = colorStack.Count > 0 ? colorStack.Pop() : (ColorSnapshot?)null;

            activeCoroutine = StartCoroutine(DrainLayer(drainState, target));
        }

        private IEnumerator DrainLayer(ObjectState drainState, ColorSnapshot? target)
        {
            switch (drainState)
            {
                case ObjectState.BlackGravity:
                    yield return StartCoroutine(DrainBlack(target));
                    break;
                case ObjectState.RedGrowth:
                    yield return StartCoroutine(DrainRed(target));
                    break;
                case ObjectState.GreenFloat:
                    yield return StartCoroutine(DrainGreen(target));
                    break;
                case ObjectState.BlueFreeze:
                    yield return StartCoroutine(DrainBlue(target));
                    break;
            }
        }

        // 黑色drain：恢复位置、旋转、颜色
        private IEnumerator DrainBlack(ColorSnapshot? target)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Color startColor = sr.color;

            Vector3 endPos = target.HasValue ? target.Value.position : originalPosition;
            Quaternion endRot = target.HasValue ? target.Value.rotation : originalRotation;
            Color endColor = target.HasValue ? target.Value.color : initialColor;

            float time = 0f;
            while (time < returnDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / returnDuration));
                transform.position = Vector3.Lerp(startPos, endPos, t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                sr.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            transform.position = endPos;
            transform.rotation = endRot;
            sr.color = endColor;
            RestoreToTarget(target);
        }

        // 红色drain：恢复大小和颜色，位置不动
        private IEnumerator DrainRed(ColorSnapshot? target)
        {
            Vector3 startScale = transform.localScale;
            Color startColor = sr.color;

            Vector3 endScale = target.HasValue ? target.Value.scale : originalScale;
            Color endColor = target.HasValue ? target.Value.color : initialColor;

            float time = 0f;
            while (time < returnDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / returnDuration));
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                sr.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            transform.localScale = endScale;
            sr.color = endColor;
            RestoreToTarget(target);
        }

        // 绿色drain：TODO
        private IEnumerator DrainGreen(ColorSnapshot? target)
        {
            Debug.Log("DrainGreen - TODO");
            RestoreToTarget(target);
            yield break;
        }

        // 蓝色drain：TODO
        private IEnumerator DrainBlue(ColorSnapshot? target)
        {
            Debug.Log("DrainBlue - TODO");
            RestoreToTarget(target);
            yield break;
        }

        // drain未激活的进度（打了1-2下还没满）
        private IEnumerator DrainCurrentProgress()
        {
            Color startColor = sr.color;
            Vector3 startScale = transform.localScale;

            Color endColor = colorStack.Count > 0 ? colorStack.Peek().color : initialColor;
            Vector3 endScale = colorStack.Count > 0 ? colorStack.Peek().scale : originalScale;

            float time = 0f;
            while (time < returnDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / returnDuration));
                sr.color = Color.Lerp(startColor, endColor, t);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            sr.color = endColor;
            transform.localScale = endScale;

            // 恢复到上一层状态
            if (colorStack.Count > 0)
            {
                ColorSnapshot prev = colorStack.Peek();
                currentState = prev.state;
                isReacting = true;
            }
            else
            {
                currentState = ObjectState.Neutral;
                isReacting = false;
            }
            hitNumber = 0;
            activeCoroutine = null;
        }

        // 恢复到目标层或完全重置
        private void RestoreToTarget(ColorSnapshot? target)
        {
            if (target.HasValue)
            {
                currentState = target.Value.state;
                isReacting = true;
                hitNumber = maxHitNumber;
            }
            else
            {
                currentState = ObjectState.Neutral;
                isReacting = false;
                hitNumber = 0;
                sr.color = initialColor;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            activeCoroutine = null;
        }

        // =====================================================
        // 渐变过程
        // =====================================================
        private void UpdateProgress()
        {
            hitNumber++;
            float t = Mathf.Clamp01((float)hitNumber / maxHitNumber);

            switch (currentState)
            {
                case ObjectState.BlackGravity:
                    sr.color = Color.Lerp(initialColor, blackColor, t);
                    break;
                case ObjectState.RedGrowth:
                    sr.color = Color.Lerp(initialColor, redColor, t);
                    transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                    break;
                case ObjectState.GreenFloat:
                    sr.color = Color.Lerp(initialColor, greenColor, t);
                    break;
                case ObjectState.BlueFreeze:
                    sr.color = Color.Lerp(initialColor, blueColor, t);
                    break;
            }
        }

        // =====================================================
        // 最终激活
        // =====================================================
        private void ActivateFinalState()
        {
            isReacting = true;

            switch (currentState)
            {
                case ObjectState.BlackGravity:
                    sr.color = blackColor;
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = 1f;
                    rb.mass = 100000f;
                    break;
                case ObjectState.RedGrowth:
                    sr.color = redColor;
                    transform.localScale = targetScale;
                    break;
                case ObjectState.GreenFloat:
                    sr.color = greenColor;
                    Debug.Log("变成绿色了！应用漂浮逻辑");
                    break;
                case ObjectState.BlueFreeze:
                    sr.color = blueColor;
                    Debug.Log("变成蓝色了！应用冰冻逻辑");
                    break;
            }
        }

        private void ResetProgress()
        {
            hitNumber = 0;
            isReacting = false;
            currentState = ObjectState.Neutral;
            sr.color = initialColor;
            activeCoroutine = null;
            colorStack.Clear();
            touchingEntities.Clear();
        }

        private bool IsColorSimilar(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.1f &&
                   Mathf.Abs(a.g - b.g) < 0.1f &&
                   Mathf.Abs(a.b - b.b) < 0.1f;
        }
    }
}