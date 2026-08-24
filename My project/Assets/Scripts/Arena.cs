using UnityEngine;

// This class controls information about the arena.
// Other scripts can use it to check whether something is inside
// the arena and to find the center of the arena.
public class Arena : MonoBehaviour
{
    // "static" means there is only one shared copy of this variable.
    // "public" means other scripts can access it.
    // "private set" means other scripts can read Instance,
    // but only this Arena script is allowed to change it.
    //
    // This allows other scripts to write:
    // Arena.Instance
    //
    // instead of having to find the Arena object themselves.
    public static Arena Instance { get; private set; }


    // This stores the PolygonCollider2D attached to the Arena.
    // The polygon is used to determine the exact shape of the arena.
    //
    // "private" means only this script can directly access it.
    private PolygonCollider2D polygon;


    // Awake runs when this object is created or loaded.
    // It runs before Start().
    private void Awake()
    {
        // Make this Arena available to other scripts through Arena.Instance.
        Instance = this;

        // Find the PolygonCollider2D attached to this same GameObject.
        polygon = GetComponent<PolygonCollider2D>();
    }


    // Checks whether a specific world position is inside the arena.
    //
    // Other scripts can use:
    //
    // Arena.Instance.IsInside(position)
    //
    // It returns true if the position is inside the polygon
    // and false if the position is outside.
    public bool IsInside(Vector2 position)
    {
        return polygon.OverlapPoint(position);
    }


    // Returns the center position of the arena.
    //
    // This assumes the Arena GameObject is positioned at the
    // center of the hexagon.
    public Vector2 GetCenter()
    {
        return transform.position;
    }


    // Calculates the direction from a given position toward
    // the center of the arena.
    //
    // This is mainly used by the AI when it gets too close
    // to the edge and needs to recover.
    public Vector2 GetSafeDirection(Vector2 position)
    {
        // Get the center of the arena.
        Vector2 center = (Vector2)transform.position;

        // Subtract the current position from the center.
        // The result points toward the center.
        //
        // .normalized changes the length of the vector to 1
        // while keeping the same direction.
        return (center - position).normalized;
    }
}