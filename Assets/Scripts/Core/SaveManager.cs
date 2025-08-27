// SaveManager.cs - Handles local save data using PlayerPrefs and JSON
using UnityEngine;
using System;

public class SaveManager : MonoBehaviour
{
    private const string GAME_DATA_KEY = "SuikaGameData";
    private const string CURRENT_SESSION_KEY = "CurrentSession";
    
    private GameData gameData;
    private GameSession currentSession;
    
    public static SaveManager Instance { get; private set; }
    
    // Events
    public event Action<GameData> OnDataLoaded;
    public event Action<GameData> OnDataSaved;
    public event Action<int> OnHighScoreChanged;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGameData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveGameData();
    }
    
    void OnApplicationQuit()
    {
        SaveGameData();
    }
    
    public void LoadGameData()
    {
        try
        {
            if (PlayerPrefs.HasKey(GAME_DATA_KEY))
            {
                string jsonData = PlayerPrefs.GetString(GAME_DATA_KEY);
                gameData = JsonUtility.FromJson<GameData>(jsonData);
                gameData.sessionCount++;
                gameData.lastPlayDate = DateTime.Now;
            }
            else
            {
                gameData = new GameData();
            }
            
            OnDataLoaded?.Invoke(gameData);
            Debug.Log($"Save data loaded. High Score: {gameData.highScore}, Sessions: {gameData.sessionCount}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load save data: {e.Message}");
            gameData = new GameData();
        }
    }
    
    public void SaveGameData()
    {
        if (gameData == null) return;
        
        try
        {
            string jsonData = JsonUtility.ToJson(gameData, true);
            PlayerPrefs.SetString(GAME_DATA_KEY, jsonData);
            PlayerPrefs.Save();
            
            OnDataSaved?.Invoke(gameData);
            Debug.Log("Save data saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e.Message}");
        }
    }
    
    public void StartNewGame()
    {
        currentSession = new GameSession
        {
            currentScore = 0,
            sessionStartTime = Time.time,
            isGameOver = false
        };
        
        gameData.totalGamesPlayed++;
        SaveGameData();
    }
    
    public void UpdateScore(int newScore)
    {
        if (currentSession != null)
        {
            currentSession.currentScore = newScore;
            
            if (newScore > gameData.highScore)
            {
                int oldHighScore = gameData.highScore;
                gameData.highScore = newScore;
                OnHighScoreChanged?.Invoke(newScore);
                Debug.Log($"New high score: {newScore}! (Previous: {oldHighScore})");
            }
        }
    }
    
    public void OnFruitMerged(int fruitLevel)
    {
        gameData.totalFruitsMerged++;
        
        if (fruitLevel >= 0 && fruitLevel < gameData.fruitLevelStats.Length)
        {
            gameData.fruitLevelStats[fruitLevel]++;
        }
    }
    
    public void EndGame(int finalScore)
    {
        if (currentSession == null) return;
        
        currentSession.isGameOver = true;
        float sessionTime = Time.time - currentSession.sessionStartTime;
        gameData.totalPlayTime += sessionTime;
        
        UpdateScore(finalScore);
        SaveGameData();
        
        Debug.Log($"Game ended. Final Score: {finalScore}, Session Duration: {sessionTime:F1}s");
    }
    
    public void UpdateSettings(float musicVolume, float sfxVolume)
    {
        gameData.musicVolume = musicVolume;
        gameData.sfxVolume = sfxVolume;
        SaveGameData();
    }
    
    public void CompleteTutorial()
    {
        gameData.tutorialCompleted = true;
        SaveGameData();
    }
    
    public void ClearAllData()
    {
        PlayerPrefs.DeleteKey(GAME_DATA_KEY);
        PlayerPrefs.DeleteKey(CURRENT_SESSION_KEY);
        PlayerPrefs.Save();
        
        gameData = new GameData();
        currentSession = null;
        
        Debug.Log("All save data cleared");
        OnDataLoaded?.Invoke(gameData);
    }
    
    // Getters
    public GameData GetGameData() => gameData;
    public GameSession GetCurrentSession() => currentSession;
    public int GetHighScore() => gameData?.highScore ?? 0;
    public bool IsTutorialCompleted() => gameData?.tutorialCompleted ?? false;
    public int GetTotalFruitsMerged() => gameData?.totalFruitsMerged ?? 0;
    public float GetTotalPlayTime() => gameData?.totalPlayTime ?? 0f;
    public int GetGamesPlayed() => gameData?.totalGamesPlayed ?? 0;
    public int[] GetFruitLevelStats() => gameData?.fruitLevelStats ?? new int[7];
}

// GameData.cs - Serializable game data structure
[System.Serializable]
public class GameData
{
    [Header("Score Data")]
    public int highScore = 0;
    public int totalGamesPlayed = 0;
    public int totalFruitsMerged = 0;
    public float totalPlayTime = 0f;
    public int[] fruitLevelStats = new int[7];
    
    [Header("Settings")]
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    public bool tutorialCompleted = false;
    
    [Header("Session Info")]
    public DateTime lastPlayDate;
    public int sessionCount = 0;
    
    public GameData()
    {
        lastPlayDate = DateTime.Now;
        fruitLevelStats = new int[7];
    }
}

// GameSession.cs - Current session data
[System.Serializable]
public class GameSession
{
    public int currentScore;
    public int nextFruitLevel;
    public float sessionStartTime;
    public bool isGameOver;
    
    [System.Serializable]
    public class FruitSaveData
    {
        public Vector3 position;
        public int fruitLevel;
        public Vector2 velocity;
    }
    
    public FruitSaveData[] activeFruits;
}

// StatisticsManager.cs - Handles statistics display and calculations
public class StatisticsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Text highScoreText;
    [SerializeField] private UnityEngine.UI.Text gamesPlayedText;
    [SerializeField] private UnityEngine.UI.Text fruitsMergedText;
    [SerializeField] private UnityEngine.UI.Text playTimeText;
    [SerializeField] private UnityEngine.UI.Text[] fruitLevelTexts = new UnityEngine.UI.Text[7];
    
    void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnDataLoaded += UpdateStatisticsDisplay;
            SaveManager.Instance.OnDataSaved += UpdateStatisticsDisplay;
            SaveManager.Instance.OnHighScoreChanged += OnHighScoreChanged;
        }
        
        UpdateStatisticsDisplay(SaveManager.Instance?.GetGameData());
    }
    
    void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnDataLoaded -= UpdateStatisticsDisplay;
            SaveManager.Instance.OnDataSaved -= UpdateStatisticsDisplay;
            SaveManager.Instance.OnHighScoreChanged -= OnHighScoreChanged;
        }
    }
    
    public void UpdateStatisticsDisplay(GameData data)
    {
        if (data == null) return;
        
        if (highScoreText != null)
            highScoreText.text = $"High Score: {data.highScore:N0}";
            
        if (gamesPlayedText != null)
            gamesPlayedText.text = $"Games Played: {data.totalGamesPlayed}";
            
        if (fruitsMergedText != null)
            fruitsMergedText.text = $"Fruits Merged: {data.totalFruitsMerged:N0}";
            
        if (playTimeText != null)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(data.totalPlayTime);
            playTimeText.text = $"Play Time: {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        
        // Update individual fruit level statistics
        for (int i = 0; i < fruitLevelTexts.Length && i < data.fruitLevelStats.Length; i++)
        {
            if (fruitLevelTexts[i] != null)
            {
                fruitLevelTexts[i].text = $"Level {i + 1}: {data.fruitLevelStats[i]}";
            }
        }
    }
    
    private void OnHighScoreChanged(int newHighScore)
    {
        // Could add special effects here for new high score
        Debug.Log($"New High Score Achieved: {newHighScore}!");
    }
    
    public float GetAverageScore()
    {
        var data = SaveManager.Instance?.GetGameData();
        if (data == null || data.totalGamesPlayed == 0) return 0f;
        
        return (float)data.highScore / data.totalGamesPlayed;
    }
    
    public float GetMergesPerGame()
    {
        var data = SaveManager.Instance?.GetGameData();
        if (data == null || data.totalGamesPlayed == 0) return 0f;
        
        return (float)data.totalFruitsMerged / data.totalGamesPlayed;
    }
}