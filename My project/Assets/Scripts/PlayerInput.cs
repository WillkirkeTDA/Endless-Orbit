using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float speedRotation = 100f;

    [Header("Impulse")]
    [SerializeField] private float minImpulse = 5f;
    [SerializeField] private float maxImpulse = 22f;
    [SerializeField] private float maxChargeTime = 0.95f;

    [Header("Movement Lock")]
    [SerializeField] private float movementThreshold = 0.5f;

    private Rigidbody2D rb;
    public bool IsCharging
    {
        get { return charging; }
    }
    private bool charging;
    private bool canInput = true;

    private float chargeTime;


    // The exact direction we locked when charging began.
    private Quaternion lockedRotation;

    public float ChargePercent
    {
        get
        {
            if (!charging)
                return 0f;

            return Mathf.Clamp01(
                chargeTime / maxChargeTime
            );
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Important:
        // Rigidbody rotation should not be allowed to
        // change because of collisions.
        rb.freezeRotation = true;
    }

    private void Update()
    {
        if (!canInput)
            return;

        // ---------------------------------------------
        // CHARGING
        // ---------------------------------------------

        if (charging)
        {
            // Force the Transform back to the rotation
            // selected when charging began.
            transform.rotation = lockedRotation;

            chargeTime += Time.deltaTime;

            chargeTime =
                Mathf.Min(
                    chargeTime,
                    maxChargeTime
                );

            // Release when the button is no longer held.
            if (!Input.anyKey)
            {
                ReleaseCharge();
            }

            return;
        }

        // ---------------------------------------------
        // NORMAL ROTATION
        // ---------------------------------------------

        if (CanControl())
        {
            transform.Rotate(
                Vector3.forward *
                speedRotation *
                Time.deltaTime
            );
        }

        // ---------------------------------------------
        // START CHARGING
        // ---------------------------------------------

        if (Input.anyKeyDown && CanControl())
        {
            StartCharging();
        }
    }

    private bool CanControl()
    {
        return rb.linearVelocity.magnitude <=
               movementThreshold;
    }

    private void StartCharging()
    {
        charging = true;
        chargeTime = 0f;

        // Save EXACTLY where the vehicle is facing.
        lockedRotation =
            transform.rotation;

        // Make absolutely sure physics can't
        // rotate the vehicle while charging.
        rb.freezeRotation = true;
    }

    private void ReleaseCharge()
    {
        charging = false;

        // Restore the locked direction one last time.
        transform.rotation = lockedRotation;

        Launch();

        chargeTime = 0f;

        // Don't accept another input until stopped.
        canInput = false;
    }

    private void Launch()
    {
        float percent =
            Mathf.Clamp01(
                chargeTime /
                maxChargeTime
            );

        float impulse =
            Mathf.Lerp(
                minImpulse,
                maxImpulse,
                percent
            );

        rb.AddForce(
            transform.right *
            impulse,
            ForceMode2D.Impulse
        );
    }

    private void FixedUpdate()
    {
        if (!canInput)
        {
            if (rb.linearVelocity.magnitude <=
                movementThreshold)
            {
                rb.linearVelocity =
                    Vector2.zero;

                canInput = true;
            }
        }
    }
}