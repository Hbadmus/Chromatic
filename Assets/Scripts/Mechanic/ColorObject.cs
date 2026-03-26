using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Chromatic.Combat;

namespace Chromatic.Environment
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(LineRenderer))]
    public class ColorObject : MonoBehaviour, IInteractiveTarget, IDrainable
    {
        private enum ObjectState
        {
            Neutral,
            BlackGravity,
            RedGrowth,
            GreenSplit,
            BlueTeleport
        }

        private struct ColorSnapshot
        {
            public ObjectState state;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Color color;
        }

        private static ColorObject firstBlueObject = null;

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

        [Header("Green (Split & Bounce)")]
        [SerializeField] private Color greenColor = Color.green;
        [SerializeField] private int splitCount = 3;         
        [SerializeField] private float splitSpacing = 2.5f;  
        [SerializeField] private float greenJumpForce = 15f; 
        
        private List<GameObject> greenClones = new List<GameObject>(); 
        [HideInInspector] public ColorObject masterGreenObject;        
        [HideInInspector] public bool isGreenClone = false;            

        [Header("Blue (Teleport)")]
        [SerializeField] private Color blueColor = Color.blue;
        [SerializeField] private float lineScrollSpeed = 2f; 

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
            
            if (!isGreenClone)
            {
                originalPosition = transform.position;
                originalRotation = transform.rotation;
                originalScale = transform.localScale;
                sr.color = initialColor;
            }
        }

        private void SetupLineRenderer()
        {
            lr.positionCount = 0;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = blueColor;
            lr.endColor = new Color(blueColor.r, blueColor.g, blueColor.b, 0.2f);
        }

        private void Update()
        {
            if (currentState == ObjectState.RedGrowth && isReacting)
            {
                touchingEntities.RemoveAll(h => h == null);
                float dmg = redDamagePerSecond * Time.deltaTime;
                foreach (Health h in touchingEntities) h.TakeDamage(dmg);
            }

            if (firstBlueObject == this && lr.positionCount == 2)
            {
                float offset = Time.time * lineScrollSpeed;
                lr.material.mainTextureOffset = new Vector2(-offset, 0);
            }
        }

        // =====================================================
        // 蓝色传送逻辑
        // =====================================================
        private void HandleBlueTeleport()
        {
            if (!isReacting) return;

            if (firstBlueObject == null)
            {
                firstBlueObject = this;
            }
            else if (firstBlueObject == this)
            {
                return;
            }
            else
            {
                PerformTeleport(firstBlueObject, this);
            }
        }

        private void PerformTeleport(ColorObject fromObj, ColorObject toObj)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                fromObj.DrawLink(toObj.transform.position);
                player.transform.position = toObj.transform.position + Vector3.up * 0.5f;
                fromObj.StartCoroutine(fromObj.ClearLinkAfterDelay(3f));
            }
            firstBlueObject = null;
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

        private IEnumerator ClearLinkAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearLink();
        }

        // =====================================================
        // 绿色分裂与跳跃逻辑
        // =====================================================
        private void HandleGreenSplit()
        {
            if (isGreenClone || splitCount <= 1) return;

            ClearGreenClones();

            int half = splitCount / 2;
            int currentSpawned = 1; 

            for (int i = 1; i <= half; i++)
            {
                if (currentSpawned < splitCount)
                {
                    CreateGreenClone(transform.position + Vector3.right * splitSpacing * i);
                    currentSpawned++;
                }
                
                if (currentSpawned < splitCount)
                {
                    CreateGreenClone(transform.position + Vector3.left * splitSpacing * i);
                    currentSpawned++;
                }
            }
        }

        private void CreateGreenClone(Vector3 pos)
        {
            GameObject clone = Instantiate(gameObject, pos, transform.rotation);
            ColorObject co = clone.GetComponent<ColorObject>();
            
            co.isGreenClone = true;
            co.masterGreenObject = this;
            co.currentState = ObjectState.GreenSplit;
            co.isReacting = true;
            co.hitNumber = this.maxHitNumber;
            
            MonoBehaviour[] scripts = clone.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script != null && script != co)
                {
                    Destroy(script); 
                }
            }
            
            co.StopAllCoroutines();
            co.GetComponent<SpriteRenderer>().color = greenColor;

            greenClones.Add(clone);
        }

        private void ClearGreenClones()
        {
            foreach (var clone in greenClones)
            {
                if (clone != null) Destroy(clone);
            }
            greenClones.Clear();
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (currentState == ObjectState.GreenSplit && isReacting)
            {
                if (collision.gameObject.CompareTag("Player"))
                {
                    Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                    if (playerRb != null && playerRb.linearVelocity.y > 0.1f)
                    {
                        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, greenJumpForce);
                    }
                }
            }
        }

        // =====================================================
        // 接口与状态处理
        // =====================================================
        public void OnHit(float damage, Color bulletColor)
        {
            if (isDraining) return;
            if (isGreenClone) return; 

            ObjectState newState = ObjectState.Neutral;

            if (IsColorSimilar(bulletColor, blackColor)) newState = ObjectState.BlackGravity;
            else if (IsColorSimilar(bulletColor, redColor)) newState = ObjectState.RedGrowth;
            else if (IsColorSimilar(bulletColor, greenColor)) newState = ObjectState.GreenSplit;
            else if (IsColorSimilar(bulletColor, blueColor)) newState = ObjectState.BlueTeleport;
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
                if (currentState == ObjectState.BlueTeleport && firstBlueObject == this) firstBlueObject = null;
                if (currentState == ObjectState.GreenSplit) ClearGreenClones();

                hitNumber = 0;
                isReacting = false;
                currentState = newState;

                // 强制切断旧的物理影响：只要切入的新状态不是黑色，立即冻结重力影响
                if (currentState != ObjectState.BlackGravity)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }

            UpdateProgress();

            if (hitNumber >= maxHitNumber)
            {
                ActivateFinalState();
            }
        }

        public void OnDrain()
        {
            if (isGreenClone)
            {
                if (masterGreenObject != null) masterGreenObject.OnDrain();
                return;
            }

            if (!CanDrain || isDraining) return;
            
            isDraining = true;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);

            if (!isReacting && hitNumber > 0)
            {
                // 仅吸取进度时，不破坏栈内数据
                ColorSnapshot? peekTarget = colorStack.Count > 0 ? colorStack.Peek() : (ColorSnapshot?)null;
                activeCoroutine = StartCoroutine(DrainCurrentProgress(peekTarget));
                return;
            }

            ObjectState drainState = currentState;
            ColorSnapshot? target = colorStack.Count > 0 ? colorStack.Pop() : (ColorSnapshot?)null;

            activeCoroutine = StartCoroutine(DrainLayer(drainState, target));
        }

        private IEnumerator DrainLayer(ObjectState drainState, ColorSnapshot? target)
        {
            ClearLink();
            if (drainState == ObjectState.BlueTeleport && firstBlueObject == this) firstBlueObject = null;
            if (drainState == ObjectState.GreenSplit) ClearGreenClones();

            switch (drainState)
            {
                case ObjectState.BlackGravity: yield return StartCoroutine(DrainBlack(target)); break;
                case ObjectState.RedGrowth: yield return StartCoroutine(DrainRed(target)); break;
                case ObjectState.GreenSplit: yield return StartCoroutine(DrainGreen(target)); break;
                case ObjectState.BlueTeleport: yield return StartCoroutine(DrainBlue(target)); break;
            }
        }

        private IEnumerator DrainBlue(ColorSnapshot? target)
        {
            // 移除了重复调用 RestoreToTarget，交由 DrainCurrentProgress 统一收尾
            yield return StartCoroutine(DrainCurrentProgress(target));
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

        private IEnumerator DrainGreen(ColorSnapshot? target)
        {
            yield return StartCoroutine(DrainCurrentProgress(target));
        }

        private IEnumerator DrainCurrentProgress(ColorSnapshot? targetToUse)
        {
            if (currentState == ObjectState.RedGrowth) touchingEntities.Clear();
            if (currentState == ObjectState.BlueTeleport && firstBlueObject == this) firstBlueObject = null;
            if (currentState == ObjectState.GreenSplit) ClearGreenClones();
            ClearLink();

            Color startColor = sr.color;
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            Color endColor = targetToUse.HasValue ? targetToUse.Value.color : initialColor;
            Vector3 endScale = targetToUse.HasValue ? targetToUse.Value.scale : originalScale;
            Vector3 endPos = targetToUse.HasValue ? targetToUse.Value.position : originalPosition;
            Quaternion endRot = targetToUse.HasValue ? targetToUse.Value.rotation : originalRotation;

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
            
            RestoreToTarget(targetToUse);
        }

        private void RestoreToTarget(ColorSnapshot? target)
        {
            if (target.HasValue)
            {
                currentState = target.Value.state;
                isReacting = true;
                hitNumber = maxHitNumber;
                
                // 修复：退回之前的状态时，必须精确恢复该状态的物理属性
                if (currentState == ObjectState.BlackGravity)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = 1f;
                    rb.mass = 100000f;
                }
                else
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                if (currentState == ObjectState.RedGrowth) RefreshTouchingEntities();
                if (currentState == ObjectState.GreenSplit) HandleGreenSplit(); 
            }
            else
            {
                currentState = ObjectState.Neutral;
                isReacting = false;
                hitNumber = 0;
                sr.color = initialColor;
                
                // 彻底清空遗留的物理属性
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.mass = 1f;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
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
                case ObjectState.GreenSplit: sr.color = Color.Lerp(baseColor, greenColor, t); break;
                case ObjectState.BlueTeleport: sr.color = Color.Lerp(baseColor, blueColor, t); break;
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
                case ObjectState.GreenSplit:
                    sr.color = greenColor;
                    HandleGreenSplit();
                    break;
                case ObjectState.BlueTeleport:
                    sr.color = blueColor;
                    HandleBlueTeleport();
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
            if (firstBlueObject == this) firstBlueObject = null;
        }
    }
}