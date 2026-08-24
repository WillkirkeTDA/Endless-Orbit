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

    // Rigidbody2D controls the player's physics movement.
    // We keep a reference to it so we can change its velocity,
    // apply forces, and control its physics settings.
    //
    // "private" means only this PlayerInput class can directly
    // access this variable.
    private Rigidbody2D rb;

    // This tells other scripts whether the player is currently charging.
    //
    // "public" means other scripts are allowed to read it.
    // The "get" returns the value of the private "charging" variable.
    //
    // We don't use "set", so other scripts cannot change IsCharging.
    // Only this script can change the actual charging state.
    public bool IsCharging
    {
        get { return charging; }
    }

    // True while the player is holding a charge.
    private bool charging;

    // Prevents the player from accepting another input while moving.
    private bool canInput = true;

    // How long the player has been charging the current attack.
    private float chargeTime;

    // Stores the exact rotation the player had when charging started.
    // The player stays locked to this direction until the charge is released.
    private Quaternion lockedRotation;

    // Returns the current charge as a value between 0 and 1.
    //
    // 0 = no charge
    // 1 = maximum charge
    //
    // Other scripts can read this value, but cannot directly change it.
    public float ChargePercent
    {
        get
        {
            if (!charging)
                return 0f;

            return Mathf.Clamp01(chargeTime / maxChargeTime);
        }
    }

    // Awake runs when the GameObject is initialized.
    // It happens before Start().
    //
    // "private" means this method is only used by this class.
    private void Awake()
    {
        // GetComponent looks for a Rigidbody2D attached to the same
        // GameObject as this script.
        rb = GetComponent<Rigidbody2D>();

        // Prevent physics collisions from rotating the player.
        // We want the player's rotation to be controlled by our script.
        rb.freezeRotation = true;
    }

    // Update runs once every frame.
    //
    // This is where we handle player input and rotation because
    // input should normally be checked every rendered frame.
    private void Update()
    {
        // If the player is currently moving, don't accept new input.
        if (!canInput)
            return;

        // If the player is already charging, handle the charge.
        if (charging)
        {
            // Keep the player pointing in exactly the direction
            // that was selected when charging started.
            transform.rotation = lockedRotation;

            // Increase the charge timer based on real frame time.
            chargeTime += Time.deltaTime;

            // Prevent the timer from going beyond the maximum charge time.
            chargeTime = Mathf.Min(chargeTime, maxChargeTime);

            // Input.anyKey is true while any keyboard key is being held.
            // When no key is being held anymore, release the charge.
            if (!Input.anyKey)
            {
                ReleaseCharge();
            }

            return;
        }

        // The player can only rotate while they are basically stopped.
        if (CanControl())
        {
            // Rotate around the Z axis because this is a 2D game.
            transform.Rotate(
                Vector3.forward * speedRotation * Time.deltaTime
            );
        }

        // Input.anyKeyDown is true for the frame when a key is first pressed.
        // That starts the charging process.
        if (Input.anyKeyDown && CanControl())
        {
            StartCharging();
        }
    }

    // Checks whether the player is moving slowly enough to be controlled.
    //
    // linearVelocity is the current movement speed and direction
    // of the Rigidbody2D.
    private bool CanControl()
    {
        return rb.linearVelocity.magnitude <= movementThreshold;
    }

    // Starts a new charge.
    private void StartCharging()
    {
        charging = true;
        chargeTime = 0f;

        // Save the exact direction the player is facing.
        // This becomes the direction of the attack.
        lockedRotation = transform.rotation;

        // Make sure physics cannot rotate the player during the charge.
        rb.freezeRotation = true;
    }

    // Called when the player releases the key.
    private void ReleaseCharge()
    {
        charging = false;

        // Restore the exact direction that was locked when charging began.
        transform.rotation = lockedRotation;

        // Apply the attack force.
        Launch();

        // Reset the charge timer so the next attack starts from zero.
        chargeTime = 0f;

        // Prevent another charge until the player has stopped moving.
        canInput = false;
    }

    // Converts the charge percentage into an actual launch force.
    private void Launch()
    {
        // Convert charge time into a value between 0 and 1.
        float percent = Mathf.Clamp01(chargeTime / maxChargeTime);

        // Interpolate between the minimum and maximum launch force.
        //
        // 0% charge  = minImpulse
        // 100% charge = maxImpulse
        float impulse = Mathf.Lerp(minImpulse, maxImpulse, percent);

        // Add an instantaneous force in the direction the player is facing.
        //
        // ForceMode2D.Impulse means this is an immediate push rather
        // than a force that is continuously applied over time.
        rb.AddForce(
            transform.right * impulse,
            ForceMode2D.Impulse
        );
    }

    // FixedUpdate runs at a fixed physics interval.
    // Physics-related checks are better handled here than in Update().
    private void FixedUpdate()
    {
        // If input is currently locked, check whether the player has stopped.
        if (!canInput)
        {
            if (rb.linearVelocity.magnitude <= movementThreshold)
            {
                // Remove any tiny remaining movement so the player
                // is completely stopped before accepting another charge.
                rb.linearVelocity = Vector2.zero;

                // The player can now rotate and charge again.
                canInput = true;
            }
        }
    }
}