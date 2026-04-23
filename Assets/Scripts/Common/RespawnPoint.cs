using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class RespawnPoint : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool startsActive = false;
    [SerializeField] private Color activeColor = Color.white;

    [Header("SFX")]
    [SerializeField] private AudioClip activateClip;
    public bool IsActive { get; private set; }
    private bool isPlayerInRange;
    private SpriteRenderer sr;
    private ParticleSystem ps;
    public Vector3 SpawnPosition
    {
        get
        {
            return transform.position;
        }
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ps = GetComponent<ParticleSystem>();
        SetActiveState(startsActive);
    }

    private void OnDisable()
    {
        isPlayerInRange = false;

        if (ps) ps.Stop();
    }

    private void Update()
    {
        if (!isPlayerInRange) return;
        if (!IsActive) return;
        if (Keyboard.current == null) return;

        // Reactivate the point if the player is in range and presses the F key
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (RespawnManager.Instance != null)
                RespawnManager.Instance.NotifyPointSetRequested(this);
        }
    }


    public void SetActiveState(bool active)
    {
        IsActive = active;

        if (sr && active)
            sr.color = activeColor;

        if (ps && !active)
            ps.Stop();
    }

    public void SetRespawnSelected(bool selected)
    {
        if (ps)
        {
            if (selected && IsActive) ps.Play();
            else ps.Stop();
        }

        if (selected && IsActive && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(activateClip);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = true;

        if (RespawnManager.Instance != null)
            RespawnManager.Instance.NotifyPointTouched(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
    }
}
