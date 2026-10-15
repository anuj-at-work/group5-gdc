using UnityEngine;

// Polyline down the middle of the street. Used to work out how far along the
// player is and which way "forward" is at that point (the street bends).
// Waypoints are just the child transforms, in order.
public class StreetPath : MonoBehaviour
{
    Vector3[] pts;
    float[] dist;

    public float Length { get; private set; }

    void Awake() => Rebuild();

    public void Rebuild()
    {
        int n = transform.childCount;
        pts = new Vector3[n];
        dist = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 p = transform.GetChild(i).position;
            p.y = 0f;
            pts[i] = p;
            if (i > 0) dist[i] = dist[i - 1] + Vector3.Distance(pts[i - 1], p);
        }
        Length = n > 0 ? dist[n - 1] : 0f;
    }

    // distance along the path of the closest point to pos (XZ only)
    public float Project(Vector3 pos, out Vector3 tangent)
    {
        if (pts == null || pts.Length < 2) Rebuild();

        pos.y = 0f;
        float best = float.MaxValue;
        float bestDist = 0f;
        tangent = Vector3.forward;

        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            Vector3 ab = b - a;
            float len = ab.magnitude;
            if (len < 0.001f) continue;
            float t = Mathf.Clamp01(Vector3.Dot(pos - a, ab) / (len * len));
            Vector3 c = a + ab * t;
            float d = (pos - c).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestDist = dist[i] + len * t;
                tangent = ab / len;
            }
        }

        // smooth the tangent around corners so the "don't look back" cone
        // doesn't snap 90 degrees the moment you pass a waypoint
        tangent = SmoothTangent(bestDist, tangent);
        return bestDist;
    }

    public float Project(Vector3 pos) => Project(pos, out _);

    public Vector3 Evaluate(float d, out Vector3 tangent)
    {
        if (pts == null || pts.Length < 2) Rebuild();
        d = Mathf.Clamp(d, 0f, Length);
        for (int i = 0; i < pts.Length - 1; i++)
        {
            if (d <= dist[i + 1] || i == pts.Length - 2)
            {
                float seg = dist[i + 1] - dist[i];
                float t = seg > 0f ? (d - dist[i]) / seg : 0f;
                tangent = (pts[i + 1] - pts[i]).normalized;
                return Vector3.Lerp(pts[i], pts[i + 1], t);
            }
        }
        tangent = Vector3.forward;
        return pts[0];
    }

    public Vector3 Evaluate(float d) => Evaluate(d, out _);

    Vector3 SmoothTangent(float d, Vector3 raw)
    {
        const float blend = 2.5f;
        Evaluate(d - blend, out Vector3 behind);
        Evaluate(d + blend, out Vector3 ahead);
        Vector3 t = (behind + raw * 2f + ahead);
        t.y = 0f;
        return t.sqrMagnitude > 0.001f ? t.normalized : raw;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        for (int i = 0; i < transform.childCount - 1; i++)
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        for (int i = 0; i < transform.childCount; i++)
            Gizmos.DrawWireSphere(transform.GetChild(i).position, 0.2f);
    }
#endif
}
