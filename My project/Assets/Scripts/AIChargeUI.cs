using UnityEngine;
using UnityEngine.UI;

public class AIChargeUI : MonoBehaviour
{
    [Header("AI")]
    [SerializeField] private AIRider ai;

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
            Debug.LogError("AI CHARGE UI: NO SLIDER FOUND!");
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 0f;

        // Get the Fill image.
        if (slider.fillRect != null)
        {
            fillImage =
                slider.fillRect.GetComponent<Image>();
        }

        if (fillImage == null)
        {
            Debug.LogWarning(
                "AI CHARGE UI: NO FILL IMAGE FOUND!"
            );
        }

        // Hide without disabling the GameObject.
        canvasGroup =
            GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        Debug.Log(
            "AI CHARGE UI READY"
        );
    }

    private void Update()
    {
        if (ai == null)
        {
            Debug.LogWarning(
                "AI CHARGE UI: AI REFERENCE IS MISSING!"
            );

            return;
        }

        // ---------------------------------------------
        // SHOW / HIDE
        // ---------------------------------------------

        if (ai.IsCharging)
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
            ai.ChargePercent;

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