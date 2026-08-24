using UnityEngine;

// This class controls an AI rider.
// It inherits from Rider, so it automatically gets the movement,
// physics, collision, knockback, and elimination systems from Rider.
public class AIRider : Rider
{
    // This enum is used to remember what the AI is currently doing.
    // The AI can only be in one state at a time.
    private enum AIState
    {
        Thinking,   // Looking for a target or deciding what to do.
        Turning,    // Rotating toward the target.
        Charging,   // Charging an attack before launching.
        Moving,     // Currently flying through the arena.
        Recovering  // Too close to the edge, so returning toward the center.
    }

    // [Header] creates a label in the Unity Inspector.
    // [SerializeField] makes a private variable visible and editable in Unity.
    // private means only this class can directly use the variable.

    [Header("Rotation")]

    // How many degrees per second the AI can rotate.
    [SerializeField] private float rotationSpeed = 240f;


    [Header("Targeting")]

    // How far into the future the AI predicts the target's movement.
    // A larger value means the AI aims more in front of moving targets.
    [SerializeField] private float predictionTime = 0.08f;

    // How close the AI must be to the correct direction before attacking.
    [SerializeField] private float aimTolerance = 1.0f;


    [Header("Attack")]

    // How long a normal attack takes to fully charge.
    [SerializeField] private float chargeDuration = 0.95f;

    // Minimum amount of charge used for a normal attack.
    [SerializeField] private float minCharge = 0.2f;

    // Maximum amount of charge used for a normal attack.
    [SerializeField] private float maxCharge = 0.7f;


    [Header("Recovery")]

    // Recovery attacks use a shorter charge than normal attacks.
    // This makes the AI get away from the edge quickly.
    [SerializeField] private float recoveryChargeDuration = 0.35f;

    // True when the current charge is being used to escape the edge.
    private bool chargingForRecovery;


    [Header("Impulse")]

    // The smallest force the AI can use when launching.
    [SerializeField] private float minImpulse = 4f;

    // The strongest force the AI can use when launching.
    [SerializeField] private float maxImpulse = 12f;


    [Header("Safety")]

    // How far around the AI we check when deciding if it is close to the edge.
    [SerializeField] private float edgeMargin = 0.7f;


    [Header("AI")]

    // How often the AI waits before thinking again.
    [SerializeField] private float thinkDelay = 0.1f;

    // How long the AI must wait after attacking before attacking again.
    [SerializeField] private float attackCooldown = 0.4f;


    [Header("Testing")]

    // Allows testing features without changing the normal AI behaviour.
    [SerializeField] private bool testMode = false;

    // When enabled together with testMode, the AI will not control itself.
    [SerializeField] private bool stayStill = false;


    // Returns true when the AI is currently charging.
    // "public" means other scripts can access this property.
    public bool IsCharging
    {
        get { return state == AIState.Charging; }
    }


    // Returns how much the current attack has charged.
    // The value is between 0 and 1.
    public float ChargePercent
    {
        get
        {
            if (chargeDuration <= 0f)
                return 0f;

            return Mathf.Clamp01(chargeTimer / chargeDuration);
        }
    }


    // Stores the current state of the AI.
    // "private" means only AIRider can directly change it.
    private AIState state;

    // The rider that the AI has selected as its target.
    private Rider target;

    // Timer used to control how often the AI thinks.
    private float thinkTimer;

    // Timer that prevents the AI from attacking too quickly.
    private float cooldownTimer;

    // How long the AI has been charging its current attack.
    private float chargeTimer;

    // The direction the AI currently wants to face.
    private Vector2 desiredDirection;


    // "protected override" means we are replacing the Awake method
    // from the parent Rider class, while still allowing child classes
    // to access it.
    protected override void Awake()
    {
        base.Awake();
    }


    // Start runs once when the object is created and enabled.
    private void Start()
    {
        state = AIState.Thinking;
        thinkTimer = 0f;
    }


    // Update runs once every frame.
    // "override" means Rider already has an Update method and we are
    // adding AI-specific behaviour to it.
    protected override void Update()
    {
        base.Update();

        // Do nothing if this AI has already been eliminated.
        if (Eliminated)
            return;

        // Test mode allows us to make the AI completely passive.
        if (testMode && stayStill)
            return;

        // Count down the attack cooldown.
        cooldownTimer -= Time.deltaTime;

        // Run the code belonging to the current AI state.
        switch (state)
        {
            case AIState.Thinking:
                Think();
                break;

            case AIState.Turning:
                Turn();
                break;

            case AIState.Charging:
                Charge();
                break;

            case AIState.Moving:
                Moving();
                break;

            case AIState.Recovering:
                Recover();
                break;
        }

        // Red shows the direction the AI is actually facing.
        Debug.DrawRay(transform.position, transform.right * 2f, Color.red);

        // Green shows the direction the AI wants to face.
        Debug.DrawRay(transform.position, desiredDirection * 2f, Color.green);
    }


    // Think decides what the AI should do next.
    private void Think()
    {
        // Wait until the thinking timer reaches zero.
        if (thinkTimer > 0f)
        {
            thinkTimer -= Time.deltaTime;
            return;
        }

        // Reset the timer so the AI does not think every frame.
        thinkTimer = thinkDelay;

        // Check for the edge before looking for an enemy.
        if (IsNearEdge())
        {
            desiredDirection = Arena.Instance.GetSafeDirection(transform.position);
            state = AIState.Recovering;
            return;
        }

        // Find the best target to attack.
        target = FindBestTarget();

        // If there are no valid targets, keep thinking.
        if (target == null)
            return;

        // Work out which direction the AI should face.
        desiredDirection = GetPredictedDirection();

        // Start rotating toward the target.
        state = AIState.Turning;
    }


    // Finds the rider the AI thinks is the best target.
    private Rider FindBestTarget()
    {
        // Find every Rider currently active in the scene.
        Rider[] riders = FindObjectsByType<Rider>();

        Rider best = null;

        // Start with the lowest possible score.
        float bestScore = float.NegativeInfinity;

        foreach (Rider rider in riders)
        {
            // Never target itself.
            if (rider == this)
                continue;

            // Never target eliminated riders.
            if (rider.Eliminated)
                continue;

            // Calculate the direction and distance to this rider.
            Vector2 toTarget = (Vector2)rider.transform.position - (Vector2)transform.position;
            float distance = toTarget.magnitude;

            // Ignore riders that are basically on top of us.
            if (distance < 0.01f)
                continue;

            // Start with a score based on distance.
            // Closer riders receive a higher score.
            float score = 10f - distance;

            // Give targets near the edge a higher score.
            if (Arena.Instance != null)
            {
                Vector2 targetPos = rider.transform.position;
                Vector2 center = Arena.Instance.GetCenter();
                float centerDistance = Vector2.Distance(targetPos, center);

                score += centerDistance * 0.7f;
            }

            // Check whether the target is moving toward the edge.
            Rigidbody2D targetBody = rider.GetComponent<Rigidbody2D>();

            if (Arena.Instance != null && targetBody != null)
            {
                Vector2 outward = ((Vector2)rider.transform.position - Arena.Instance.GetCenter()).normalized;

                float outwardVelocity = Vector2.Dot(targetBody.linearVelocity, outward);

                score += outwardVelocity * 1.5f;
            }

            // If this rider has the best score so far, remember it.
            if (score > bestScore)
            {
                bestScore = score;
                best = rider;
            }
        }

        return best;
    }


    // Calculates the direction toward the target.
    // If the target is moving, the AI aims slightly ahead of it.
    private Vector2 GetPredictedDirection()
    {
        // If there is no target, keep the current direction.
        if (target == null)
            return transform.right;

        Vector2 targetPosition = (Vector2)target.transform.position;
        Vector2 myPosition = (Vector2)transform.position;
        Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();

        // If the target is moving slowly, aim directly at it.
        if (targetBody == null || targetBody.linearVelocity.magnitude < 1f)
            return (targetPosition - myPosition).normalized;

        // Predict where the target will be shortly.
        Vector2 predictedPosition = targetPosition + targetBody.linearVelocity * predictionTime;

        return (predictedPosition - myPosition).normalized;
    }


    // Rotates the AI toward its target and decides when to attack.
    private void Turn()
    {
        // If the target disappeared or was eliminated, find another one.
        if (target == null || target.Eliminated)
        {
            state = AIState.Thinking;
            return;
        }

        // If the AI gets too close to the edge while aiming,
        // stop attacking and recover first.
        if (IsNearEdge())
        {
            desiredDirection = Arena.Instance.GetSafeDirection(transform.position);
            state = AIState.Recovering;
            return;
        }

        // Update the target direction in case the target moved.
        desiredDirection = GetPredictedDirection();

        // Rotate toward the target.
        RotateToward(desiredDirection);

        // Get the AI's actual facing direction.
        Vector2 actualDirection = transform.right.normalized;

        // Get the direction the AI wants to face.
        Vector2 targetDirection = desiredDirection.normalized;

        // Calculate the angle between the two directions.
        float angle = Vector2.Angle(actualDirection, targetDirection);

        // Red shows the actual direction.
        Debug.DrawRay(transform.position, actualDirection * 3f, Color.red);

        // Green shows the desired direction.
        Debug.DrawRay(transform.position, targetDirection * 3f, Color.green);

        // Do not attack until the AI is properly aimed.
        if (angle > aimTolerance)
            return;

        // Wait if the attack is still on cooldown.
        if (cooldownTimer > 0f)
            return;

        // Make sure the AI will not launch itself outside the arena.
        if (!IsLaunchSafe(actualDirection, minCharge))
        {
            Debug.Log(name + " wants to attack, but launch is unsafe.");
            state = AIState.Recovering;
            return;
        }

        Debug.Log(name + " PERFECT AIM! angle=" + angle);

        StartCharging();
    }


    // Rotates the AI toward a specific direction.
    private void RotateToward(Vector2 direction)
    {
        // Ignore extremely small or invalid directions.
        if (direction.sqrMagnitude < 0.001f)
            return;

        // Convert the direction from Vector2 into an angle in degrees.
        float desiredAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Get the AI's current Z rotation.
        float currentAngle = transform.eulerAngles.z;

        // Move toward the desired angle without instantly snapping.
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle, rotationSpeed * Time.deltaTime);

        // Apply the new rotation.
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }


    // Starts charging either a normal attack or a recovery attack.
    private void StartCharging(bool recovery = false)
    {
        state = AIState.Charging;
        chargeTimer = 0f;

        // Remember whether this is an edge recovery charge.
        chargingForRecovery = recovery;

        // Lock the exact direction the AI is currently facing.
        desiredDirection = transform.right.normalized;

        Debug.Log(name + (recovery ? " STARTED RECOVERY CHARGE" : " STARTED CHARGING"));
    }


    // Increases the charge timer until the attack is ready.
    private void Charge()
    {
        chargeTimer += Time.deltaTime;

        // Keep the AI facing the locked direction while charging.
        transform.right = desiredDirection;

        // Recovery charges use a different duration.
        float duration = chargingForRecovery ? recoveryChargeDuration : chargeDuration;

        // Launch when the required charge time has passed.
        if (chargeTimer >= duration)
        {
            if (chargingForRecovery)
            {
                Debug.Log(name + " RECOVERY CHARGE COMPLETE -> LAUNCH");
                LaunchRecovery();
            }
            else
            {
                Debug.Log(name + " CHARGE COMPLETE -> LAUNCH");
                Launch();
            }
        }
    }


    // Performs a normal attack launch.
    private void Launch()
    {
        Vector2 direction = desiredDirection.normalized;

        // Convert the current charge time into a 0-1 percentage.
        float chargePercent = Mathf.Clamp01(chargeTimer / chargeDuration);

        // Convert the percentage into an impulse between min and max.
        float impulse = Mathf.Lerp(minImpulse, maxImpulse, chargePercent);

        Debug.Log(name + " LAUNCHING " + direction + " impulse=" + impulse);

        // Add an instant physics force in the desired direction.
        rb.AddForce(direction * impulse, ForceMode2D.Impulse);

        // Start the attack cooldown.
        cooldownTimer = attackCooldown;

        // The AI is now moving through the arena.
        state = AIState.Moving;
    }


    // Performs a recovery launch toward the center.
    private void LaunchRecovery()
    {
        Vector2 direction = desiredDirection.normalized;

        Debug.Log(name + " RECOVERY LAUNCH | direction=" + direction + " | impulse=" + maxImpulse);

        // Use the maximum impulse so the AI gets away from the edge quickly.
        rb.AddForce(direction * maxImpulse, ForceMode2D.Impulse);

        chargingForRecovery = false;

        // Let physics control the AI while it moves.
        state = AIState.Moving;
    }


    // Handles the period when the AI is physically moving.
    private void Moving()
    {
        // The AI does not steer while moving.
        // Its Rigidbody2D controls its movement.

        // Keep waiting while the AI is still moving.
        if (rb.linearVelocity.magnitude > 0.5f)
            return;

        // Stop tiny leftover movement.
        rb.linearVelocity = Vector2.zero;

        // Give the AI a short delay before thinking again.
        thinkTimer = thinkDelay;

        // Start thinking again.
        state = AIState.Thinking;
    }


    // Handles escaping from the arena edge.
    private void Recover()
    {
        // If there is no Arena object, return to normal AI behaviour.
        if (Arena.Instance == null)
        {
            state = AIState.Thinking;
            return;
        }

        // If the AI is safely away from the edge,
        // stop recovering and return to normal behaviour.
        if (!IsNearEdge())
        {
            rb.linearVelocity *= 0.5f;

            thinkTimer = thinkDelay;
            state = AIState.Thinking;

            Debug.Log(name + " FINISHED EDGE RECOVERY");

            return;
        }

        // Get the center of the arena.
        Vector2 center = Arena.Instance.GetCenter();

        // Get the AI's current position.
        Vector2 position = transform.position;

        // Calculate the direction from the AI toward the center.
        Vector2 toCenter = center - position;

        // If we are already basically at the center,
        // there is no need to recover.
        if (toCenter.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            state = AIState.Thinking;
            return;
        }

        // The center becomes the direction we want to face.
        desiredDirection = toCenter.normalized;

        // Rotate toward the center.
        RotateToward(desiredDirection);

        // Blue shows the direction used for recovery.
        Debug.DrawRay(transform.position, desiredDirection * 3f, Color.blue);

        // Check how closely we are facing the center.
        float angle = Vector2.Angle(transform.right, desiredDirection);

        // Keep rotating until we are aimed correctly.
        if (angle > aimTolerance)
            return;

        // Once aimed correctly, charge a recovery attack.
        StartCharging(true);
    }


    // Checks whether the AI is close to the edge of the arena.
    private bool IsNearEdge()
    {
        // Without an Arena, we cannot perform an edge check.
        if (Arena.Instance == null)
            return false;

        Vector2 position = transform.position;

        // If the AI is already outside the arena,
        // it definitely needs recovery.
        if (!Arena.Instance.IsInside(position))
            return true;

        // These directions are used to check around the AI.
        // This works with the hexagon because we check multiple directions.
        Vector2[] directions =
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down,
            new Vector2(0.707f, 0.707f),
            new Vector2(-0.707f, 0.707f),
            new Vector2(0.707f, -0.707f),
            new Vector2(-0.707f, -0.707f)
        };

        // Check every direction around the AI.
        foreach (Vector2 direction in directions)
        {
            Vector2 check = position + direction * edgeMargin;

            // If any point around the AI is outside,
            // the AI is considered close to the edge.
            if (!Arena.Instance.IsInside(check))
                return true;
        }

        // None of the checks reached outside the arena.
        return false;
    }


    // Checks whether launching in a direction would send the AI outside.
    private bool IsLaunchSafe(Vector2 direction, float charge)
    {
        // If there is no Arena, we cannot check safety.
        // Returning true allows the launch.
        if (Arena.Instance == null)
            return true;

        // Make sure the direction has a length of exactly 1.
        direction.Normalize();

        // Convert the charge into a percentage.
        float percent = Mathf.Clamp01(charge / maxCharge);

        // Estimate how far the AI could travel from this launch.
        float estimatedDistance = Mathf.Lerp(1.0f, 3.5f, percent);

        // Number of points checked along the launch path.
        const int samples = 20;

        // Check several points between the AI and the estimated destination.
        for (int i = 1; i <= samples; i++)
        {
            // Convert the loop number into a percentage from 0 to 1.
            float t = i / (float)samples;

            // Calculate a point along the launch path.
            Vector2 point = (Vector2)transform.position + direction * estimatedDistance * t;

            // If any point leaves the arena, this launch is unsafe.
            if (!Arena.Instance.IsInside(point))
                return false;
        }

        // Every checked point stayed inside the arena.
        return true;
    }
}