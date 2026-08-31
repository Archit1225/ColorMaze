using Cinemachine;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public AudioSource audioSource;
    public AudioClip winAudio;
    [SerializeField] private CinemachineVirtualCamera cinemachine;

    private int totalTiles = 0;
    private int paintedTiles = 0;
    private int totalSwipes = 0;

    private float timeTaken = 0;
    private bool startTimer;
    private bool playerWon = false;


    [SerializeField] private GameAuthManager gameAuthManager;

    /*[DllImport("__Internal")]
    private static extern void ExportScoreToWeb(string score, int seed);

    public void OnLevelComplete(string finalScore, int levelSeed)
    {
        // This only executes in the final WebGL browser build
        #if UNITY_WEBGL && !UNITY_EDITOR
            ExportScoreToWeb(finalScore, levelSeed);
        #endif

        Debug.Log("Score exported to the web!");
    */
    private void Awake()
    {
        gameAuthManager = GameObject.Find("ScoreManager").GetComponent<GameAuthManager>();
        cinemachine = GameObject.Find("CM Vcam").GetComponent<CinemachineVirtualCamera>();
        // Set up the Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    private void Update()
    {
        if (startTimer) {
            timeTaken += Time.deltaTime;  
            UI_Controller.Instance.SetTimeText(timeTaken);
        }
    }

    public void SetVirtualCamera(Transform playerTransform)
    {
        cinemachine.Follow = playerTransform;
    }
    public void ResetData()
    {
        totalTiles = 0;
        paintedTiles = 0;
        totalSwipes = 0;
        timeTaken = 0;
        UI_Controller.Instance.SetScoreText("");
        UI_Controller.Instance.SetSwipeText(totalSwipes);
        UI_Controller.Instance.SetTimeText(timeTaken);
        startTimer = false;
        playerWon = false;
}
    // Called by each tile when it spawns
    public void AddPathTile()
    {
        totalTiles++;
    }
    public void AddSwipes()
    {
        if (playerWon) { startTimer = false; return; }
        totalSwipes++;
        if(totalSwipes == 1) startTimer = true;
        UI_Controller.Instance.SetSwipeText(totalSwipes);
    }

    // Called by the player when they paint an unpainted tile
    public void TilePainted()
    {
        paintedTiles++;

        // Check for win condition
        if (paintedTiles >= totalTiles && totalTiles !=0)
        {
            GameManager.Instance.TriggerWin();
            startTimer = false;
        }
    }

    private void TriggerWin()
    {
        int score = (int)(100000 / (totalSwipes * timeTaken));
        UI_Controller.Instance.SetScoreText(score.ToString());
        playerWon = true;
        Debug.Log($"Maze Complete! You painted all tiles. Swipes Taken: {totalSwipes}. TimeTaken: {timeTaken}. Score: {score}");
        audioSource.PlayOneShot(winAudio);
        UI_Controller.Instance.EnableWinPanel();
        gameAuthManager.SubmitScore(score, "pts", UI_Controller.Instance.GetSeed(), (success, message) =>
        {
            if (success)
            {
                Debug.Log("High score successfully posted to leaderboard!");
                //UI_Controller.Instance.SetScoreText("324234");
            }
            else
            {
                Debug.LogWarning($"Failed to post score: {message}");
                //UI_Controller.Instance.SetSwipeText(10000000);
            }
        }
        );
        //ScoreManager.Instance.SaveAndSendScore("Token", "Test1", score, int.Parse(UI_Controller.Instance.GetSeed()));
    }
}