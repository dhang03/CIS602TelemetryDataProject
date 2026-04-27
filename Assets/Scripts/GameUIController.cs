using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUIController : MonoBehaviour
{
    [Header("Panels & Overlays")]
    public GameObject rulesPanel;
    public TMP_Text countdownText;
    public TMP_Text collectibleCounterText;
    public GameObject nextLevelPanel;
    public GameObject gameOverPanel;

    [Header("Navigation")]
    public string nextLevelSceneName;
    public string titleSceneName = "TitleScene";

    private BaseLevelManager levelManager;
    private PlayerController playerController;
    private bool isGameActive = false;


    void Awake()
    {
        levelManager = FindObjectOfType<BaseLevelManager>();
        playerController = FindObjectOfType<PlayerController>();

        rulesPanel.SetActive(true);
        countdownText.gameObject.SetActive(false);
        nextLevelPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        if (playerController) playerController.enabled = false;
    }

    void Update()
    {
        if (!isGameActive) return;

        if (levelManager != null && collectibleCounterText != null)
        {
            collectibleCounterText.text = $"{levelManager.collectedCount}/{levelManager.collectibleCount}";

            if (levelManager.collectedCount >= levelManager.collectibleCount &&
                !nextLevelPanel.activeSelf &&
                !gameOverPanel.activeSelf)
            {
                OnLevelComplete();
            }
        }
    }

    public void OnStartButtonClicked()
    {
        rulesPanel.SetActive(false);
        countdownText.gameObject.SetActive(true);
        StartCoroutine(RunCountdown());
    }

    public void OnNextLevelClicked() => SceneManager.LoadScene(nextLevelSceneName);
    public void OnMainMenuClicked() => SceneManager.LoadScene(titleSceneName);

    public void OnExitClicked()
    {
        UnityEditor.EditorApplication.isPlaying = false;
    }




    IEnumerator RunCountdown()
    {
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);

        if (levelManager != null) levelManager.StartLevel();

        if (levelManager != null && levelManager.GetModeName() == "ForcedSwitching")
        {
            var ringUI = FindObjectOfType<RingSwitchUI>();
            if (ringUI != null) ringUI.ShowUI();
        }

        StartGameplay();
    }

    void StartGameplay()
    {
        isGameActive = true;
        if (playerController) playerController.enabled = true;
    }

    public void OnLevelComplete()
    {
        isGameActive = false;
        if (playerController) playerController.enabled = false;

        if (!string.IsNullOrEmpty(nextLevelSceneName)) 
        {
            nextLevelPanel.SetActive(true);
        }
        else
        {
            ShowGameOver(); 
        }
    }

    public void OnLevelFailed()
    {
        isGameActive = false;
        if (playerController != null) playerController.enabled = false;
        ShowGameOver();
    }

    void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }
 

}