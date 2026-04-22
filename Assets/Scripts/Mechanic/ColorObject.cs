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

        private static ColorObject pendingFirstBlue = null;
        
        [HideInInspector] public ColorObject linkedBlueObject = null;
        [HideInInspector] public bool isTeleportEntrance = false;

        [Header("Common Settings")]
        [SerializeField] private int maxHitNumber = 3;
        [SerializeField] private float returnDuration = 3f;
        [SerializeField] private Color initialColor = Color.white;

        [Header("Reset Flash Animation")]
        [SerializeField] private Color resetFlashColor = Color.white;
        [SerializeField] private float resetFlashDuration = 0.1f;
        [SerializeField] private float resetHideDelay = 0.12f;
        [SerializeField] private float resetFadeInDuration = 0.2f;

        [Header("Black (Gravity)")]
        [SerializeField] private Color blackColor = Color.black;

        [Header("Red (Growth)")]
        [SerializeField] private Color redColor = Color.red;
        [SerializeField] private Vector3 redScaleMultiplier = new Vector3(2f, 2f, 1f); 
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

        public bool IsGreenBounceActive => currentState == ObjectState.GreenSplit && isReacting;
        public float GreenBounceForce => greenJumpForce;

        [Header("Blue (Teleport)")]
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
        private bool isDraining = false; 

        private Stack<ColorSnapshot> colorStack = new Stack<ColorSnapshot>();

        public bool CanDrain => colorStack.Count > 0 || hitNumber > 0;

        public void ForceResetToInitialState()
        {
            if (isGreenClone)
            {
                if (masterGreenObject != null)
                {
                    masterGreenObject.ForceResetToInitialState();
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            // Immediately freeze physics and clear all active effects so the
            // platform stops mid-air while the flash animation plays.
            isDraining = false;
            isReacting = false;
            hitNumber = 0;
            currentState = ObjectState.Neutral;

            ClearBlueLinks();
            ClearGreenClones();
            touchingEntities.Clear();
            colorStack.Clear();

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.mass = 1f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            activeCoroutine = StartCoroutine(ResetFlashCoroutine());
        }

        private IEnumerator ResetFlashCoroutine()
        {
            Color savedColor = sr.color;

            // Single flash: white → original
            sr.color = resetFlashColor;
            yield return new WaitForSeconds(resetFlashDuration);
            sr.color = savedColor;
            yield return new WaitForSeconds(resetFlashDuration);

            // Brief invisible pause
            sr.color = new Color(savedColor.r, savedColor.g, savedColor.b, 0f);
            yield return new WaitForSeconds(resetHideDelay);

            // Snap to original position while still invisible, then fade in
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;

            float elapsed = 0f;
            while (elapsed < resetFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / resetFadeInDuration);
                sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, a);
                yield return null;
            }
            sr.color = initialColor;

            activeCoroutine = null;
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
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

        private void Update()
        {
            if (currentState == ObjectState.RedGrowth && isReacting)
            {
                touchingEntities.RemoveAll(h => h == null);
                float dmg = redDamagePerSecond * Time.deltaTime;
                foreach (Health h in touchingEntities) h.TakeDamage(dmg);
            }
        }

        private Vector3 GetRedTargetScale()
        {
            return new Vector3(
                originalScale.x * redScaleMultiplier.x,
                originalScale.y * redScaleMultiplier.y,
                originalScale.z * redScaleMultiplier.z
            );
        }

        private void HandleBlueTeleport()
        {
            if (!isReacting) return;

            if (pendingFirstBlue == null)
            {
                pendingFirstBlue = this;
                isTeleportEntrance = true;
            }
            else if (pendingFirstBlue != this)
            {
                this.isTeleportEntrance = false;
                
                this.linkedBlueObject = pendingFirstBlue;
                pendingFirstBlue.linkedBlueObject = this;
                
                pendingFirstBlue = null; 
            }
        }

        private void CheckTeleport(GameObject targetObj)
        {
            if (currentState == ObjectState.BlueTeleport && isReacting && linkedBlueObject != null && isTeleportEntrance)
            {
                if (targetObj.CompareTag("Player"))
                {
                    PerformTeleport(targetObj, linkedBlueObject);
                }
            }
        }

        private void PerformTeleport(GameObject player, ColorObject toObj)
        {
            Collider2D targetCol = toObj.GetComponent<Collider2D>();
            float targetY = toObj.transform.position.y;
            
            if (targetCol != null)
            {
                targetY = targetCol.bounds.max.y;
            }
            
            float yOffset = 0.5f; 
            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (playerCol != null)
            {
                yOffset = playerCol.bounds.extents.y + 0.05f; 
            }

            player.transform.position = new Vector3(toObj.transform.position.x, targetY + yOffset, toObj.transform.position.z);
        }

        private void ClearBlueLinks()
        {
            if (pendingFirstBlue == this) pendingFirstBlue = null;
            
            if (linkedBlueObject != null)
            {
                linkedBlueObject.linkedBlueObject = null;
                linkedBlueObject.isTeleportEntrance = false;
                linkedBlueObject = null;
            }
            
            isTeleportEntrance = false;
        }

        private void HandleGreenSplit()
        {
            if (isGreenClone || splitCount <= 1) return;

            ClearGreenClones();

            int half = splitCount / 2;
            int currentSpawned = 1; 

            float currentRatio = transform.localScale.x / originalScale.x;
            float dynamicSpacing = splitSpacing * currentRatio;

            for (int i = 1; i <= half; i++)
            {
                if (currentSpawned < splitCount)
                {
                    CreateGreenClone(transform.position + Vector3.right * dynamicSpacing * i);
                    currentSpawned++;
                }
                
                if (currentSpawned < splitCount)
                {
                    CreateGreenClone(transform.position + Vector3.left * dynamicSpacing * i);
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
            
            co.originalPosition = pos;
            co.originalRotation = transform.rotation;
            co.originalScale = transform.localScale; 
            co.initialColor = greenColor; 
            
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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckTeleport(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckTeleport(other.gameObject);

            if (currentState != ObjectState.RedGrowth || !isReacting) return;
            Health h = other.GetComponent<Health>();
            if (h != null && !touchingEntities.Contains(h)) touchingEntities.Add(h);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Health h = other.GetComponent<Health>();
            if (h != null) touchingEntities.Remove(h);
        }

        public void OnHit(float damage, Color bulletColor)
        {
            if (isDraining) return;

            ObjectState newState = ObjectState.Neutral;

            if (IsColorSimilar(bulletColor, blackColor)) newState = ObjectState.BlackGravity;
            else if (IsColorSimilar(bulletColor, redColor)) newState = ObjectState.RedGrowth;
            else if (IsColorSimilar(bulletColor, greenColor)) newState = ObjectState.GreenSplit;
            else if (IsColorSimilar(bulletColor, blueColor)) newState = ObjectState.BlueTeleport;
            else return;

            if (isGreenClone && newState == ObjectState.GreenSplit) return; 

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
                
                ClearBlueLinks();
                
                hitNumber = 0;
                isReacting = false;
                currentState = newState;

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
            if (isGreenClone && currentState == ObjectState.GreenSplit)
            {
                if (masterGreenObject != null) masterGreenObject.RecallFromClone();
                return;
            }

            if (!CanDrain || isDraining) return;
            
            if (currentState == ObjectState.BlueTeleport && linkedBlueObject != null)
            {
                ColorObject buddy = linkedBlueObject;
                
                linkedBlueObject = null;
                buddy.linkedBlueObject = null; 
                this.isTeleportEntrance = false;
                buddy.isTeleportEntrance = false;
                
                if (!buddy.isDraining)
                {
                    buddy.OnDrain();
                }
            }

            isDraining = true;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);

            if (!isReacting && hitNumber > 0)
            {
                ColorSnapshot? peekTarget = colorStack.Count > 0 ? colorStack.Peek() : (ColorSnapshot?)null;
                activeCoroutine = StartCoroutine(DrainCurrentProgress(peekTarget));
                return;
            }

            ObjectState drainState = currentState;
            ColorSnapshot? target = colorStack.Count > 0 ? colorStack.Pop() : (ColorSnapshot?)null;

            activeCoroutine = StartCoroutine(DrainLayer(drainState, target));
        }

        public void RecallFromClone()
        {
            if (isDraining) return;

            isDraining = true;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);

            ColorSnapshot[] stackArray = colorStack.ToArray();
            int deepestGreenIndex = -1;
            for (int i = 0; i < stackArray.Length; i++)
            {
                if (stackArray[i].state == ObjectState.GreenSplit)
                {
                    deepestGreenIndex = i; 
                }
            }

            if (deepestGreenIndex != -1)
            {
                for (int i = 0; i <= deepestGreenIndex; i++)
                {
                    colorStack.Pop();
                }
            }

            ColorSnapshot? target = colorStack.Count > 0 ? colorStack.Pop() : (ColorSnapshot?)null;
            ObjectState drainState = currentState;
            
            ClearGreenClones();

            activeCoroutine = StartCoroutine(DrainLayer(drainState, target));
        }

        private bool HasGreenStateRemaining(ColorSnapshot? target)
        {
            if (target.HasValue && target.Value.state == ObjectState.GreenSplit) return true;
            foreach (var snap in colorStack)
            {
                if (snap.state == ObjectState.GreenSplit) return true;
            }
            return false;
        }

        private IEnumerator DrainLayer(ObjectState drainState, ColorSnapshot? target)
        {
            ClearBlueLinks();
            
            if (drainState == ObjectState.GreenSplit && !HasGreenStateRemaining(target)) 
            {
                ClearGreenClones();
            }

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
            
            ClearBlueLinks();

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
                if (currentState == ObjectState.GreenSplit && greenClones.Count == 0) HandleGreenSplit(); 
            }
            else
            {
                if (isGreenClone)
                {
                    currentState = ObjectState.GreenSplit;
                    isReacting = true;
                    hitNumber = maxHitNumber;
                    sr.color = greenColor;
                    
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;
                    rb.mass = 1f;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                else
                {
                    currentState = ObjectState.Neutral;
                    isReacting = false;
                    hitNumber = 0;
                    sr.color = initialColor;
                    
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;
                    rb.mass = 1f;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
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
                    transform.localScale = Vector3.Lerp(baseScale, GetRedTargetScale(), t);
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
                    transform.localScale = GetRedTargetScale();
                    RefreshTouchingEntities();
                    break;
                case ObjectState.GreenSplit:
                    sr.color = greenColor;
                    if (greenClones.Count == 0) HandleGreenSplit();
                    break;
                case ObjectState.BlueTeleport:
                    sr.color = blueColor;
                    HandleBlueTeleport();
                    break;
            }
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
            ClearBlueLinks();
        }
    }
}