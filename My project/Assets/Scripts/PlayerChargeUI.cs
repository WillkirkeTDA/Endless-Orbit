using UnityEngine;
using UnityEngine.UI;

public class PlayerChargeUI : MonoBehaviour
{
    // Reference to the player's movement script.
    // This lets the UI know whether the player is charging
    // and how much charge they currently have.
    [Header("Player")]
    [SerializeField] private PlayerInput player;

    // The color used when the charge is low.
    [Header("Colors")]
    [SerializeField] private Color lowChargeColor = Color.red;

    // The color used when the charge reaches the medium
    // charge threshold.
    [SerializeField] private Color mediumChargeColor = Color.yellow;

    // The color used when the charge reaches the maximum
    // charge threshold.
    [SerializeField] private Color maxChargeColor = Color.green;

    // The percentage where the charge bar changes from
    // low charge to medium charge.
    [Header("Charge Thresholds")]
    [SerializeField] private float mediumThreshold = 0.5f;

    // The percentage where the charge bar changes from
    // medium charge to maximum charge.
    [SerializeField] private float maxThreshold = 0.9f;

    // The Unity Slider component that visually represents
    // the player's charge.
    private Slider slider;

    // The Image used by the Slider as its fill area.
    // This is what allows us to change the bar's color.
    private Image fillImage;

    // CanvasGroup controls the visibility of the entire
    // charge bar without disabling the GameObject.
    private CanvasGroup canvasGroup;

    // Awake runs when this GameObject is initialized.
    // It is commonly used to find components and set them up.
    private void Awake()
    {
        // Get the Slider component attached to this GameObject.
        slider = GetComponent<Slider>();

        // Stop setup if there is no Slider.
        if (slider == null)
        {
            Debug.LogError("NO SLIDER FOUND!");
            return;
        }

        // Set the minimum possible Slider value.
        slider.minValue = 0f;

        // Set the maximum possible Slider value.
        slider.maxValue = 1f;

        // Allow the Slider to use decimal values instead
        // of only whole numbers.
        slider.wholeNumbers = false;

        // Start the charge bar at zero.
        slider.value = 0f;

        // Check whether the Slider has a Fill object.
        if (slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();

        // Warn us if the Fill object does not have an Image.
        if (fillImage == null)
            Debug.LogWarning("CHARGE UI: NO FILL IMAGE FOUND!");

        // Try to find a CanvasGroup on this GameObject.
        canvasGroup = GetComponent<CanvasGroup>();

        // If there isn't one, create one automatically.
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Hide the charge bar when the game starts.
        canvasGroup.alpha = 0f;

        Debug.Log("CHARGE UI READY");
    }

    // Update runs once every frame.
    // We use it here because the player's charge can change
    // every frame while the player is holding the button.
    private void Update()
    {
        // Make sure the PlayerInput reference was assigned.
        if (player == null)
        {
            Debug.LogWarning("PLAYER REFERENCE IS MISSING!");
            return;
        }

        // Show the charge bar while the player is charging.
        if (player.IsCharging)
            canvasGroup.alpha = 1f;
        else
            canvasGroup.alpha = 0f;

        // Get the player's current charge percentage.
        // The value will be between 0 and 1.
        float charge = player.ChargePercent;

        // Update the Slider so its visual amount matches
        // the player's current charge.
        slider.value = charge;

        // Update the bar's color based on the charge amount.
        UpdateColor(charge);
    }

    // Changes the Fill Image's color depending on how much
    // the player has charged.
    private void UpdateColor(float charge)
    {
        // There is nothing to change if the Fill Image
        // could not be found.
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
        // Otherwise, use the low charge color.
        else
        {
            fillImage.color = lowChargeColor;
        }
    }
}