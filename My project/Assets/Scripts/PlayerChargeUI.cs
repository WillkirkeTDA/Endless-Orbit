using UnityEngine;
using UnityEngine.UI;

public class PlayerChargeUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerInput player;

    [Header("Colors")]
    [SerializeField] private Color lowChargeColor = Color.red;
    [SerializeField] private Color mediumChargeColor = Color.yellow;
    [SerializeField] private Color maxChargeColor = Color.green;

    [Header("Charge Thresholds")]
    [SerializeField] private float mediumThreshold = 0.5f;
    [SerializeField] private float maxThreshold = 0.9f;

    private Slider slider;
    private Image fillImage;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        if (slider == null)
        {
            Debug.LogError("NO SLIDER FOUND!");
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 0f;

        // Find Fill image
        if (slider.fillRect != null)
        {
            fillImage =
                slider.fillRect.GetComponent<Image>();
        }

        if (fillImage == null)
        {
            Debug.LogWarning(
                "CHARGE UI: NO FILL IMAGE FOUND!"
            );
        }

        // CanvasGroup lets us hide the bar
        // without disabling the GameObject.
        canvasGroup =
            GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        Debug.Log("CHARGE UI READY");
    }

    private void Update()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "PLAYER REFERENCE IS MISSING!"
            );

            return;
        }

        // ---------------------------------------------
        // SHOW / HIDE
        // ---------------------------------------------

        if (player.IsCharging)
        {
            canvasGroup.alpha = 1f;
        }
        else
        {
            canvasGroup.alpha = 0f;
        }

        // ---------------------------------------------
        // CHARGE
        // ---------------------------------------------

        float charge =
            player.ChargePercent;

        slider.value = charge;

        // ---------------------------------------------
        // COLOR
        // ---------------------------------------------

        UpdateColor(charge);
    }

    private void UpdateColor(float charge)
    {
        if (fillImage == null)
            return;

        if (charge >= maxThreshold)
        {
            fillImage.color =
                maxChargeColor;
        }
        else if (charge >= mediumThreshold)
        {
            fillImage.color =
                mediumChargeColor;
        }
        else
        {
            fillImage.color =
                lowChargeColor;
        }
    }
}