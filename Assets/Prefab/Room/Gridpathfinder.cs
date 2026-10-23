using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Grid A* di atas Tilemap. Sebuah cell dianggap walkable jika ada tile di Ground
/// dan tidak ada tile di Wall. Panggil Build() setiap kali ganti room.
/// Gerakan 8 arah, diagonal tidak boleh memotong sudut tembok.
/// </summary>
public static class GridPathfinder
{
    private struct HeapItem
    {
        public float f;
        public int idx;
    }

    private const float DiagonalCost = 1.41421356f;

    private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

    private static Tilemap refMap;
    private static Vector3Int origin;
    private static int width, height;

    private static bool[] walkable;
    private static float[] gCost;
    private static int[] parent;
    private static bool[] closed;
    private static readonly List<HeapItem> heap = new List<HeapItem>();

    public static bool IsReady => walkable != null && refMap != null;

    // ---------------- Build ----------------

    public static void Build(Tilemap ground, Tilemap wall)
    {
        if (ground == null)
        {
            Debug.LogError("GridPathfinder.Build: tilemap Ground null.");
            Clear();
            return;
        }

        ground.CompressBounds();
        BoundsInt b = ground.cellBounds;

        refMap = ground;
        origin = new Vector3Int(b.xMin, b.yMin, b.zMin);
        width = b.size.x;
        height = b.size.y;

        int n = width * height;
        walkable = new bool[n];
        gCost = new float[n];
        parent = new int[n];
        closed = new bool[n];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
                bool isGround = ground.HasTile(cell);
                bool isWall = wall != null && wall.HasTile(cell);
                walkable[y * width + x] = isGround && !isWall;
            }
        }
    }

    public static void Clear()
    {
        refMap = null;
        walkable = null;
    }

    // ---------------- Query ----------------

    public static bool IsWalkable(Vector2 world)
    {
        if (!IsReady) return false;
        return InBounds(WorldToGrid(world, out int x, out int y), x, y) && walkable[y * width + x];
    }

    /// <summary>Cari path dari start ke end (world position). Hasil berisi waypoint, TANPA cell awal.</summary>
    public static bool FindPath(Vector2 startWorld, Vector2 endWorld, List<Vector2> result)
    {
        result.Clear();
        if (!IsReady) return false;

        if (!TryGetWalkableCell(startWorld, 3, out int sx, out int sy, out _)) return false;
        if (!TryGetWalkableCell(endWorld, 4, out int ex, out int ey, out bool endSnapped)) return false;

        int start = sy * width + sx;
        int goal = ey * width + ex;

        if (start == goal)
        {
            result.Add(endSnapped ? CellCenter(ex, ey) : endWorld);
            return true;
        }

        System.Array.Fill(gCost, float.MaxValue);
        System.Array.Clear(closed, 0, closed.Length);
        heap.Clear();

        gCost[start] = 0f;
        parent[start] = -1;
        HeapPush(Heuristic(sx, sy, ex, ey), start);

        bool found = false;

        while (heap.Count > 0)
        {
            int cur = HeapPop().idx;
            if (closed[cur]) continue;
            closed[cur] = true;

            if (cur == goal) { found = true; break; }

            int cx = cur % width;
            int cy = cur / width;

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + Dx[d];
                int ny = cy + Dy[d];
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                int ni = ny * width + nx;
                if (!walkable[ni] || closed[ni]) continue;

                bool diagonal = d >= 4;
                if (diagonal && (!walkable[cy * width + nx] || !walkable[ny * width + cx])) continue;

                float ng = gCost[cur] + (diagonal ? DiagonalCost : 1f);
                if (ng < gCost[ni])
                {
                    gCost[ni] = ng;
                    parent[ni] = cur;
                    HeapPush(ng + Heuristic(nx, ny, ex, ey), ni);
                }
            }
        }

        if (!found) return false;

        // Rekonstruksi path (dari goal mundur ke start, cell start tidak dimasukkan)
        for (int i = goal; i != start; i = parent[i])
            result.Add(CellCenter(i % width, i / width));
        result.Reverse();

        // Kalau tujuan memang walkable, pakai posisi tepatnya di titik terakhir
        if (!endSnapped)
            result[result.Count - 1] = endWorld;

        return true;
    }

    // ---------------- Helpers ----------------

    private static float Heuristic(int x, int y, int ex, int ey)
    {
        int dx = Mathf.Abs(x - ex);
        int dy = Mathf.Abs(y - ey);
        return (dx + dy) + (DiagonalCost - 2f) * Mathf.Min(dx, dy); // octile
    }

    private static Vector3Int WorldToGrid(Vector2 world, out int x, out int y)
    {
        Vector3Int c = refMap.WorldToCell(world);
        x = c.x - origin.x;
        y = c.y - origin.y;
        return c;
    }

    private static bool InBounds(Vector3Int _, int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    private static Vector2 CellCenter(int x, int y)
    {
        return refMap.GetCellCenterWorld(new Vector3Int(origin.x + x, origin.y + y, origin.z));
    }

    /// <summary>Ambil cell walkable di posisi ini; kalau tidak walkable, cari yang terdekat dalam radius cell.</summary>
    private static bool TryGetWalkableCell(Vector2 world, int maxRadius, out int cx, out int cy, out bool snapped)
    {
        WorldToGrid(world, out int x, out int y);
        snapped = false;
        cx = x;
        cy = y;

        if (x >= 0 && y >= 0 && x < width && y < height && walkable[y * width + x])
            return true;

        snapped = true;
        Vector2 cellPos = world;
        float bestDist = float.MaxValue;
        bool any = false;

        for (int r = 1; r <= maxRadius && !any; r++)
        {
            for (int oy = -r; oy <= r; oy++)
            {
                for (int ox = -r; ox <= r; ox++)
                {
                    if (Mathf.Max(Mathf.Abs(ox), Mathf.Abs(oy)) != r) continue; // hanya ring terluar

                    int nx = x + ox;
                    int ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (!walkable[ny * width + nx]) continue;

                    float dist = (CellCenter(nx, ny) - cellPos).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        cx = nx;
                        cy = ny;
                        any = true;
                    }
                }
            }
        }

        return any;
    }

    // ---------------- Min-heap ----------------

    private static void HeapPush(float f, int idx)
    {
        heap.Add(new HeapItem { f = f, idx = idx });
        int i = heap.Count - 1;
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (heap[p].f <= heap[i].f) break;
            (heap[p], heap[i]) = (heap[i], heap[p]);
            i = p;
        }
    }

    private static HeapItem HeapPop()
    {
        HeapItem top = heap[0];
        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);

        int i = 0;
        while (true)
        {
            int l = i * 2 + 1;
            int r = l + 1;
            int s = i;
            if (l < heap.Count && heap[l].f < heap[s].f) s = l;
            if (r < heap.Count && heap[r].f < heap[s].f) s = r;
            if (s == i) break;
            (heap[s], heap[i]) = (heap[i], heap[s]);
            i = s;
        }

        return top;
    }
}