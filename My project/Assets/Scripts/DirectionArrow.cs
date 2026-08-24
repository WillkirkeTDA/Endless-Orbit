using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DirectionArrow : MonoBehaviour
{
    [Header("Arrow")]
    [SerializeField] private float length = 1.5f;
    [SerializeField] private float width = 0.06f;

    [Header("Color")]
    [SerializeField] private Color color = Color.red;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;

        line.startColor = color;
        line.endColor = color;

        // Makes it render above normal sprites.
        line.sortingOrder = 100;
    }

    private void LateUpdate()
    {
        Vector3 start =
            transform.position;

        Vector3 end =
            transform.position +
            transform.right * length;

        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
}