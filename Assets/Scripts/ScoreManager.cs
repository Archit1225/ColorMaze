using UnityEngine;

// 1. Add this attribute so Unity knows it can be converted to JSON
[System.Serializable]
public class PlayerScoreData
{
    public string token;
    public string playerName;
    public string score;
    public int levelSeed;
}
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    private void Start()
    {
        if (Instance != null) Destroy(Instance);
        else Instance = this;
    }
    public void SaveAndSendScore(string idToken, string pName, string finalScore, int seed)
    {
        PlayerScoreData myData = new PlayerScoreData
        {
            token = idToken,
            playerName = pName,
            score = finalScore,
            levelSeed = seed
        };

        string jsonString = JsonUtility.ToJson(myData);

        Debug.Log("Generated JSON: " + jsonString);
    }
}