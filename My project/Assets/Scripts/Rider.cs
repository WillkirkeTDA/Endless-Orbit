using UnityEngine;

public class Rider : MonoBehaviour
{
    // "protected" means this variable can be used by this class
    // and also by classes that inherit from Rider, such as AIRider.
    //
    // "SerializeField" makes the variable visible in the Unity Inspector.
    //
    // Maximum normal movement speed of the rider.
    [Header("RIDER - Movement")]
    [SerializeField] protected float maxSpeed = 25f;

    // Controls how quickly the rider loses speed over time.
    // Closer to 1 = less slowdown.
    // Lower values = more slowdown.
    [SerializeField] protected float drag = 0.985f;


    // Controls how much knockback a rider produces when hitting another rider.
    [Header("RIDER - Impact")]

    // Base multiplier used when calculating knockback.
    [SerializeField] protected float impactMultiplier = 1.7f;

    // Adds extra knockback as the attacker's speed approaches maxSpeed.
    // 1 = no additional increase.
    // 2 = up to twice the normal knockback.
    [SerializeField] protected float maxSpeedKnockbackMultiplier = 1.5f;

    // Absolute maximum knockback that can be applied to another rider.
    [SerializeField] protected float maxKnockback = 25f;

    // Maximum speed a rider can temporarily reach because of knockback.
    // This is separate from the normal maxSpeed.
    [SerializeField] protected float maxKnockbackSpeed = 40f;

    // Controls how much speed the attacker loses after hitting someone.
    // 0 = keeps all speed.
    // 1 = loses all speed.
    [SerializeField] protected float selfImpactResistance = 0.15f;

    // Minimum collision speed required before an impact is registered.
    [SerializeField] protected float minimumImpactSpeed = 1.5f;


    // Controls how long riders have to wait before another collision
    // can be processed.
    [Header("RIDER - Impact Cooldown")]

    // Time in seconds before another collision can be processed.
    [SerializeField] private float impactCooldownTime = 0.15f;

    // How long the defender is allowed to exceed normal maxSpeed
    // after being hit.
    [SerializeField] private float knockbackSpeedTime = 0.15f;


    // "private" means only this Rider class can access the variable.
    // Stores the remaining collision cooldown time.
    private float impactCooldown;

    // Stores how much time remains where extra knockback speed is allowed.
    private float knockbackSpeedTimer;

    // Stores the Rigidbody2D attached to this rider.
    //
    // It is protected so AIRider can also use the Rigidbody2D.
    protected Rigidbody2D rb;

    // Tells other scripts whether this rider has been eliminated.
    //
    // "private set" means other scripts can read this value,
    // but only Rider can change it.
    public bool Eliminated { get; private set; }

    // Stores the rider who most recently hit this rider.
    // This is used to give the correct player credit for a knockout.
    private Rider lastAttacker;


    // Awake is called by Unity when the object is initialized.
    //
    // "virtual" means a child class such as AIRider can override
    // this method and provide its own version.
    protected virtual void Awake()
    {
        // Find the Rigidbody2D attached to this GameObject.
        rb = GetComponent<Rigidbody2D>();

        // Warn us if the Rigidbody2D is missing.
        if (rb == null)
        {
            Debug.LogError($"[{name}] Rider requires a Rigidbody2D.", this);
        }
    }


    // FixedUpdate runs on Unity's physics clock.
    // Physics movement is normally handled here.
    protected virtual void FixedUpdate()
    {
        // Stop doing physics logic if this rider has been eliminated.
        if (Eliminated)
            return;

        // Gradually reduce the rider's velocity.
        ApplyDrag();

        // While the knockback timer is active, the rider is allowed
        // to travel faster than the normal maxSpeed.
        if (knockbackSpeedTimer > 0f)
        {
            knockbackSpeedTimer -= Time.fixedDeltaTime;

            // Even during knockback, prevent an extreme velocity.
            if (rb.linearVelocity.magnitude > maxKnockbackSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxKnockbackSpeed;
            }
        }
        else
        {
            // Once knockback has finished, use the normal speed limit.
            LimitSpeed();
        }

        // Check whether the rider has left the arena.
        CheckArena();

        // Count down the collision cooldown.
        if (impactCooldown > 0f)
        {
            impactCooldown -= Time.fixedDeltaTime;

            if (impactCooldown < 0f)
                impactCooldown = 0f;
        }
    }


    // Update runs once per rendered frame.
    //
    // This debug ray shows the direction the rider is facing.
    protected virtual void Update()
    {
        Debug.DrawRay(transform.position, transform.right * 2f, Color.red);
    }


    // Unity automatically calls this when this rider collides
    // with another 2D physics object.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Try to find a Rider component on the object we hit.
        Rider other = collision.collider.GetComponent<Rider>();

        // Ignore objects that are not riders.
        if (other == null)
            return;

        // Ignore collisions involving eliminated riders.
        if (Eliminated || other.Eliminated)
            return;

        // Make sure both riders have Rigidbody2D components.
        if (rb == null || other.rb == null)
            return;

        // Prevent this rider from processing repeated impacts
        // during its short cooldown.
        if (impactCooldown > 0f)
            return;

        // Also make sure the other rider is not on cooldown.
        if (other.impactCooldown > 0f)
            return;

        // Process the collision.
        ProcessCollision(other);
    }


    // Determines who attacked whom and calculates the knockback.
    private void ProcessCollision(Rider other)
    {
        // Save the current velocities before changing them.
        Vector2 myVelocity = rb.linearVelocity;
        Vector2 otherVelocity = other.rb.linearVelocity;

        // Calculate the direction from this rider toward the other rider.
        Vector2 directionToOther = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

        // If the riders are practically in the exact same position,
        // there is no reliable direction to use.
        if (directionToOther.sqrMagnitude < 0.001f)
            return;

        // Calculate how quickly this rider is moving toward the other rider.
        float myClosingSpeed = Vector2.Dot(myVelocity, directionToOther);

        // Calculate how quickly the other rider is moving toward this rider.
        float otherClosingSpeed = Vector2.Dot(otherVelocity, -directionToOther);

        // These variables will store who attacked and who was hit.
        Rider attacker;
        Rider defender;

        // Direction the knockback should travel.
        Vector2 attackDirection;

        // Speed used to calculate the strength of the impact.
        float impactSpeed;

        // The rider moving toward the other rider faster becomes the attacker.
        if (myClosingSpeed > otherClosingSpeed)
        {
            attacker = this;
            defender = other;
            attackDirection = directionToOther;
            impactSpeed = myClosingSpeed;
        }
        else
        {
            attacker = other;
            defender = this;
            attackDirection = -directionToOther;
            impactSpeed = otherClosingSpeed;
        }

        // Print collision information to the Unity Console.
        Debug.Log(
            $"<color=#00CCFF>[COLLISION]</color> {attacker.name} -> {defender.name}\n" +
            $"  Attacker velocity: {attacker.rb.linearVelocity}\n" +
            $"  Defender velocity: {defender.rb.linearVelocity}\n" +
            $"  Attacker closing speed: {impactSpeed:F2}\n" +
            $"  Direction: {attackDirection}"
        );

        // Ignore impacts that are too slow.
        if (impactSpeed < minimumImpactSpeed)
        {
            Debug.Log(
                $"<color=#888888>[IMPACT IGNORED]</color> " +
                $"{attacker.name} -> {defender.name} | Impact speed: {impactSpeed:F2}"
            );

            return;
        }

        // Convert the impact speed into a percentage.
        //
        // 0 = minimum impact speed.
        // 1 = maxSpeed.
        float speedPercent = Mathf.InverseLerp(minimumImpactSpeed, maxSpeed, impactSpeed);

        // Increase knockback as the attacker's speed increases.
        float speedKnockbackMultiplier = Mathf.Lerp(1f, maxSpeedKnockbackMultiplier, speedPercent);

        // Calculate the final knockback amount.
        float knockback = impactSpeed * impactMultiplier * speedKnockbackMultiplier;

        // Make sure knockback cannot exceed the Inspector limit.
        knockback = Mathf.Clamp(knockback, 0f, maxKnockback);

        // Save both velocities before applying the impact.
        Vector2 attackerVelocityBefore = attacker.rb.linearVelocity;
        Vector2 defenderVelocityBefore = defender.rb.linearVelocity;

        // Apply the knockback force to the defender.
        defender.rb.AddForce(attackDirection * knockback, ForceMode2D.Impulse);

        // Allow the defender to temporarily move faster than normal.
        defender.knockbackSpeedTimer = knockbackSpeedTime;

        // Remember who caused this impact.
        defender.lastAttacker = attacker;

        // Reduce the attacker's speed after the collision.
        attacker.rb.linearVelocity *= 1f - selfImpactResistance;

        // Start the collision cooldown on both riders.
        attacker.impactCooldown = impactCooldownTime;
        defender.impactCooldown = impactCooldownTime;

        // Save velocities after the impact.
        Vector2 attackerVelocityAfter = attacker.rb.linearVelocity;
        Vector2 defenderVelocityAfter = defender.rb.linearVelocity;

        // Calculate how much the attacker's speed changed.
        float attackerSpeedChange = attackerVelocityAfter.magnitude - attackerVelocityBefore.magnitude;

        // Calculate how much the defender's speed changed.
        float defenderSpeedChange = defenderVelocityAfter.magnitude - defenderVelocityBefore.magnitude;

        // Give the impact a simple strength classification.
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

        // Print the final impact information to the Console.
        Debug.Log(
            $"<color=#FFD84D>[{strength} IMPACT]</color> {attacker.name} -> {defender.name}\n" +
            $"  Impact speed: {impactSpeed:F2}\n" +
            $"  Knockback: {knockback:F2}\n" +
            $"  Direction: {attackDirection}\n" +
            $"  {attacker.name} speed: {attackerVelocityBefore.magnitude:F2} -> {attackerVelocityAfter.magnitude:F2}\n" +
            $"  {defender.name} speed: {defenderVelocityBefore.magnitude:F2} -> {defenderVelocityAfter.magnitude:F2}\n" +
            $"  Attacker change: {attackerSpeedChange:+0.00;-0.00;0.00}\n" +
            $"  Defender change: {defenderSpeedChange:+0.00;-0.00;0.00}"
        );
    }


    // Gradually slows the rider down over time.
    private void ApplyDrag()
    {
        rb.linearVelocity *= Mathf.Pow(drag, Time.fixedDeltaTime * 60f);
    }


    // Prevents normal movement from exceeding maxSpeed.
    private void LimitSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }


    // Checks whether the rider has left the arena.
    private void CheckArena()
    {
        // If there is no Arena object, there is nothing to check.
        if (Arena.Instance == null)
            return;

        // If the rider is outside the arena collider, eliminate them.
        if (!Arena.Instance.IsInside(transform.position))
        {
            Debug.Log($"<color=#FF4444>[ELIMINATED] {name} left the arena!</color>");
            Eliminate();
        }
    }


    // Removes the rider from the active game.
    //
    // "virtual" means a child class can replace this method
    // with its own version if needed.
    public virtual void Eliminate()
    {
        // Prevent this method from running twice.
        if (Eliminated)
            return;

        // Mark this rider as eliminated.
        Eliminated = true;

        // Check whether this rider is an AI.
        AIRider ai = GetComponent<AIRider>();

        // If this is an AI, tell the AI spawner that an AI was defeated.
        if (ai != null)
        {
            AISpawner spawner = FindAnyObjectByType<AISpawner>();

            if (spawner != null)
            {
                spawner.RegisterAIDefeated();
            }
        }

        // Check whether another rider caused the elimination.
        if (lastAttacker != null)
        {
            PlayerInput player = lastAttacker.GetComponent<PlayerInput>();

            // If the player knocked out an AI, increase the score.
            if (player != null && ai != null)
            {
                GameManager.RiderKnocked(this);

                Debug.Log(
                    "<color=#00FF00>[SCORE] PLAYER KNOCKED AI! " +
                    "Total: " + GameManager.Knocked +
                    "</color>"
                );
            }
        }

        // Tell the GameManager that this rider was eliminated.
        GameManager.RiderEliminated(this);

        // Disable the rider's GameObject instead of destroying it.
        gameObject.SetActive(false);
    }
}