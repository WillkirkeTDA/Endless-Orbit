using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameManager
{
    // =====================================================
    // SCORE
    // =====================================================

    public static int Knocked { get; private set; }

    private static bool gameOver;

    // =====================================================
    // START GAME
    // =====================================================

    public static void StartGame()
    {
        Knocked = 0;
        gameOver = false;

        Debug.Log("GAME STARTED");

        SceneManager.LoadScene("Game");
    }

    // =====================================================
    // AI KNOCKED
    // =====================================================

    public static void RiderKnocked(Rider rider)
    {
        if (gameOver)
            return;

        Knocked++;

        Debug.Log(
            "RIDER KNOCKED! Total: " +
            Knocked
        );
    }

    // =====================================================
    // RIDER ELIMINATED
    // =====================================================

    public static void RiderEliminated(Rider rider)
    {
        if (gameOver)
            return;

        PlayerInput player =
            rider.GetComponent<PlayerInput>();

        if (player != null)
        {
            GameOver();
        }
    }

    // =====================================================
    // GAME OVER
    // =====================================================

    private static void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;

        Debug.Log(
            "GAME OVER! Final Knocked: " +
            Knocked
        );

        SceneManager.LoadScene("Start");
    }
}