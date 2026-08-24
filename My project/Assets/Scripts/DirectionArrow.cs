using UnityEngine;

// This attribute tells Unity that this GameObject must have a
// LineRenderer component attached to it.
//
// If a LineRenderer is missing, Unity will automatically add one.
[RequireComponent(typeof(LineRenderer))]
public class DirectionArrow : MonoBehaviour
{
    [Header("Arrow")]

    // Controls how long the direction arrow is.
    [SerializeField] private float length = 1.5f;

    // Controls how thick the arrow line is.
    [SerializeField] private float width = 0.06f;


    [Header("Color")]

    // Controls the color of the direction arrow.
    // Color.red means the default color is red.
    [SerializeField] private Color color = Color.red;


    // Stores a reference to the LineRenderer component.
    //
    // We use this variable later instead of repeatedly searching
    // for the LineRenderer.
    private LineRenderer line;


    // Awake runs when the GameObject is created or loaded.
    private void Awake()
    {
        // Get the LineRenderer attached to this same GameObject.
        line = GetComponent<LineRenderer>();

        // The arrow only needs two points:
        //
        // Point 0 = where the arrow starts.
        // Point 1 = where the arrow ends.
        line.positionCount = 2;

        // Set the thickness of the beginning of the line.
        line.startWidth = width;

        // Set the thickness of the end of the line.
        line.endWidth = width;

        // Set the color at the beginning of the line.
        line.startColor = color;

        // Set the color at the end of the line.
        line.endColor = color;

        // A higher sorting order makes the arrow render above
        // normal sprites.
        line.sortingOrder = 100;
    }


    // LateUpdate runs once every frame after normal Update methods.
    //
    // This is useful for the arrow because we want its position
    // to be updated after the rider's normal movement/rotation.
    private void LateUpdate()
    {
        // Get the position of the GameObject.
        //
        // This is where the arrow begins.
        Vector3 start = transform.position;

        // Calculate where the arrow should end.
        //
        // transform.right is the object's local right direction.
        // length controls how far the arrow extends.
        Vector3 end = transform.position + transform.right * length;

        // Tell the LineRenderer where its first point should be.
        line.SetPosition(0, start);

        // Tell the LineRenderer where its second point should be.
        line.SetPosition(1, end);
    }
}