using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton so the gas can easily find it

    [Header("UI References")]
    public CanvasGroup gameOverPanel;
    public Button restartButton;

    private bool isGameOver = false;

    private void Awake()
    {
        // Simple Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Ensure the panel is hidden and unclickable at the start
        if (gameOverPanel != null)
        {
            gameOverPanel.alpha = 0f;
            gameOverPanel.interactable = false;
            gameOverPanel.blocksRaycasts = false;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("Player was consumed by the Uroboros Edge!");

        // Stop the player from moving/building here
        // e.g., FindObjectOfType<GridBuildController>().enabled = false;

        StartCoroutine(FadeInGameOverScreen());
    }

    private IEnumerator FadeInGameOverScreen()
    {
        // Make the panel block clicks so the player can click the Restart button
        gameOverPanel.interactable = true;
        gameOverPanel.blocksRaycasts = true;

        float fadeDuration = 1.5f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            gameOverPanel.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        gameOverPanel.alpha = 1f;
    }

    private void RestartGame()
    {
        Debug.Log("Restarting Game...");
        // Reloads the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
