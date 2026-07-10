using Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] private CinemachineVirtualCamera cinemachine;

    private int totalTiles = 0;
    private int paintedTiles = 0;
    private int totalSwipes = 0;

    private float timeTaken = 0;
    private bool startTimer;
    private bool playerWon = false;

    private void Awake()
    {
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
        if (playerWon) return;
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
            TriggerWin();
            startTimer = false;
        }
    }

    private void TriggerWin()
    {
        string score = (10000 / (totalSwipes * timeTaken)).ToString("F2");
        UI_Controller.Instance.SetScoreText(score);
        playerWon = true;
        Debug.Log($"Maze Complete! You painted all tiles. Swipes Taken: {totalSwipes}. TimeTaken: {timeTaken}. Score: {score}");
    }
}