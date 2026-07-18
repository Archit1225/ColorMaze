using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
    [SerializeField] private GameObject quitButton;

    public void ChangeScene(int sceneNo)
    {
        SceneManager.LoadScene(sceneNo);
    }

    public void ExitGame()
    {
        Debug.Log("Attempting to exit the game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Exits standalone builds
            Application.Quit();
#endif
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Disable the quit button so WebGL players never even see it
            if (quitButton != null)
            {
                quitButton.SetActive(false);
            }
#endif
    }
}