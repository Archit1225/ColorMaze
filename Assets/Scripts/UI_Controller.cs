using UnityEngine;
using TMPro;

public class UI_Controller : MonoBehaviour
{
    public TMP_InputField seedInput;
    public TMP_Text timeText;
    public TMP_Text swipesText;
    public TMP_Text scoreText;

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
        string seedToPlay = seedInput.text;
        puzzleMaker.GenerateFromSeed(int.Parse(seedToPlay));
    }

    public void GenerateRandom()
    {
        puzzleMaker.ResetPuzzle();
    }

    public void SetTimeText(float timeTaken)
    {
        timeText.SetText($"Time Taken: {timeTaken.ToString("F2")}");
    }
    public void SetSwipeText(int totalSwipes)
    {
        swipesText.SetText($"Swipes: {totalSwipes}");
    }
    public void SetScoreText(string score)
    {
        scoreText.SetText($"Score: {score}");
    }
    public void SetSeedText(int currentSeed)
    {
        seedInput.text = currentSeed.ToString();
    }
}
