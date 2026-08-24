using UnityEngine;

public class Rider : MonoBehaviour
{
    [Header("RIDER - Movement")]
    [SerializeField] protected float maxSpeed = 25f;
    [SerializeField] protected float drag = 0.985f;

    [Header("RIDER - Impact")]
    [SerializeField] protected float impactMultiplier = 1.7f;
    [SerializeField] protected float selfImpactResistance = 0.15f;
    [SerializeField] protected float minimumImpactSpeed = 1.5f;

    [Header("RIDER - Impact Cooldown")]
    [SerializeField] private float impactCooldownTime = 0.15f;

    private float impactCooldown;

    protected Rigidbody2D rb;

    public bool Eliminated { get; private set; }

    // The rider who most recently hit this rider.
    private Rider lastAttacker;


    // =====================================================
    // AWAKE
    // =====================================================

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                $"[{name}] Rider requires a Rigidbody2D.",
                this
            );
        }
    }


    // =====================================================
    // FIXED UPDATE
    // =====================================================

    protected virtual void FixedUpdate()
    {
        if (Eliminated)
            return;

        ApplyDrag();
        LimitSpeed();
        CheckArena();

        if (impactCooldown > 0f)
        {
            impactCooldown -= Time.fixedDeltaTime;

            if (impactCooldown < 0f)
                impactCooldown = 0f;
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    protected virtual void Update()
    {
        // Red = actual facing / launch direction.
        Debug.DrawRay(
            transform.position,
            transform.right * 2f,
            Color.red
        );
    }


    // =====================================================
    // COLLISION
    // =====================================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rider other =
            collision.collider.GetComponent<Rider>();

        if (other == null)
            return;

        if (Eliminated || other.Eliminated)
            return;

        if (rb == null || other.rb == null)
            return;

        // Prevent the same collision from being processed
        // repeatedly during the short impact window.
        if (impactCooldown > 0f)
            return;

        if (other.impactCooldown > 0f)
            return;

        ProcessCollision(other);
    }


    // =====================================================
    // PROCESS COLLISION
    // =====================================================

    private void ProcessCollision(Rider other)
    {
        Vector2 myVelocity =
            rb.linearVelocity;

        Vector2 otherVelocity =
            other.rb.linearVelocity;


        // =================================================
        // DIRECTION BETWEEN RIDERS
        // =================================================

        Vector2 directionToOther =
            (
                (Vector2)other.transform.position -
                (Vector2)transform.position
            ).normalized;

        if (directionToOther.sqrMagnitude < 0.001f)
            return;


        // =================================================
        // CLOSING SPEED
        // =================================================

        // Positive = THIS rider is moving toward OTHER.
        float myClosingSpeed =
            Vector2.Dot(
                myVelocity,
                directionToOther
            );


        // Positive = OTHER rider is moving toward THIS.
        float otherClosingSpeed =
            Vector2.Dot(
                otherVelocity,
                -directionToOther
            );


        // =================================================
        // DETERMINE ATTACKER
        // =================================================

        Rider attacker;
        Rider defender;

        Vector2 attackDirection;
        float impactSpeed;


        if (myClosingSpeed > otherClosingSpeed)
        {
            attacker = this;
            defender = other;

            attackDirection =
                directionToOther;

            impactSpeed =
                myClosingSpeed;
        }
        else
        {
            attacker = other;
            defender = this;

            attackDirection =
                -directionToOther;

            impactSpeed =
                otherClosingSpeed;
        }


        // =================================================
        // COLLISION DEBUG
        // =================================================

        Debug.Log(
            $"<color=#00CCFF>[COLLISION]</color> " +
            $"{attacker.name} -> {defender.name}\n" +

            $"  Attacker velocity: " +
            $"{attacker.rb.linearVelocity}\n" +

            $"  Defender velocity: " +
            $"{defender.rb.linearVelocity}\n" +

            $"  Attacker closing speed: " +
            $"{impactSpeed:F2}\n" +

            $"  Direction: " +
            $"{attackDirection}"
        );


        // =================================================
        // MINIMUM IMPACT
        // =================================================

        if (impactSpeed < minimumImpactSpeed)
        {
            Debug.Log(
                $"<color=#888888>" +
                $"[IMPACT IGNORED]</color> " +

                $"{attacker.name} -> " +
                $"{defender.name} | " +

                $"Impact speed: " +
                $"{impactSpeed:F2}"
            );

            return;
        }


        // =================================================
        // CALCULATE KNOCKBACK
        // =================================================

        float knockback =
            impactSpeed *
            impactMultiplier;

        knockback =
            Mathf.Clamp(
                knockback,
                0f,
                25f
            );


        // =================================================
        // SAVE VELOCITIES
        // =================================================

        Vector2 attackerVelocityBefore =
            attacker.rb.linearVelocity;

        Vector2 defenderVelocityBefore =
            defender.rb.linearVelocity;


        // =================================================
        // APPLY KNOCKBACK
        // =================================================

        defender.rb.AddForce(
            attackDirection * knockback,
            ForceMode2D.Impulse
        );


        // =================================================
        // REMEMBER ATTACKER
        // =================================================

        defender.lastAttacker =
            attacker;


        // =================================================
        // ATTACKER REACTION
        // =================================================

        attacker.rb.linearVelocity *=
            (1f - selfImpactResistance);


        // =================================================
        // COOLDOWN
        // =================================================

        attacker.impactCooldown =
            impactCooldownTime;

        defender.impactCooldown =
            impactCooldownTime;


        // =================================================
        // VELOCITIES AFTER IMPACT
        // =================================================

        Vector2 attackerVelocityAfter =
            attacker.rb.linearVelocity;

        Vector2 defenderVelocityAfter =
            defender.rb.linearVelocity;


        // =================================================
        // SPEED CHANGES
        // =================================================

        float attackerSpeedChange =
            attackerVelocityAfter.magnitude -
            attackerVelocityBefore.magnitude;

        float defenderSpeedChange =
            defenderVelocityAfter.magnitude -
            defenderVelocityBefore.magnitude;


        // =================================================
        // IMPACT CLASSIFICATION
        // =================================================

        string strength;

        if (knockback < 5f)
        {
            strength = "WEAK";
        }
        else if (knockback < 9f)
        {
            strength = "MEDIUM";
        }
        else if (knockback < 14f)
        {
            strength = "STRONG";
        }
        else
        {
            strength = "HUGE";
        }


        // =================================================
        // IMPACT LOG
        // =================================================

        Debug.Log(
            $"<color=#FFD84D>" +
            $"[{strength} IMPACT]" +
            $"</color> " +

            $"{attacker.name} -> " +
            $"{defender.name}\n" +

            $"  Impact speed: " +
            $"{impactSpeed:F2}\n" +

            $"  Knockback: " +
            $"{knockback:F2}\n" +

            $"  Direction: " +
            $"{attackDirection}\n" +

            $"  {attacker.name} speed: " +
            $"{attackerVelocityBefore.magnitude:F2} -> " +
            $"{attackerVelocityAfter.magnitude:F2}\n" +

            $"  {defender.name} speed: " +
            $"{defenderVelocityBefore.magnitude:F2} -> " +
            $"{defenderVelocityAfter.magnitude:F2}\n" +

            $"  Attacker change: " +
            $"{attackerSpeedChange:+0.00;-0.00;0.00}\n" +

            $"  Defender change: " +
            $"{defenderSpeedChange:+0.00;-0.00;0.00}"
        );
    }


    // =====================================================
    // DRAG
    // =====================================================

    private void ApplyDrag()
    {
        rb.linearVelocity *=
            Mathf.Pow(
                drag,
                Time.fixedDeltaTime * 60f
            );
    }


    // =====================================================
    // SPEED LIMIT
    // =====================================================

    private void LimitSpeed()
    {
        if (rb.linearVelocity.magnitude >
            maxSpeed)
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized *
                maxSpeed;
        }
    }


    // =====================================================
    // ARENA CHECK
    // =====================================================

    private void CheckArena()
    {
        if (Arena.Instance == null)
            return;

        if (!Arena.Instance.IsInside(
                transform.position))
        {
            Debug.Log(
                $"<color=#FF4444>" +
                $"[ELIMINATED] {name} " +
                $"left the arena!" +
                $"</color>"
            );

            Eliminate();
        }
    }


    // =====================================================
    // ELIMINATION
    // =====================================================

    public virtual void Eliminate()
    {
        if (Eliminated)
            return;

        Eliminated = true;


        // =================================================
        // SCORE
        // =================================================

        if (lastAttacker != null)
        {
            PlayerInput player =
                lastAttacker.GetComponent<PlayerInput>();

            AIRider ai =
                GetComponent<AIRider>();


            // Player knocked an AI.
            if (player != null && ai != null)
            {
                GameManager.RiderKnocked(this);

                Debug.Log(
                    "<color=#00FF00>" +
                    "[SCORE] PLAYER KNOCKED AI! " +
                    "Total: " +
                    GameManager.Knocked +
                    "</color>"
                );
            }
        }


        // =================================================
        // GAME MANAGER
        // =================================================

        GameManager.RiderEliminated(this);


        // =================================================
        // DISABLE
        // =================================================

        gameObject.SetActive(false);
    }
}