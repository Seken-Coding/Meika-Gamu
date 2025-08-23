// ImprovedGameManager.cs - Replaces SimpleFruitTest with proper game management
using UnityEngine;
using UnityEngine.UI;

public class ImprovedGameManager : MonoBehaviour
{
    [Header("Game Setup")]
    [SerializeField] private FruitProgressionSO fruitProgression;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform gameOverLine;
    
    [Header("Game Settings")]
    [SerializeField] private float dropCooldown = 1f;
    [SerializeField] private float containerWidth = 8f;
    [SerializeField] private int gameOverGraceTime = 3;
    
    [Header("UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text nextFruitText;
    [SerializeField] private Image nextFruitPreview;
    [SerializeField] private Button restartButton;
    [SerializeField] private GameObject gameOverPanel;
    
    // Game State
    private int currentScore = 0;
    private int nextFruitLevel = 0;
    private bool gameOver = false;
    private bool canDrop = true;
    private int fruitsInScene = 0;
    
    // References
    private Camera mainCamera;
    
    public static ImprovedGameManager Instance { get; private set; }
    
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
        if (fruitProgression == null)
        {
            Debug.LogError("No FruitProgression ScriptableObject assigned!");
            return;
        }
        
        GenerateNextFruit();
        UpdateUI();
        
        // Setup restart button
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
            
        Debug.Log("Improved Game Manager ready! Click to drop fruits.");
    }
    
    void Update()
    {
        if (gameOver) return;
        
        // Drop fruit on click
        if (canDrop && Input.GetMouseButtonDown(0))
        {
            DropFruit();
        }
        
        // Check game over condition
        CheckGameOverCondition();
        
        // Debug controls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RestartGame();
        }
    }
    
    void DropFruit()
    {
        GameObject fruitPrefab = fruitProgression.GetFruitPrefab(nextFruitLevel);
        if (fruitPrefab == null)
        {
            Debug.LogError($"No prefab for fruit level {nextFruitLevel}!");
            return;
        }
        
        // Get drop position
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        worldPos.x = Mathf.Clamp(worldPos.x, -containerWidth/2f, containerWidth/2f);
        worldPos.y = spawnPoint.position.y;
        
        // Create fruit
        GameObject newFruit = Instantiate(fruitPrefab, worldPos, Quaternion.identity);
        
        // Setup the fruit controller
        ImprovedFruitController fruitController = newFruit.GetComponent<ImprovedFruitController>();
        if (fruitController == null)
        {
            fruitController = newFruit.AddComponent<ImprovedFruitController>();
        }
        fruitController.Initialize(nextFruitLevel, fruitProgression);
        
        fruitsInScene++;
        
        // Generate next fruit and start cooldown
        GenerateNextFruit();
        UpdateUI();
        
        canDrop = false;
        Invoke(nameof(ResetDropCooldown), dropCooldown);
    }
    
    void GenerateNextFruit()
    {
        nextFruitLevel = fruitProgression.GetRandomSpawnLevel();
    }
    
    void ResetDropCooldown()
    {
        canDrop = true;
    }
    
    public void OnFruitMerged(int fromLevel, Vector3 position)
    {
        // Update score
        int scoreGain = fruitProgression.GetScoreValue(fromLevel) * 2; // Double for merge bonus
        currentScore += scoreGain;
        
        // Create next level fruit
        int nextLevel = fromLevel + 1;
        GameObject nextFruitPrefab = fruitProgression.GetFruitPrefab(nextLevel);
        
        if (nextFruitPrefab != null)
        {
            GameObject newFruit = Instantiate(nextFruitPrefab, position, Quaternion.identity);
            ImprovedFruitController fruitController = newFruit.GetComponent<ImprovedFruitController>();
            if (fruitController == null)
            {
                fruitController = newFruit.AddComponent<ImprovedFruitController>();
            }
            fruitController.Initialize(nextLevel, fruitProgression);
        }
        else
        {
            // Max level reached! Big score bonus
            currentScore += 1000;
            Debug.Log("Max level fruit created! Bonus points!");
        }
        
        fruitsInScene--; // Two fruits merged into one, net -1
        UpdateUI();
    }
    
    public void OnFruitDestroyed()
    {
        fruitsInScene--;
    }
    
    void CheckGameOverCondition()
    {
        // Check if any fruits are above the game over line
        ImprovedFruitController[] allFruits = FindObjectsOfType<ImprovedFruitController>();
        
        foreach (var fruit in allFruits)
        {
            if (fruit.transform.position.y > gameOverLine.position.y)
            {
                // Start game over countdown
                if (!IsInvoking(nameof(TriggerGameOver)))
                {
                    Invoke(nameof(TriggerGameOver), gameOverGraceTime);
                }
                return;
            }
        }
        
        // No fruits above line, cancel game over
        if (IsInvoking(nameof(TriggerGameOver)))
        {
            CancelInvoke(nameof(TriggerGameOver));
        }
    }
    
    void TriggerGameOver()
    {
        gameOver = true;
        Debug.Log($"Game Over! Final Score: {currentScore}");
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
    
    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
            
        if (nextFruitText != null)
        {
            var nextFruitData = fruitProgression.GetFruitData(nextFruitLevel);
            nextFruitText.text = $"Next: {nextFruitData?.name ?? "Unknown"}";
        }
        
        // Update next fruit preview if you have an image
        if (nextFruitPreview != null)
        {
            var nextFruitPrefab = fruitProgression.GetFruitPrefab(nextFruitLevel);
            if (nextFruitPrefab != null)
            {
                SpriteRenderer spriteRenderer = nextFruitPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    nextFruitPreview.sprite = spriteRenderer.sprite;
                }
            }
        }
    }
    
    public void RestartGame()
    {
        // Clear all fruits
        ImprovedFruitController[] allFruits = FindObjectsOfType<ImprovedFruitController>();
        foreach (var fruit in allFruits)
        {
            Destroy(fruit.gameObject);
        }
        
        // Reset game state
        currentScore = 0;
        fruitsInScene = 0;
        gameOver = false;
        canDrop = true;
        
        GenerateNextFruit();
        UpdateUI();
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        // Cancel any pending game over
        CancelInvoke(nameof(TriggerGameOver));
    }
    
    void OnDrawGizmos()
    {
        // Draw spawn area
        Gizmos.color = Color.green;
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.up * 5f;
        Gizmos.DrawWireCube(spawnPos, new Vector3(containerWidth, 0.5f, 0));
        
        // Draw game over line
        if (gameOverLine != null)
        {
            Gizmos.color = Color.red;
            Vector3 linePos = gameOverLine.position;
            Gizmos.DrawLine(new Vector3(-containerWidth/2f, linePos.y, 0), 
                           new Vector3(containerWidth/2f, linePos.y, 0));
        }
    }
}
