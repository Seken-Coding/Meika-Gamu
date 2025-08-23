// FruitProgression.cs - Manages fruit types and progression
using UnityEngine;

[System.Serializable]
public class FruitData
{
    public string name;
    public GameObject prefab;
    public int scoreValue;
    public Color debugColor = Color.white;
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float spawnWeight = 1f; // Higher = more likely to spawn
}

[CreateAssetMenu(fileName = "FruitProgression", menuName = "Game/Fruit Progression")]
public class FruitProgressionSO : ScriptableObject
{
    [Header("Fruit Levels (0 = smallest)")]
    public FruitData[] fruitLevels = new FruitData[7];
    
    [Header("Spawn Settings")]
    public int maxSpawnLevel = 3; // Only spawn levels 0-3 initially
    
    [Header("Spawn Weights by Game Progress")]
    public AnimationCurve spawnWeightCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);
    
    public FruitData GetFruitData(int level)
    {
        if (level < 0 || level >= fruitLevels.Length) return null;
        return fruitLevels[level];
    }
    
    public GameObject GetFruitPrefab(int level)
    {
        var data = GetFruitData(level);
        return data?.prefab;
    }
    
    public int GetScoreValue(int level)
    {
        var data = GetFruitData(level);
        return data?.scoreValue ?? 0;
    }
    
    public int GetRandomSpawnLevel()
    {
        // Create weighted array
        float totalWeight = 0f;
        for (int i = 0; i <= maxSpawnLevel && i < fruitLevels.Length; i++)
        {
            totalWeight += fruitLevels[i].spawnWeight;
        }
        
        // Random selection
        float random = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        for (int i = 0; i <= maxSpawnLevel && i < fruitLevels.Length; i++)
        {
            currentWeight += fruitLevels[i].spawnWeight;
            if (random <= currentWeight)
                return i;
        }
        
        return 0; // Fallback
    }
}