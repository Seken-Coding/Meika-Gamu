// GameManager.cs - Main game controller
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Setup")]
    [SerializeField] private FruitProgressionSO fruitProgression;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform gameOverLine;
    
    [Header("Game Settings")]
    [SerializeField] private float dropCooldown = 1f;
    [SerializeField] private float containerWidth = 8f;
    [SerializeField] private float gameOverGraceTime = 3f;
    
    [Header("UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text nextFruitText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Image nextFruitPreview;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    
    // Game State
    private int currentScore = 0;
    private int nextFruitLevel = 0;
    private bool gameOver = false;
    private bool gamePaused = false;
    private bool canDrop = true;
    private int fruitsInScene = 0;
    
    // References
    private Camera mainCamera;
    
    public static GameManager Instance { get; private set; }
    
    // Events
    public System.Action<int> OnScoreChanged;
    public System.Action<bool> OnGameStateChanged;
    public System.Action OnGameOver;
    
    // Properties
    public int CurrentScore => currentScore;
    public bool IsGameOver => gameOver;
    public bool IsGamePaused => gamePaused;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        ValidateSetup();
        InitializeGame();
        SetupUI();
        
        Debug.Log("Game Manager initialized. Click/tap to drop fruits!");
    }
    
    void Update()
    {
        if (gameOver || gamePaused) return;
        
        HandleInput();
        CheckGameOverCondition();
        
        #if UNITY_EDITOR
        HandleDebugInput();
        #endif
    }
    
    void ValidateSetup()
    {
        if (fruitProgression == null)
        {
            Debug.LogError("FruitProgression ScriptableObject not assigned!");
            enabled = false;
            return;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not assigned!");
            enabled = false;
            return;
        }
    }
    
    void InitializeGame()
    {
        GenerateNextFruit();
        UpdateUI();
        
        // Initialize save system
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame();
        }
        
        // Set initial game state
        currentScore = 0;
        fruitsInScene = 0;
        gameOver = false;
        gamePaused = false;
        canDrop = true;
        
        // Hide UI panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }
    
    void SetupUI()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
            
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
        
        // Load high score display
        UpdateHighScoreDisplay();
    }
    
    void HandleInput()
    {
        if (!canDrop) return;
        
        bool inputDetected = false;
        Vector3 worldPosition = Vector3.zero;
        
        // Mouse input
        if (Input.GetMouseButtonDown(0))
        {
            worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            inputDetected = true;
        }
        // Touch input
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            worldPosition = mainCamera.ScreenToWorldPoint(Input.GetTouch(0).position);
            inputDetected = true;
        }
        
        if (inputDetected)
        {
            worldPosition.z = 0;
            DropFruit(worldPosition);
        }
    }
    
    void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.R)) RestartGame();
        if (Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (Input.GetKeyDown(KeyCode.G)) TriggerGameOver();
    }
    
    void DropFruit(Vector3 inputPosition)
    {
        GameObject fruitPrefab = fruitProgression.GetFruitPrefab(nextFruitLevel);
        if (fruitPrefab == null)
        {
            Debug.LogError($"No prefab assigned for fruit level {nextFruitLevel}!");
            return;
        }
        
        // Calculate drop position
        Vector3 dropPosition = CalculateDropPosition(inputPosition);
        
        // Instantiate fruit
        GameObject newFruit = Instantiate(fruitPrefab, dropPosition, Quaternion.identity);
        
        // Initialize fruit controller
        FruitController fruitController = newFruit.GetComponent<FruitController>();
        if (fruitController == null)
        {
            fruitController = newFruit.AddComponent<FruitController>();
        }
        fruitController.Initialize(nextFruitLevel, fruitProgression);
        
        // Update game state
        fruitsInScene++;
        GenerateNextFruit();
        UpdateUI();
        
        // Play audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFruitDrop();
        }
        
        // Start drop cooldown
        StartDropCooldown();
    }
    
    Vector3 CalculateDropPosition(Vector3 inputPosition)
    {
        Vector3 dropPos = inputPosition;
        dropPos.x = Mathf.Clamp(dropPos.x, -containerWidth / 2f, containerWidth / 2f);
        dropPos.y = spawnPoint.position.y;
        dropPos.z = 0;
        return dropPos;
    }
    
    void StartDropCooldown()
    {
        canDrop = false;
        Invoke(nameof(ResetDropCooldown), dropCooldown);
    }
    
    void ResetDropCooldown()
    {
        canDrop = true;
    }
    
    void GenerateNextFruit()
    {
        nextFruitLevel = fruitProgression.GetRandomSpawnLevel();
    }
    
    public void OnFruitMerged(int fromLevel, Vector3 position)
    {
        // Calculate score
        int baseScore = fruitProgression.GetScoreValue(fromLevel);
        int levelBonus = (fromLevel + 1) * 10;
        int scoreGain = baseScore + levelBonus;
        
        AddScore(scoreGain);
        
        // Update save system
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnFruitMerged(fromLevel);
        }
        
        // Create next level fruit
        CreateMergedFruit(fromLevel + 1, position);
        
        // Play merge audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFruitMerge();
        }
        
        // Two fruits became one
        fruitsInScene--;
    }
    
    void CreateMergedFruit(int level, Vector3 position)
    {
        GameObject nextLevelPrefab = fruitProgression.GetFruitPrefab(level);
        
        if (nextLevelPrefab != null)
        {
            GameObject newFruit = Instantiate(nextLevelPrefab, position, Quaternion.identity);
            FruitController fruitController = newFruit.GetComponent<FruitController>();
            if (fruitController == null)
            {
                fruitController = newFruit.AddComponent<FruitController>();
            }
            fruitController.Initialize(level, fruitProgression);
        }
        else
        {
            // Maximum level reached - big bonus!
            AddScore(1000);
            Debug.Log("Maximum fruit level achieved! Bonus points!");
        }
    }
    
    public void OnFruitDestroyed()
    {
        fruitsInScene = Mathf.Max(0, fruitsInScene - 1);
    }
    
    void AddScore(int points)
    {
        currentScore += points;
        
        // Update save system
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateScore(currentScore);
        }
        
        OnScoreChanged?.Invoke(currentScore);
        UpdateUI();
    }
    
    void CheckGameOverCondition()
    {
        // Find fruits above the game over line
        FruitController[] allFruits = FindObjectsOfType<FruitController>();
        bool fruitAboveLine = false;
        
        foreach (var fruit in allFruits)
        {
            if (fruit.transform.position.y > gameOverLine.position.y)
            {
                fruitAboveLine = true;
                break;
            }
        }
        
        if (fruitAboveLine)
        {
            if (!IsInvoking(nameof(TriggerGameOver)))
            {
                Invoke(nameof(TriggerGameOver), gameOverGraceTime);
            }
        }
        else
        {
            if (IsInvoking(nameof(TriggerGameOver)))
            {
                CancelInvoke(nameof(TriggerGameOver));
            }
        }
    }
    
    void TriggerGameOver()
    {
        if (gameOver) return;
        
        gameOver = true;
        OnGameOver?.Invoke();
        
        // Update save system
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.EndGame(currentScore);
        }
        
        // Play game over audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }
        
        // Show game over UI
        ShowGameOverPanel();
        
        Debug.Log($"Game Over! Final Score: {currentScore}");
    }
    
    void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score: {currentScore:N0}";
            }
        }
    }
    
    public void RestartGame()
    {
        // Clear existing fruits
        FruitController[] allFruits = FindObjectsOfType<FruitController>();
        foreach (var fruit in allFruits)
        {
            Destroy(fruit.gameObject);
        }
        
        // Cancel any pending invokes
        CancelInvoke();
        
        // Reset game state
        InitializeGame();
        
        Debug.Log("Game restarted");
    }
    
    public void TogglePause()
    {
        if (gameOver) return;
        
        gamePaused = !gamePaused;
        Time.timeScale = gamePaused ? 0f : 1f;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(gamePaused);
        }
        
        OnGameStateChanged?.Invoke(gamePaused);
    }
    
    public void ResumeGame()
    {
        if (!gamePaused) return;
        
        gamePaused = false;
        Time.timeScale = 1f;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        OnGameStateChanged?.Invoke(false);
    }
    
    void UpdateUI()
    {
        UpdateScoreDisplay();
        UpdateNextFruitDisplay();
        UpdateHighScoreDisplay();
    }
    
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore:N0}";
        }
    }
    
    void UpdateNextFruitDisplay()
    {
        if (nextFruitText != null)
        {
            var fruitData = fruitProgression.GetFruitData(nextFruitLevel);
            string fruitName = fruitData?.name ?? "Unknown";
            nextFruitText.text = $"Next: {fruitName}";
        }
        
        if (nextFruitPreview != null)
        {
            var fruitPrefab = fruitProgression.GetFruitPrefab(nextFruitLevel);
            if (fruitPrefab != null)
            {
                SpriteRenderer spriteRenderer = fruitPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    nextFruitPreview.sprite = spriteRenderer.sprite;
                    nextFruitPreview.enabled = true;
                }
                else
                {
                    nextFruitPreview.enabled = false;
                }
            }
        }
    }
    
    void UpdateHighScoreDisplay()
    {
        if (highScoreText != null && SaveManager.Instance != null)
        {
            int highScore = SaveManager.Instance.GetHighScore();
            highScoreText.text = $"Best: {highScore:N0}";
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw spawn area
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Vector3 center = spawnPoint.position;
            Vector3 size = new Vector3(containerWidth, 0.5f, 0);
            Gizmos.DrawWireCube(center, size);
        }
        
        // Draw game over line
        if (gameOverLine != null)
        {
            Gizmos.color = Color.red;
            Vector3 start = new Vector3(-containerWidth / 2f, gameOverLine.position.y, 0);
            Vector3 end = new Vector3(containerWidth / 2f, gameOverLine.position.y, 0);
            Gizmos.DrawLine(start, end);
        }
        
        // Draw container bounds
        Gizmos.color = Color.blue;
        Vector3 leftBound = new Vector3(-containerWidth / 2f, 0, 0);
        Vector3 rightBound = new Vector3(containerWidth / 2f, 0, 0);
        Gizmos.DrawLine(leftBound + Vector3.up * 10, leftBound + Vector3.down * 10);
        Gizmos.DrawLine(rightBound + Vector3.up * 10, rightBound + Vector3.down * 10);
    }
}