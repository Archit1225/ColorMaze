using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
    public void ChangeScene(int sceneNo)
    {
        SceneManager.LoadScene(sceneNo);
    }
}
