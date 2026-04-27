using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public string firstLevelScene = "BaseScene";

    public void OnPlayClicked()
    {
        Debug.Log("[TitleManager] Play clicked!");
        SceneManager.LoadScene(firstLevelScene);
    }

    public void OnExitClicked()
    {
        Debug.Log("[TitleManager] Exit clicked!");
        UnityEditor.EditorApplication.isPlaying = false;

    }
}