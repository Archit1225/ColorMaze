using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ScoreSubmitter : MonoBehaviour
{
    public string baseUrl = "https://achi-wali-website.vercel.app/";
    public string gameSecret = "should_mutually agree";
    public string gameId = "Color Maze";
    public string username = "testplayer";
    public string password = "password123";
    public TMP_InputField usernameIF;
    public TMP_InputField passwordIF;

    [Serializable]
    private class LoginPayload { public string identifier; public string password; }

    [Serializable]
    private class LoginResponse { public bool action; public string message; public LoginData data; }

    [Serializable]
    private class LoginData { public string userId; public string gameToken; }

    [Serializable]
    private class ScorePayload
    {
        public string gameId;
        public float score;
        public string scoreStr;
        public long timestamp;
        public string gameToken;
        public string signature;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    [Serializable]
    private class ScoreResponse { public bool action; public string message; }

    // Call this from your GameManager when the level ends
    public void SubmitGameData(float finalScore)
    {
        StartCoroutine(SubmitRoutine(finalScore));
        Debug.Log("FunctionRan");
    }

    public void SubmitDetails()
    {
        this.username = usernameIF.text;
        this.password = passwordIF.text;
    }

    private IEnumerator SubmitRoutine(float finalScore)
    {
        string loginJson = JsonUtility.ToJson(new LoginPayload { identifier = username, password = password });

        using (UnityWebRequest loginReq = new UnityWebRequest(baseUrl + "/game/login", "POST"))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(loginJson);
            loginReq.uploadHandler = new UploadHandlerRaw(jsonToSend);
            loginReq.downloadHandler = new DownloadHandlerBuffer();
            loginReq.SetRequestHeader("Content-Type", "application/json");

            yield return loginReq.SendWebRequest();
            if (loginReq.result != UnityWebRequest.Result.Success) yield break;

            LoginResponse loginRes = JsonUtility.FromJson<LoginResponse>(loginReq.downloadHandler.text);
            if (!loginRes.action) yield break;

            string userId = loginRes.data.userId;
            string gameToken = loginRes.data.gameToken;

            yield return new WaitForSeconds(3f);

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // The anti-cheat signature is back to the original format without the seed
            string message = $"{userId}:{finalScore}:{timestamp}:{gameSecret}";
            string signature = ComputeSHA256Hash(message);

            ScorePayload scorePayload = new ScorePayload
            {
                gameId = gameId,
                score = finalScore,
                scoreStr = finalScore.ToString("0.00") + " pt",
                timestamp = timestamp,
                gameToken = gameToken,
                signature = signature
            };

            string scoreJson = JsonUtility.ToJson(scorePayload);

            using (UnityWebRequest scoreReq = new UnityWebRequest(baseUrl + "/game/score", "POST"))
            {
                byte[] scoreBytes = new UTF8Encoding().GetBytes(scoreJson);
                scoreReq.uploadHandler = new UploadHandlerRaw(scoreBytes);
                scoreReq.downloadHandler = new DownloadHandlerBuffer();
                scoreReq.SetRequestHeader("Content-Type", "application/json");

                yield return scoreReq.SendWebRequest();
            }
            Debug.Log("Done");
        }
    }

    private string ComputeSHA256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}