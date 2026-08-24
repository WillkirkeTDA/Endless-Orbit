using UnityEngine;
using UnityEngine.SceneManagement;

// This class controls information that belongs to the whole game,
// rather than to one specific GameObject.
//
// "static" means we do not need to create a GameManager GameObject.
// Other scripts can access it directly using:
//
// GameManager.Knocked
//
// Because this class is static, it cannot inherit from MonoBehaviour.
public static class GameManager
{
    // Stores the number of AI riders the player has knocked out.
    //
    // "public" means other scripts can read this value.
    //
    // "private set" means other scripts can NOT change the value.
    // Only this GameManager can change it.
    //
    // Other scripts can do:
    //
    // int score = GameManager.Knocked;
    //
    // But they cannot do:
    //
    // GameManager.Knocked = 10;
    public static int Knocked { get; private set; }


    // Keeps track of whether the game has ended.
    //
    // "private" means only this GameManager can access it.
    //
    // "static" means there is only one copy shared by the whole game.
    private static bool gameOver;


    // Starts a new game.
    //
    // This resets the score and then loads the Game scene.
    public static void StartGame()
    {
        // Reset the number of AI riders knocked out.
        Knocked = 0;

        // The game is no longer considered over.
        gameOver = false;

        Debug.Log("GAME STARTED");

        // Load the scene named "Game".
        //
        // Make sure the scene is added to:
        // File > Build Settings > Scenes In Build
        SceneManager.LoadScene("Game");
    }


    // Called when the player knocks an AI rider.
    //
    // "Rider rider" means this method receives the Rider that
    // was knocked out.
    //
    // We currently don't need to use the rider parameter directly,
    // but it allows the collision/elimination system to tell the
    // GameManager which rider was knocked out.
    public static void RiderKnocked(Rider rider)
    {
        // Do not change the score after the game has ended.
        if (gameOver)
            return;

        // Add one to the player's knock-out score.
        Knocked++;

        Debug.Log("RIDER KNOCKED! Total: " + Knocked);
    }


    // Called whenever a Rider is eliminated.
    //
    // This method checks whether the eliminated Rider was the player.
    // If it was, the game ends.
    public static void RiderEliminated(Rider rider)
    {
        // Ignore eliminations after the game is already over.
        if (gameOver)
            return;

        // Try to find a PlayerInput component on the eliminated rider.
        //
        // If the rider is the player, it should have PlayerInput.
        // AI riders do not have PlayerInput.
        PlayerInput player = rider.GetComponent<PlayerInput>();

        // If PlayerInput was found, the player was eliminated.
        if (player != null)
            GameOver();
    }


    // Ends the game and returns the player to the Start scene.
    //
    // "private" means only GameManager can call this method.
    private static void GameOver()
    {
        // Prevent GameOver from running more than once.
        if (gameOver)
            return;

        // Remember that the game has ended.
        gameOver = true;

        // Show the final score in the Console.
        Debug.Log("GAME OVER! Final Knocked: " + Knocked);

        // Return to the Start scene.
        SceneManager.LoadScene("Start");
    }
}