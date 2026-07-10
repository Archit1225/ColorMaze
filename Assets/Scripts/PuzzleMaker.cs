using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PuzzleMaker : MonoBehaviour
{
    public enum TileType
    {
        Empty,
        Wall,
        Path
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pathPrefab;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    private GameObject activePlayer; // Keep track of the player so we don't spawn duplicates

    [Header("Maze Config")]
    [SerializeField] private int width = 12;
    [SerializeField] private int height = 12;

    [Range(0f, 1f)]
    [SerializeField] private float wallChance = 0.2f;

    [Header("Camera & Scaling")]
    [Tooltip("Reference to your main orthographic camera.")]
    [SerializeField] private Camera mainCamera;
    [Range(0f, 1f)][SerializeField] private float safeAreaLeft = 0.25f;
    [Range(0f, 1f)][SerializeField] private float safeAreaRight = 1f;
    [Range(0f, 1f)][SerializeField] private float safeAreaTop = 1f;
    [Range(0f, 1f)][SerializeField] private float safeAreaBottom = 0f;

    [Tooltip("How many path segments the generator will attempt to draw.")]
    [SerializeField] private int maxSegments = 40;

    [Tooltip("The minimum number of tiles a path must travel before a wall can randomly spawn.")]
    [SerializeField] private int minSegmentLength = 2;

    [Tooltip("Empty space (in Unity units) to leave around the edges of the screen.")]
    [SerializeField] private float padding = 0.5f;

    [Header("Instant Difficulty Estimator")]
    [Tooltip("Target difficulty score (e.g., Easy = 10, Med = 25, Hard = 40)")]
    [SerializeField] private int targetDifficultyScore = 20;
    [SerializeField] private int maxRegenRetries = 100;

    [Header("Seed System")]
    [Tooltip("If true, a random seed is chosen every time. If false, it uses Custom Seed.")]
    public bool useRandomSeed = true;
    [Tooltip("Type a number here to play a specific level layout.")]
    public int customSeed = 12345;

    [Tooltip("The seed currently being played. (Read Only)")]
    [SerializeField] public int currentSeed;

    private TileType[,] grid;
    private List<Vector2Int> stoppingPoints;

    // We store a reference to the active maze so we can delete it later
    private GameObject currentMazeContainer;

    private void Start()
    {
        GenerateNewMaze();
    }
    //UI Functions
    public void GenerateRandomPuzzle()
    {
        useRandomSeed = true;
        GenerateNewMaze();
    }

    public void GenerateFromSeed(int seedToPlay)
    {
        useRandomSeed = false;
        customSeed = seedToPlay;
        GenerateNewMaze();
    }
    private void GenerateNewMaze()
    {
        //Initialize the Seed
        if (useRandomSeed)
        {
            // Pick a random 6-digit number to act as the seed
            currentSeed = Random.Range(100000, 999999);
            UI_Controller.Instance.SetSeedText(currentSeed);
        }
        else
        {
            currentSeed = customSeed;
        }

        // Lock the random number generator to this specific seed
        Random.InitState(currentSeed);

        //Clean up the old maze if it exists
        if (currentMazeContainer != null)
        {
            Destroy(currentMazeContainer);
        }

        //Difficulty filter
        int attempts = 0;
        int currentScore = 0;
        bool validLevel = false;

        while (!validLevel && attempts < maxRegenRetries)
        {
            GenerateMazeData();
            currentScore = CalculateDifficultyScore();

            if (currentScore >= targetDifficultyScore)
            {
                validLevel = true;
            }
            attempts++;
        }

        if (validLevel)
            Debug.Log($"Success! Hit difficulty {currentScore} in {attempts} attempts.");
        else
            Debug.LogWarning($"Best generated difficulty was {currentScore} after {maxRegenRetries} attempts.");
        BuildMazeVisuals();

    }

    private void GenerateMazeData()
    {
        grid = new TileType[width, height];
        stoppingPoints = new List<Vector2Int>();

        //Generates a boundary wall first
        for (int x = 0; x < width; x++)
        {
            grid[x, 0] = TileType.Wall;
            grid[x, height - 1] = TileType.Wall;
        }
        for (int y = 0; y < height; y++)
        {
            grid[0, y] = TileType.Wall;
            grid[width - 1, y] = TileType.Wall;
        }

        //Initializes Player's starting position
        int currentX = 1;
        int currentY = height - 2;

        grid[currentX, currentY] = TileType.Path;
        stoppingPoints.Add(new Vector2Int(currentX, currentY));

        bool movingVertical = true;

        for (int i = 0; i < maxSegments; i++)
        {
            //If the block had moved vertically it makes sure the next direction is horizontal
            Vector2Int dir1 = movingVertical ? Vector2Int.up : Vector2Int.right;
            Vector2Int dir2 = movingVertical ? Vector2Int.down : Vector2Int.left;

            bool canGoDir1 = grid[currentX + dir1.x, currentY + dir1.y] != TileType.Wall;
            bool canGoDir2 = grid[currentX + dir2.x, currentY + dir2.y] != TileType.Wall;

            //If both the direction is blocked by a wall then it randomly chooses previous stopping points
            if (!canGoDir1 && !canGoDir2)
            {
                Vector2Int randomStop = stoppingPoints[Random.Range(0, stoppingPoints.Count)];
                currentX = randomStop.x;
                currentY = randomStop.y;
                movingVertical = Random.value > 0.5f;
                continue;
            }

            Vector2Int chosenDir;
            if (canGoDir1 && canGoDir2)
                chosenDir = Random.value > 0.5f ? dir1 : dir2;
            else
                chosenDir = canGoDir1 ? dir1 : dir2;

            int tilesMoved = 0;

            //Moves until it encounters a wall or it makes a wall
            while (true)
            {
                int nextX = currentX + chosenDir.x;
                int nextY = currentY + chosenDir.y;

                if (grid[nextX, nextY] == TileType.Wall)
                {
                    break;
                }

                bool isAlreadyPath = (grid[nextX, nextY] == TileType.Path);

                if (tilesMoved >= minSegmentLength && Random.value < wallChance)
                {
                    if (!isAlreadyPath)
                    {
                        grid[nextX, nextY] = TileType.Wall;
                        break;
                    }
                }

                currentX = nextX;
                currentY = nextY;
                grid[currentX, currentY] = TileType.Path;
                tilesMoved++;
            }

            stoppingPoints.Add(new Vector2Int(currentX, currentY));
            movingVertical = !movingVertical;
        }

        //If there are still some empty tiles left then wall is generated over them
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == TileType.Empty)
                {
                    grid[x, y] = TileType.Wall;
                }
            }
        }
    }

    private int CalculateDifficultyScore()
    {
        int totalPathTiles = 0;
        int deadEnds = 0;
        int corners = 0;
        int flatWallChoices = 0;

        Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (grid[x, y] == TileType.Path)
                {
                    totalPathTiles++;
                    int openSides = 0;

                    foreach (Vector2Int dir in neighbors)
                    {
                        if (grid[x + dir.x, y + dir.y] == TileType.Path) openSides++;
                    }

                    // Score based on sliding mechanics!
                    if (openSides == 1)
                    {
                        deadEnds++; // Player is forced to stop and reverse
                    }
                    else if (openSides == 2)
                    {
                        // To ensure it's an actual corner and not just a straight hallway
                        bool upDown = grid[x, y + 1] == TileType.Path && grid[x, y - 1] == TileType.Path;
                        bool leftRight = grid[x + 1, y] == TileType.Path && grid[x - 1, y] == TileType.Path;

                        if (!upDown && !leftRight)
                        {
                            corners++; // Player stops and must turn
                        }
                    }
                    else if (openSides == 3)
                    {
                        flatWallChoices++; // Player hits a flat wall and must choose left or right
                    }
                }
            }
        }

        // Tweak these values! 
        // Dead ends require the most slides to paint. 
        // Flat wall choices (3-way stops) require the most mental planning.
        int difficultyScore = (deadEnds * 4) + (flatWallChoices * 3) + (corners * 1) + (totalPathTiles / 10);

        return difficultyScore;
    }

    private void BuildMazeVisuals()
    {
        // Create the new container and save it to our variable
        currentMazeContainer = new GameObject("MazeGrid");
        GameManager.Instance.ResetData();

        // Spawn Player and make it a child of the MazeGrid so it gets deleted on reset
        GameObject player = Instantiate(playerPrefab, new Vector3(1, height - 2, 0), Quaternion.identity);
        player.transform.SetParent(currentMazeContainer.transform);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 spawnPosition = new Vector3(x, y, 0);
                GameObject prefabToSpawn = (grid[x, y] == TileType.Wall) ? wallPrefab : pathPrefab;

                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, currentMazeContainer.transform);
            }
        }

        if (mainCamera != null && mainCamera.orthographic)
        {
            // 1. Convert the Viewport percentages (0.0 to 1.0) into actual World Space coordinates
            Vector3 bottomLeftWorld = mainCamera.ViewportToWorldPoint(new Vector3(safeAreaLeft, safeAreaBottom, 0f));
            Vector3 topRightWorld = mainCamera.ViewportToWorldPoint(new Vector3(safeAreaRight, safeAreaTop, 0f));

            // 2. Calculate how much physical world space we have inside that designated box
            float safeWidth = (topRightWorld.x - bottomLeftWorld.x) - padding;
            float safeHeight = (topRightWorld.y - bottomLeftWorld.y) - padding;

            // 3. Calculate the scale required to fit the maze inside the new safe area
            float scaleX = safeWidth / width;
            float scaleY = safeHeight / height;

            float finalScale = Mathf.Min(scaleX, scaleY);
            finalScale = Mathf.Min(1f, finalScale); // Prevent it from stretching too large
            finalScale = Mathf.Clamp(finalScale, 0.01f, 1f);

            // Apply the scale to the parent container
            currentMazeContainer.transform.localScale = new Vector3(finalScale, finalScale, 1f);

            // 4. Find the mathematical center of our custom Safe Area
            float safeCenterX = (bottomLeftWorld.x + topRightWorld.x) / 2f;
            float safeCenterY = (bottomLeftWorld.y + topRightWorld.y) / 2f;

            // 5. Find the center of the scaled maze
            float mazeCenterX = ((width - 1) / 2f) * finalScale;
            float mazeCenterY = ((height - 1) / 2f) * finalScale;

            // 6. Move the grid so its center perfectly aligns with the Safe Area's center
            currentMazeContainer.transform.position = new Vector3(safeCenterX - mazeCenterX, safeCenterY - mazeCenterY, 0f);
        }
        else
        {
            Debug.LogWarning("Camera is missing or not set to Orthographic!");
        }
    }
}