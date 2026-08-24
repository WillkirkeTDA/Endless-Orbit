using UnityEngine;

public class Arena : MonoBehaviour
{
    public static Arena Instance { get; private set; }

    private PolygonCollider2D polygon;

    private void Awake()
    {
        Instance = this;

        polygon = GetComponent<PolygonCollider2D>();
    }

    public bool IsInside(Vector2 position)
    {
        return polygon.OverlapPoint(position);
    }

    public Vector2 GetCenter()
    {
        return transform.position;
    }

    public Vector2 GetSafeDirection(Vector2 position)
    {
        Vector2 center =
            (Vector2)transform.position;

        return (center - position).normalized;
    }
}