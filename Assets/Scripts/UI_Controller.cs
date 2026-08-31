using UnityEngine;
using TMPro;

public class UI_Controller : MonoBehaviour
{
    public TMP_InputField seedInputIF;
    public TMP_Text timeText;
    public TMP_Text timeText2;
    public TMP_Text swipesText;
    public TMP_Text swipesText2;
    public TMP_Text scoreText;
    public TMP_Text seedDisplayText;
    public GameObject winPanel;

    [SerializeField] private PuzzleMaker puzzleMaker;

    public static UI_Controller Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        // Set up the Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GenerateFromSeed()
    {
        string seedToPlay = seedInputIF.text;
        SetSeedText(int.Parse(seedToPlay));
        puzzleMaker.GenerateFromSeed(int.Parse(seedToPlay));
    }

    public void GenerateRandom()
    {
        puzzleMaker.GenerateRandomPuzzle();
    }

    public void GenerateMaze()
    {
        if (seedInputIF.text == "") GenerateRandom();
        else GenerateFromSeed();
        DisableWinPanel();
    }

    public void SetTimeText(float timeTaken)
    {
        timeText.SetText($"Time Taken: {timeTaken.ToString("F2")}");
        timeText2.SetText($"Time Taken: {timeTaken.ToString("F2")}");
    }
    public void SetSwipeText(int totalSwipes)
    {
        swipesText.SetText($"Swipes: {totalSwipes}");
        swipesText2.SetText($"Swipes: {totalSwipes}");
    }
    public void SetScoreText(string score)
    {
        scoreText.SetText($"Score: {score}");
    }
    public void SetSeedText(int currentSeed)
    {
        //seedInput.text = currentSeed.ToString();
        seedDisplayText.text = $"Seed: {currentSeed.ToString()}";
    }

    public void EnableWinPanel()
    {
        seedInputIF.text = "";
        winPanel.SetActive(true);
    }
    public void DisableWinPanel()
    {
        winPanel.SetActive(false);
    }

    public string GetSeed()
    {
        return seedInputIF.text;
    }
}
