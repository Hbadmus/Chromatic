using System.Collections.Generic;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }
    private readonly List<RespawnPoint> points = new();

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

    private void Start()
    {
        var existing = FindObjectsOfType<RespawnPoint>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            Register(existing[i]);
        }
    }

    public void Register(RespawnPoint point)
    {
        if (point == null) return;
        if (!points.Contains(point)) points.Add(point);
    }

    public void Unregister(RespawnPoint point)
    {
        if (point == null) return;
        points.Remove(point);
    }

    public RespawnPoint GetNearestActivePoint(Vector3 fromPosition)
    {
        RespawnPoint nearest = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p == null) continue;
            if (!p.IsActive) continue;

            float sqr = (p.SpawnPosition - fromPosition).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = p;
            }
        }

        return nearest;
    }

    public void RespawnPlayer(GameObject player, Vector3 deathPosition)
    {
        if (player == null) return;

        var point = GetNearestActivePoint(deathPosition);

        if (point != null)
        {
            Teleport(player, point.SpawnPosition);
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
}
