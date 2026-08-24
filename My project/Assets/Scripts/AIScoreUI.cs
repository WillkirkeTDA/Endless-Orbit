using UnityEngine;
using UnityEngine.UI;

public class AIScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<Text>();
    }

    private void Update()
    {
        scoreText.text =
            "Knocked: " +
            GameManager.Knocked;
    }
}