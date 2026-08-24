using UnityEngine;
using UnityEngine.UI;

public class AIChargeUI : MonoBehaviour
{
    // Reference to the AI's movement script.
    // This lets the UI know when the AI is charging
    // and how much charge it currently has.
    [Header("AI")]
    [SerializeField] private AIRider ai;

    // Color used when the AI has a low amount of charge.
    [Header("Colors")]
    [SerializeField] private Color lowChargeColor = Color.red;

    // Color used when the AI reaches the medium charge
    // threshold.
    [SerializeField] private Color mediumChargeColor = Color.yellow;

    // Color used when the AI reaches the maximum charge
    // threshold.
    [SerializeField] private Color maxChargeColor = Color.green;

    // Charge percentage where the bar changes from
    // low charge to medium charge.
    [Header("Charge Thresholds")]
    [SerializeField] private float mediumThreshold = 0.5f;

    // Charge percentage where the bar changes from
    // medium charge to maximum charge.
    [SerializeField] private float maxThreshold = 0.9f;

    // The Slider component that visually displays the
    // AI's current charge.
    private Slider slider;

    // The Image used by the Slider as its fill area.
    // This is used to change the charge bar's color.
    private Image fillImage;

    // CanvasGroup controls the visibility of the charge bar.
    // We use it to hide the bar without disabling the
    // entire GameObject.
    private CanvasGroup canvasGroup;

    // Awake runs when this GameObject is initialized.
    // It is commonly used to find components and prepare them.
    private void Awake()
    {
        // Find the Slider component attached to this
        // GameObject.
        slider = GetComponent<Slider>();

        // Stop setting up the UI if there is no Slider.
        if (slider == null)
        {
            Debug.LogError("AI CHARGE UI: NO SLIDER FOUND!");
            return;
        }

        // The charge value starts at 0.
        slider.minValue = 0f;

        // The maximum charge value is 1.
        slider.maxValue = 1f;

        // Allow decimal values between 0 and 1.
        slider.wholeNumbers = false;

        // Start with an empty charge bar.
        slider.value = 0f;

        // Check if the Slider has a Fill object.
        if (slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();

        // Warn us if the Fill object could not be found.
        if (fillImage == null)
            Debug.LogWarning("AI CHARGE UI: NO FILL IMAGE FOUND!");

        // Try to find a CanvasGroup attached to this
        // GameObject.
        canvasGroup = GetComponent<CanvasGroup>();

        // If there is no CanvasGroup, create one automatically.
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Hide the charge bar when the game starts.
        canvasGroup.alpha = 0f;

        Debug.Log("AI CHARGE UI READY");
    }

    // Update runs once every frame.
    // The AI's charge can change every frame, so we
    // update the UI here.
    private void Update()
    {
        // Make sure an AIRider was assigned in the Inspector.
        if (ai == null)
        {
            Debug.LogWarning("AI CHARGE UI: AI REFERENCE IS MISSING!");
            return;
        }

        // Show the charge bar while the AI is charging.
        if (ai.IsCharging)
            canvasGroup.alpha = 1f;
        else
            canvasGroup.alpha = 0f;

        // Get the AI's current charge percentage.
        // The value is between 0 and 1.
        float charge = ai.ChargePercent;

        // Make the Slider match the AI's current charge.
        slider.value = charge;

        // Change the bar's color based on the charge amount.
        UpdateColor(charge);
    }

    // Changes the color of the charge bar depending on
    // how much the AI has charged.
    private void UpdateColor(float charge)
    {
        // If the Fill Image could not be found, there is
        // nothing to recolor.
        if (fillImage == null)
            return;

        // Use the maximum charge color when the charge
        // reaches the maximum threshold.
        if (charge >= maxThreshold)
        {
            fillImage.color = maxChargeColor;
        }
        // Use the medium color when the charge reaches
        // the medium threshold.
        else if (charge >= mediumThreshold)
        {
            fillImage.color = mediumChargeColor;
        }
        // Otherwise, the charge is still low.
        else
        {
            fillImage.color = lowChargeColor;
        }
    }
}