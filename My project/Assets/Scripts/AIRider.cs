using UnityEngine;

public class AIRider : Rider
{
    private enum AIState
    {
        Thinking,
        Turning,
        Charging,
        Moving,
        Recovering
    }

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 240f;

    [Header("Targeting")]
    [SerializeField] private float predictionTime = 0.08f;
    [SerializeField] private float aimTolerance = 1.0f;

    [Header("Attack")]
    [SerializeField] private float chargeDuration = 0.95f;
    [SerializeField] private float minCharge = 0.2f;
    [SerializeField] private float maxCharge = 0.7f;

    [Header("Impulse")]
    [SerializeField] private float minImpulse = 4f;
    [SerializeField] private float maxImpulse = 12f;

    [Header("Safety")]
    [SerializeField] private float edgeMargin = 1.5f;

    [Header("AI")]
    [SerializeField] private float thinkDelay = 0.1f;
    [SerializeField] private float attackCooldown = 0.4f;

    [Header("Testing")]
    [SerializeField] private bool testMode = false;
    [SerializeField] private bool stayStill = false;

    public bool IsCharging
    {
        get
        {
            return state == AIState.Charging;
        }
    }

    public float ChargePercent
    {
        get
        {
            if (chargeDuration <= 0f)
                return 0f;

            return Mathf.Clamp01(
                chargeTimer / chargeDuration
            );
        }
    }

    private AIState state;

    private Rider target;

    private float thinkTimer;
    private float cooldownTimer;

    private float chargeTimer;

    private Vector2 desiredDirection;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        state = AIState.Thinking;
        thinkTimer = 0f;
    }

    protected override void Update()
    {
        base.Update();

        if (Eliminated)
            return;

        // =====================================================
        // TEST MODE
        // =====================================================

        if (testMode && stayStill)
        {
            // AI does absolutely nothing.
            // Physics is still fully active, so the player
            // can hit and knock the AI around.
            return;
        }

        // =====================================================
        // NORMAL AI
        // =====================================================

        cooldownTimer -= Time.deltaTime;

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

        // =====================================================
        // DEBUG
        // =====================================================

        // Red = actual launch direction.
        Debug.DrawRay(
            transform.position,
            transform.right * 2f,
            Color.red
        );

        // Green = direction AI currently wants to face.
        Debug.DrawRay(
            transform.position,
            desiredDirection * 2f,
            Color.green
        );
    }

    // =====================================================
    // THINK
    // =====================================================

    private void Think()
    {
        if (thinkTimer > 0f)
        {
            thinkTimer -= Time.deltaTime;
            return;
        }

        thinkTimer = thinkDelay;

        // -------------------------------------------------
        // SAFETY FIRST
        // -------------------------------------------------

        if (IsNearEdge())
        {
            desiredDirection =
                Arena.Instance.GetSafeDirection(
                    transform.position
                );

            state = AIState.Recovering;
            return;
        }

        // -------------------------------------------------
        // FIND TARGET
        // -------------------------------------------------

        target = FindBestTarget();

        if (target == null)
            return;

        desiredDirection =
            GetPredictedDirection();

        state = AIState.Turning;
    }

    // =====================================================
    // TARGET SELECTION
    // =====================================================

    private Rider FindBestTarget()
    {
        Rider[] riders = FindObjectsByType<Rider>();

        Rider best = null;

        float bestScore =
            float.NegativeInfinity;

        foreach (Rider rider in riders)
        {
            if (rider == this)
                continue;

            if (rider.Eliminated)
                continue;

            Vector2 toTarget =
                (Vector2)rider.transform.position -
                (Vector2)transform.position;

            float distance =
                toTarget.magnitude;

            if (distance < 0.01f)
                continue;

            // ---------------------------------------------
            // BASIC DISTANCE SCORE
            // ---------------------------------------------

            float score =
                10f -
                distance;

            // ---------------------------------------------
            // TARGET NEAR EDGE = BETTER TARGET
            // ---------------------------------------------

            if (Arena.Instance != null)
            {
                Vector2 targetPos =
                    rider.transform.position;

                Vector2 center =
                    Arena.Instance.GetCenter();

                float centerDistance =
                    Vector2.Distance(
                        targetPos,
                        center
                    );

                score +=
                    centerDistance * 0.7f;
            }

            // ---------------------------------------------
            // TARGET MOVING OUTWARD = BETTER
            // ---------------------------------------------

            Rigidbody2D targetBody =
                rider.GetComponent<Rigidbody2D>();

            if (Arena.Instance != null)
            {
                Vector2 outward =
                    (
                        (Vector2)rider.transform.position -
                        Arena.Instance.GetCenter()
                    ).normalized;

                float outwardVelocity =
                    Vector2.Dot(
                        targetBody.linearVelocity,
                        outward
                    );

                score +=
                    outwardVelocity * 1.5f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = rider;
            }
        }

        return best;
    }

    // =====================================================
    // PREDICTION
    // =====================================================

    private Vector2 GetPredictedDirection()
    {
        if (target == null)
            return transform.right;

        Vector2 targetPosition =
            (Vector2)target.transform.position;

        Vector2 myPosition =
            (Vector2)transform.position;

        Rigidbody2D targetBody =
            target.GetComponent<Rigidbody2D>();

        // If the player is moving slowly,
        // DON'T predict anything.
        if (targetBody == null ||
            targetBody.linearVelocity.magnitude < 1f)
        {
            return
                (targetPosition - myPosition)
                .normalized;
        }

        // Small prediction only for a moving target.
        Vector2 predictedPosition =
            targetPosition +
            targetBody.linearVelocity *
            predictionTime;

        return
            (predictedPosition - myPosition)
            .normalized;
    }

    // =====================================================
    // TURN
    // =====================================================

    private void Turn()
    {
        if (target == null || target.Eliminated)
        {
            state = AIState.Thinking;
            return;
        }

        // ---------------------------------------------
        // SAFETY
        // ---------------------------------------------

        if (IsNearEdge())
        {
            desiredDirection =
                Arena.Instance.GetSafeDirection(
                    transform.position
                );

            state = AIState.Recovering;
            return;
        }

        // ---------------------------------------------
        // AIM AT TARGET
        // ---------------------------------------------

        desiredDirection =
            GetPredictedDirection();

        RotateToward(desiredDirection);

        // ---------------------------------------------
        // COMPARE ACTUAL FORWARD WITH TARGET
        // ---------------------------------------------

        Vector2 actualDirection =
            transform.right.normalized;

        Vector2 targetDirection =
            desiredDirection.normalized;

        float angle =
            Vector2.Angle(
                actualDirection,
                targetDirection
            );

        // Red = actual launch direction
        Debug.DrawRay(
            transform.position,
            actualDirection * 3f,
            Color.red
        );

        // Green = desired target direction
        Debug.DrawRay(
            transform.position,
            targetDirection * 3f,
            Color.green
        );

        // ---------------------------------------------
        // DON'T ATTACK UNTIL REALLY AIMED
        // ---------------------------------------------

        if (angle > 1.0f)
        {
            return;
        }

        // ---------------------------------------------
        // COOLDOWN
        // ---------------------------------------------

        if (cooldownTimer > 0f)
        {
            return;
        }

        // ---------------------------------------------
        // MAKE SURE THE ATTACK IS SAFE
        // ---------------------------------------------

        if (!IsLaunchSafe(
                actualDirection,
                minCharge
            ))
        {
            Debug.Log(
                name +
                " wants to attack, but launch is unsafe."
            );

            state = AIState.Recovering;
            return;
        }

        // ---------------------------------------------
        // PERFECT AIM
        // ---------------------------------------------

        Debug.Log(
            name +
            " PERFECT AIM! angle=" +
            angle
        );

        StartCharging();
    }

    // =====================================================
    // ROTATION
    // =====================================================

    private void RotateToward(Vector2 direction)
    {
        if (direction.sqrMagnitude <
            0.001f)
            return;

        float desiredAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        float currentAngle =
            transform.eulerAngles.z;

        float newAngle =
            Mathf.MoveTowardsAngle(
                currentAngle,
                desiredAngle,
                rotationSpeed *
                Time.deltaTime
            );

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                newAngle
            );
    }

    // =====================================================
    // CHARGE
    // =====================================================

    private void StartCharging()
    {
        state = AIState.Charging;

        chargeTimer = 0f;

        // Lock the exact direction we're facing.
        desiredDirection =
            transform.right.normalized;

        Debug.Log(
            name +
            " STARTED CHARGING"
        );
    }

    private void Charge()
    {
        chargeTimer += Time.deltaTime;

        // Don't rotate while charging.
        transform.right =
            desiredDirection;

        if (chargeTimer >= chargeDuration)
        {
            Debug.Log(
                name +
                " CHARGE COMPLETE -> LAUNCH"
            );

            Launch();
        }
    }

    // =====================================================
    // LAUNCH
    // =====================================================

    private void Launch()
    {
        Vector2 direction =
            desiredDirection.normalized;

        float chargePercent =
            Mathf.Clamp01(
                 chargeTimer / chargeDuration
            );

        float impulse =
            Mathf.Lerp(
                minImpulse,
                maxImpulse,
                chargePercent
            );

        Debug.Log(
            name +
            " LAUNCHING " +
            direction +
            " impulse=" +
            impulse
        );

        rb.AddForce(
            direction * impulse,
            ForceMode2D.Impulse
        );

        cooldownTimer =
            attackCooldown;

        state =
            AIState.Moving;
    }

    // =====================================================
    // MOVING
    // =====================================================

    private void Moving()
    {
        // Absolutely nothing happens here.
        //
        // No:
        // - rotation
        // - targeting
        // - charging
        // - steering
        //
        // The physics controls the rider.

        if (rb.linearVelocity.magnitude >
            0.5f)
        {
            return;
        }

        rb.linearVelocity =
            Vector2.zero;

        thinkTimer =
            thinkDelay;

        state =
            AIState.Thinking;
    }

    // =====================================================
    // RECOVER
    // =====================================================

    private void Recover()
    {
        if (Arena.Instance == null)
        {
            state = AIState.Thinking;
            return;
        }

        Vector2 center =
            Arena.Instance.GetCenter();

        Vector2 toCenter =
            center -
            (Vector2)transform.position;

        if (toCenter.sqrMagnitude <
            0.01f)
        {
            state = AIState.Thinking;
            return;
        }

        desiredDirection =
            toCenter.normalized;

        RotateToward(
            desiredDirection
        );

        float angle =
            Vector2.Angle(
                transform.right,
                desiredDirection
            );

        // Wait until facing center.
        if (angle > aimTolerance)
            return;

        // Make a small controlled push
        // toward the center.
        if (IsLaunchSafe(
                transform.right,
                0.3f
            ))
        {
            rb.AddForce(
                transform.right *
                minImpulse,
                ForceMode2D.Impulse
            );

            state =
                AIState.Moving;
        }
    }

    // =====================================================
    // EDGE DETECTION
    // =====================================================

    private bool IsNearEdge()
    {
        if (Arena.Instance == null)
            return false;

        Vector2 position =
            transform.position;

        // First: actual position.
        if (!Arena.Instance.IsInside(position))
            return true;

        // Check around the rider.
        //
        // This is deliberately conservative.
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

        foreach (Vector2 direction in directions)
        {
            Vector2 check =
                position +
                direction *
                edgeMargin;

            if (!Arena.Instance.IsInside(check))
                return true;
        }

        return false;
    }

    // =====================================================
    // LAUNCH SAFETY
    // =====================================================

    private bool IsLaunchSafe(
     Vector2 direction,
     float charge
 )
    {
        if (Arena.Instance == null)
            return true;

        direction.Normalize();

        float percent =
            Mathf.Clamp01(
                charge / maxCharge
            );

        float estimatedDistance =
            Mathf.Lerp(
                1.0f,
                3.5f,
                percent
            );

        const int samples = 20;

        for (int i = 1; i <= samples; i++)
        {
            float t =
                i / (float)samples;

            Vector2 point =
                (Vector2)transform.position +
                direction *
                estimatedDistance *
                t;

            if (!Arena.Instance.IsInside(point))
            {
                return false;
            }
        }

        return true;
    }
}