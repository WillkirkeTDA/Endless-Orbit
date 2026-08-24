using UnityEngine;
using UnityEngine.UI;

// This script updates the UI text that shows how many AI riders
// the player has knocked out.
public class AIScoreUI : MonoBehaviour
{
    // This is the UI Text component that will display the score.
    //
    // [SerializeField] makes the private variable visible in the
    // Unity Inspector, so you can drag your Text object into it.
    //
    // If you leave it empty, Awake() will automatically try to
    // find a Text component on the same GameObject.
    [SerializeField] private Text scoreText;


    // Awake runs when this object is created or enabled.
    // It happens before Start().
    private void Awake()
    {
        // If no Text component was assigned in the Inspector,
        // look for one on this same GameObject.
        if (scoreText == null)
            scoreText = GetComponent<Text>();
    }


    // Update runs once every frame.
    // Here we update the text so the displayed score always
    // matches the current number stored in GameManager.
    private void Update()
    {
        scoreText.text = "Knocked: " + GameManager.Knocked;
    }
}