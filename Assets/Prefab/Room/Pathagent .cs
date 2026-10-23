using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pengganti NavMeshAgent untuk 2D. Menyediakan API yang mirip (SetDestination, isStopped,
/// velocity, remainingDistance, stoppingDistance, pathPending) supaya Movement.cs
/// hampir tidak perlu diubah. Path dihitung ulang paling cepat tiap repathInterval detik.
/// Menggerakkan lewat transform; kalau enemy punya Rigidbody2D, set ke Kinematic.
/// </summary>
public class PathAgent : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float _stoppingDistance = 0.1f;
    [Tooltip("Jeda minimum antar perhitungan ulang path (hemat CPU saat Chase).")]
    [SerializeField] private float repathInterval = 0.2f;

    public float Speed { get => speed; set => speed = value; }
    public float stoppingDistance => _stoppingDistance;
    public bool isStopped { get; set; }
    public Vector2 velocity { get; private set; }
    public bool pathPending => pending;

    public float remainingDistance
    {
        get
        {
            if (pending) return Mathf.Infinity;
            if (path.Count == 0 || index >= path.Count) return 0f;

            Vector2 pos = transform.position;
            float d = Vector2.Distance(pos, path[index]);
            for (int i = index + 1; i < path.Count; i++)
                d += Vector2.Distance(path[i - 1], path[i]);
            return d;
        }
    }

    private readonly List<Vector2> path = new List<Vector2>();
    private int index;
    private bool pending;
    private Vector2 requestedDestination;
    private bool hasDestination;
    private float nextRepathTime;

    public bool SetDestination(Vector2 destination)
    {
        // Tujuan hampir sama dengan sebelumnya -> tidak perlu hitung ulang
        if (hasDestination && (destination - requestedDestination).sqrMagnitude < 0.0025f)
            return true;

        requestedDestination = destination;
        hasDestination = true;
        pending = true;

        if (Time.time >= nextRepathTime)
            Recompute();

        return true;
    }

    public void ResetPath()
    {
        path.Clear();
        index = 0;
        pending = false;
        hasDestination = false;
        velocity = Vector2.zero;
    }

    private void Recompute()
    {
        pending = false;
        nextRepathTime = Time.time + repathInterval;
        index = 0;

        if (!GridPathfinder.FindPath(transform.position, requestedDestination, path))
            path.Clear();
    }

    private void Update()
    {
        if (pending && Time.time >= nextRepathTime)
            Recompute();

        velocity = Vector2.zero;

        if (isStopped || path.Count == 0 || index >= path.Count) return;

        Vector2 pos = transform.position;
        Vector2 toPoint = path[index] - pos;
        float step = speed * Time.deltaTime;
        float dist = toPoint.magnitude;

        if (dist <= step)
        {
            transform.position = new Vector3(path[index].x, path[index].y, transform.position.z);
            index++;
            if (index >= path.Count) path.Clear(); // sampai tujuan
            velocity = dist > 0.0001f ? toPoint / dist * speed : Vector2.zero;
        }
        else
        {
            Vector2 dir = toPoint / dist;
            transform.position += (Vector3)(dir * step);
            velocity = dir * speed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (path == null || path.Count == 0) return;
        Gizmos.color = Color.red;
        Vector3 prev = transform.position;
        for (int i = index; i < path.Count; i++)
        {
            Gizmos.DrawLine(prev, path[i]);
            prev = path[i];
        }
    }
}