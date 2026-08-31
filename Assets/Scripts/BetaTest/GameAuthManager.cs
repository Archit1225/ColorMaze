using System;
using System.Collections;
using TMPro;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameAuthManager : MonoBehaviour
{
    public static GameAuthManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private string baseUrl = "https://www.cgsiitkgp.org/api";
    [SerializeField] private string gameSecret = "hamaigayhu6769"; 
    [SerializeField] private string gameId = "tyagi-uchalo"; // Change this in the Inspector if this is a different mini-game
    public TMP_InputField loginCodeIF;
    [Header("Player Session State")]
    public string UserId { get; private set; }
    public string Username { get; private set; }
    public string GameToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(GameToken);

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetUrlSearchQuery();
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 1. Try to auto-login via the URL first (just in case it's hosted directly)
        string authCode = ExtractAuthCodeFromUrl();

        if (!string.IsNullOrEmpty(authCode))
        {
            StartCoroutine(ExchangeAuthCodeCoroutine(authCode));
        }
        else
        {
            Debug.Log("[GameAuth] No gameAuthCode found in URL. Waiting for player to paste code manually.");
        }
    }

    // --- NEW: Called directly by your GameManager's OnLoginButtonClicked() ---
    public void LoginWithPastedCode()
    {
        string pastedCode = loginCodeIF.text;
        if (string.IsNullOrEmpty(pastedCode))
        {
            Debug.LogWarning("[GameAuth] Pasted code is empty!");
            return;
        }
        
        Debug.Log($"[GameAuth] Exchanging pasted code with backend...");
        StartCoroutine(ExchangeAuthCodeCoroutine(pastedCode));
    }

    private string ExtractAuthCodeFromUrl()
    {
        string queryString = "";

#if UNITY_WEBGL && !UNITY_EDITOR
        queryString = GetUrlSearchQuery();
#else
        if (Application.absoluteURL.Contains("?"))
        {
            queryString = Application.absoluteURL.Substring(Application.absoluteURL.IndexOf("?"));
        }
#endif

        if (string.IsNullOrEmpty(queryString)) return null;

        if (queryString.StartsWith("?")) queryString = queryString.Substring(1);
        string[] pairs = queryString.Split('&');
        foreach (string pair in pairs)
        {
            string[] kv = pair.Split('=');
            if (kv.Length == 2 && kv[0] == "gameAuthCode")
            {
                return UnityWebRequest.UnEscapeURL(kv[1]);
            }
        }
        return null;
    }

    private IEnumerator ExchangeAuthCodeCoroutine(string code)
    {
        string endpoint = $"{baseUrl}/game/session/exchange";
        string jsonBody = JsonUtility.ToJson(new ExchangeRequest { gameAuthCode = code });

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                ExchangeResponse res = JsonUtility.FromJson<ExchangeResponse>(request.downloadHandler.text);
                if (res != null && res.action && res.data != null)
                {
                    UserId = res.data.userId;
                    Username = res.data.username;
                    GameToken = res.data.gameToken;
                    Debug.Log($"[GameAuth] ✅ Authenticated as: {Username} ({UserId})");
                }
                else
                {
                    Debug.LogError($"[GameAuth] ❌ Token exchange rejected: {(res != null ? res.message : "No message")}");
                }
            }
            else
            {
                Debug.LogError($"[GameAuth] ❌ Network Error during login: {request.error}");
            }
        }
    }

    public void SubmitScore(int score, string scoreStr, string seed, Action<bool, string> onComplete = null)
    {
        if (!IsAuthenticated)
        {
            onComplete?.Invoke(false, "Player is not authenticated. Please paste your login code.");
            return;
        }

        StartCoroutine(SubmitScoreCoroutine(score, scoreStr, seed, onComplete));
    }

    private IEnumerator SubmitScoreCoroutine(int score, string scoreStr, string seed, Action<bool, string> onComplete)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string signaturePayload = $"{UserId}:{score}:{timestamp}:{gameSecret}";
        string signature = ComputeSha256(signaturePayload);

        ScoreRequest payload = new ScoreRequest
        {
            gameId = this.gameId,
            score = score,
            scoreStr = scoreStr,
            seed = seed,
            timestamp = timestamp,
            gameToken = this.GameToken,
            signature = signature
        };

        string endpoint = $"{baseUrl}/game/score";
        string jsonBody = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                BaseResponse res = JsonUtility.FromJson<BaseResponse>(request.downloadHandler.text);
                
                if (res != null && res.action)
                {
                    Debug.Log($"[GameAuth] 🚀 Score of {scoreStr} submitted successfully!");
                    onComplete?.Invoke(true, "Score submitted.");
                }
                else
                {
                    string detailedError = res != null ? res.message : request.error;
                    if (res != null && res.errors != null && res.errors.Length > 0)
                    {
                        detailedError += "\nDetailed Errors:\n - " + string.Join("\n - ", res.errors);
                    }
                    onComplete?.Invoke(false, detailedError);
                }
            }
            else
            {
                onComplete?.Invoke(false, $"Network error: {request.error}");
            }
        }
    }

    private static string ComputeSha256(string rawData)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    #region Data Transfer Classes
    [Serializable] private class ExchangeRequest { public string gameAuthCode; }
    [Serializable] private class ExchangeResponse { public bool action; public ExchangeData data; public string message; }
    [Serializable] private class ExchangeData { public string userId; public string username; public string gameToken; }
    [Serializable] private class ScoreRequest { public string gameId; public int score; public string scoreStr; public string seed; public long timestamp; public string gameToken; public string signature; }
    [Serializable] private class BaseResponse { public bool action; public string message; public string[] errors; }
    #endregion
}