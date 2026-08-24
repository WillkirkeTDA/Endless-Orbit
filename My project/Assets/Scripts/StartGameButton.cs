using UnityEngine;

public class StartGameButton : MonoBehaviour
{
    // This method is called by the Unity UI Button.
    // It tells the GameManager to start the game.
    public void StartGame()
    {
        GameManager.StartGame();
    }
}