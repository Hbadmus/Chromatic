using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Chromatic.Combat;

namespace Chromatic.Environment
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(LineRenderer))] // 自动添加连线组件
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

        private struct ColorSnapshot
        {
            public ObjectState state;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Color color;
        }

        // =====================================================
        // 静态/全局管理 (仅限玩家激活的绿色物体)
        // =====================================================
        private static ColorObject firstGreenObject = null;

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

        [Header("Green (Teleport)")]
        [SerializeField] private Color greenColor = Color.green;
        [SerializeField] private float lineScrollSpeed = 2f; // 线条动画速度

        [Header("Blue (Platform)")]
        [SerializeField] private Color blueColor = Color.blue;

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;

        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private LineRenderer lr;

        private ObjectState currentState = ObjectState.Neutral;
        private bool isReacting = false;
        private int hitNumber = 0;
        private Coroutine activeCoroutine;
        private bool isDraining = false; 

        private Stack<ColorSnapshot> colorStack = new Stack<ColorSnapshot>();

        public bool CanDrain => colorStack.Count > 0 || hitNumber > 0;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            lr = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        private void Start()
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
            sr.color = initialColor;
        }

        private void SetupLineRenderer()
        {
            lr.positionCount = 0;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = greenColor;
            lr.endColor = new Color(greenColor.r, greenColor.g, greenColor.b, 0.2f);
        }

        private void Update()
        {
            // 红色伤害逻辑
            if (currentState == ObjectState.RedGrowth && isReacting)
            {
                touchingEntities.RemoveAll(h => h == null);
                float dmg = redDamagePerSecond * Time.deltaTime;
                foreach (Health h in touchingEntities) h.TakeDamage(dmg);
            }

            // 绿色连线特效更新
            if (firstGreenObject == this && lr.positionCount == 2)
            {
                float offset = Time.time * lineScrollSpeed;
                lr.material.mainTextureOffset = new Vector2(-offset, 0);
            }
        }

        // =====================================================
        // 核心：绿色传送逻辑
        // =====================================================
        private void HandleGreenTeleport()
        {
            // 只有当玩家射击导致的 isReacting 为 true 时才触发
            if (!isReacting) return;

            if (firstGreenObject == null)
            {
                firstGreenObject = this;
                Debug.Log("第一个绿色节点已激活，等待连接...");
            }
            else if (firstGreenObject == this)
            {
                return;
            }
            else
            {
                // 执行传送
                PerformTeleport(firstGreenObject, this);
                
                // 传送后自动清除两个物体的绿色状态
                firstGreenObject.OnDrain();
                this.OnDrain();
            }
        }

        private void PerformTeleport(ColorObject fromObj, ColorObject toObj)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // 绘制最后的连线特效
                fromObj.DrawLink(toObj.transform.position);
                
                // 实际传送 (增加一点Y轴偏移防止卡地)
                player.transform.position = toObj.transform.position + Vector3.up * 0.5f;
                Debug.Log("已传送到目标物体。");
            }
            firstGreenObject = null;
        }

        public void DrawLink(Vector3 targetPos)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, targetPos);
        }

        public void ClearLink()
        {
            lr.positionCount = 0;
        }

        // =====================================================
        // 接口与状态处理
        // =====================================================
        public void OnHit(float damage, Color bulletColor)
        {
            if (isDraining) return;

            ObjectState newState = ObjectState.Neutral;

            if (IsColorSimilar(bulletColor, blackColor)) newState = ObjectState.BlackGravity;
            else if (IsColorSimilar(bulletColor, redColor)) newState = ObjectState.RedGrowth;
            else if (IsColorSimilar(bulletColor, greenColor)) newState = ObjectState.GreenFloat;
            else if (IsColorSimilar(bulletColor, blueColor)) newState = ObjectState.BlueFreeze;
            else return;

            if (currentState == newState && isReacting) return;

            if (currentState != newState)
            {
                if (activeCoroutine != null) StopCoroutine(activeCoroutine);

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
                else if (!isReacting && hitNumber > 0)
                {
                    sr.color = GetBaseColor();
                    transform.localScale = GetBaseScale();
                }

                if (currentState == ObjectState.RedGrowth) touchingEntities.Clear();
                if (currentState == ObjectState.GreenFloat && firstGreenObject == this) firstGreenObject = null;

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

        public void OnDrain()
        {
            if (!CanDrain || isDraining) return;
            
            isDraining = true;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);

            if (!isReacting && hitNumber > 0)
            {
                activeCoroutine = StartCoroutine(DrainCurrentProgress());
                return;
            }

            ObjectState drainState = currentState;
            ColorSnapshot? target = colorStack.Count > 0 ? colorStack.Pop() : (ColorSnapshot?)null;

            activeCoroutine = StartCoroutine(DrainLayer(drainState, target));
        }

        private IEnumerator DrainLayer(ObjectState drainState, ColorSnapshot? target)
        {
            ClearLink();
            if (drainState == ObjectState.GreenFloat && firstGreenObject == this) firstGreenObject = null;

            switch (drainState)
            {
                case ObjectState.BlackGravity: yield return StartCoroutine(DrainBlack(target)); break;
                case ObjectState.RedGrowth: yield return StartCoroutine(DrainRed(target)); break;
                case ObjectState.GreenFloat: yield return StartCoroutine(DrainGreen(target)); break;
                case ObjectState.BlueFreeze: yield return StartCoroutine(DrainBlue(target)); break;
            }
        }

        private IEnumerator DrainGreen(ColorSnapshot? target)
        {
            // 绿色状态下，平滑恢复到基础状态
            yield return StartCoroutine(DrainCurrentProgress());
            RestoreToTarget(target);
        }

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

        private IEnumerator DrainBlue(ColorSnapshot? target)
        {
            yield return StartCoroutine(DrainCurrentProgress());
            RestoreToTarget(target);
        }

        private IEnumerator DrainCurrentProgress()
        {
            if (currentState == ObjectState.RedGrowth) touchingEntities.Clear();
            if (currentState == ObjectState.GreenFloat && firstGreenObject == this) firstGreenObject = null;
            ClearLink();

            ColorSnapshot? target = colorStack.Count > 0 ? colorStack.Pop() : (ColorSnapshot?)null;

            Color startColor = sr.color;
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            Color endColor = target.HasValue ? target.Value.color : initialColor;
            Vector3 endScale = target.HasValue ? target.Value.scale : originalScale;
            Vector3 endPos = target.HasValue ? target.Value.position : originalPosition;
            Quaternion endRot = target.HasValue ? target.Value.rotation : originalRotation;

            float time = 0f;
            while (time < returnDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / returnDuration));
                sr.color = Color.Lerp(startColor, endColor, t);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            sr.color = endColor;
            transform.localScale = endScale;
            transform.position = endPos;
            transform.rotation = endRot;
            RestoreToTarget(target);
        }

        private void RestoreToTarget(ColorSnapshot? target)
        {
            if (target.HasValue)
            {
                currentState = target.Value.state;
                isReacting = true;
                hitNumber = maxHitNumber;
                if (currentState == ObjectState.RedGrowth) RefreshTouchingEntities();
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
            isDraining = false;
        }

        private void UpdateProgress()
        {
            hitNumber++;
            float t = Mathf.Clamp01((float)hitNumber / maxHitNumber);

            Color baseColor = GetBaseColor();
            Vector3 baseScale = GetBaseScale();

            switch (currentState)
            {
                case ObjectState.BlackGravity: sr.color = Color.Lerp(baseColor, blackColor, t); break;
                case ObjectState.RedGrowth:
                    sr.color = Color.Lerp(baseColor, redColor, t);
                    transform.localScale = Vector3.Lerp(baseScale, targetScale, t);
                    break;
                case ObjectState.GreenFloat: sr.color = Color.Lerp(baseColor, greenColor, t); break;
                case ObjectState.BlueFreeze: sr.color = Color.Lerp(baseColor, blueColor, t); break;
            }
        }

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
                    RefreshTouchingEntities();
                    break;
                case ObjectState.GreenFloat:
                    sr.color = greenColor;
                    HandleGreenTeleport();
                    break;
                case ObjectState.BlueFreeze:
                    sr.color = blueColor;
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (currentState != ObjectState.RedGrowth || !isReacting) return;
            Health h = other.GetComponent<Health>();
            if (h != null && !touchingEntities.Contains(h)) touchingEntities.Add(h);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Health h = other.GetComponent<Health>();
            if (h != null) touchingEntities.Remove(h);
        }

        private void RefreshTouchingEntities()
        {
            touchingEntities.Clear();
            Collider2D trigger = null;
            foreach (Collider2D col in GetComponents<Collider2D>()) { if (col.isTrigger) { trigger = col; break; } }
            if (trigger == null) return;

            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            List<Collider2D> results = new List<Collider2D>();
            trigger.Overlap(filter, results);

            foreach (Collider2D col in results)
            {
                Health h = col.GetComponent<Health>();
                if (h != null && !touchingEntities.Contains(h)) touchingEntities.Add(h);
            }
        }

        private Color GetBaseColor() => colorStack.Count > 0 ? colorStack.Peek().color : initialColor;
        private Vector3 GetBaseScale() => colorStack.Count > 0 ? colorStack.Peek().scale : originalScale;

        private bool IsColorSimilar(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.1f && Mathf.Abs(a.g - b.g) < 0.1f && Mathf.Abs(a.b - b.b) < 0.1f;
        }

        private void OnDestroy()
        {
            // 防止物体销毁时静态引用悬空
            if (firstGreenObject == this) firstGreenObject = null;
        }
    }
}