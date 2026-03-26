using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }
    private RespawnPoint currentRespawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NotifyPointTouched(RespawnPoint point)
    {
        if (point == null) return;

        if (!point.IsActive)
        {
            point.SetActiveState(true);
            SetCurrentRespawnPoint(point);
        }
    }

    public void NotifyPointSetRequested(RespawnPoint point)
    {
        if (point == null) return;
        if (!point.IsActive) return;

        SetCurrentRespawnPoint(point);
    }


    public void RespawnPlayer(GameObject player, Vector3 deathPosition)
    {
        if (player == null) return;

        if (currentRespawnPoint != null)
        {
            Teleport(player, currentRespawnPoint.SpawnPosition);
            return;
        }
    }

    private void Teleport(GameObject player, Vector3 targetPosition)
    {
        player.transform.position = targetPosition;

        var rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
    }

    private void SetCurrentRespawnPoint(RespawnPoint point)
    {
        if (point == null) return;

        if (currentRespawnPoint != null)
        {
            currentRespawnPoint.SetRespawnSelected(false);
        }

        currentRespawnPoint = point;
        currentRespawnPoint.SetRespawnSelected(true);
    }
}
