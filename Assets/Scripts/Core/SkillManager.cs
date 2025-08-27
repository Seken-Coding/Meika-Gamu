// SkillManager.cs - Manages skill cooldowns and execution
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    [Header("Skill Cooldowns")]
    [SerializeField] private float shakeCooldown = 30f;
    [SerializeField] private float waterFillCooldown = 45f;
    
    [Header("UI References")]
    [SerializeField] private Button shakeButton;
    [SerializeField] private Button waterButton;
    [SerializeField] private Image shakeCooldownOverlay;
    [SerializeField] private Image waterCooldownOverlay;
    [SerializeField] private Text shakeCooldownText;
    [SerializeField] private Text waterCooldownText;
    
    // Skill instances
    private ShakeSkill shakeSkill;
    private WaterFillSkill waterFillSkill;
    
    // Cooldown tracking
    private float shakeLastUsed = -999f;
    private float waterLastUsed = -999f;
    
    public static SkillManager Instance { get; private set; }
    
    // Events
    public System.Action<SkillType> OnSkillUsed;
    public System.Action<SkillType, float> OnCooldownChanged;
    
    public enum SkillType
    {
        Shake,
        WaterFill
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeSkills();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        SetupUI();
    }
    
    void Update()
    {
        UpdateCooldowns();
        UpdateUI();
    }
    
    void InitializeSkills()
    {
        shakeSkill = gameObject.GetComponent<ShakeSkill>() ?? gameObject.AddComponent<ShakeSkill>();
        waterFillSkill = gameObject.GetComponent<WaterFillSkill>() ?? gameObject.AddComponent<WaterFillSkill>();
    }
    
    void SetupUI()
    {
        if (shakeButton != null)
            shakeButton.onClick.AddListener(() => UseSkill(SkillType.Shake));
            
        if (waterButton != null)
            waterButton.onClick.AddListener(() => UseSkill(SkillType.WaterFill));
    }
    
    public bool UseSkill(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Shake:
                if (CanUseShake())
                {
                    shakeLastUsed = Time.time;
                    shakeSkill.Execute();
                    OnSkillUsed?.Invoke(SkillType.Shake);
                    return true;
                }
                break;
                
            case SkillType.WaterFill:
                if (CanUseWater())
                {
                    waterLastUsed = Time.time;
                    waterFillSkill.Execute();
                    OnSkillUsed?.Invoke(SkillType.WaterFill);
                    return true;
                }
                break;
        }
        return false;
    }
    
    public bool CanUseShake() => Time.time >= shakeLastUsed + shakeCooldown;
    public bool CanUseWater() => Time.time >= waterLastUsed + waterFillCooldown && !waterFillSkill.IsActive;
    
    public float GetCooldownRemaining(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Shake:
                return Mathf.Max(0, (shakeLastUsed + shakeCooldown) - Time.time);
            case SkillType.WaterFill:
                return Mathf.Max(0, (waterLastUsed + waterFillCooldown) - Time.time);
            default:
                return 0f;
        }
    }
    
    void UpdateCooldowns()
    {
        OnCooldownChanged?.Invoke(SkillType.Shake, GetCooldownRemaining(SkillType.Shake));
        OnCooldownChanged?.Invoke(SkillType.WaterFill, GetCooldownRemaining(SkillType.WaterFill));
    }
    
    void UpdateUI()
    {
        UpdateSkillButton(SkillType.Shake, shakeButton, shakeCooldownOverlay, shakeCooldownText);
        UpdateSkillButton(SkillType.WaterFill, waterButton, waterCooldownOverlay, waterCooldownText);
    }
    
    void UpdateSkillButton(SkillType skillType, Button button, Image overlay, Text cooldownText)
    {
        if (button == null) return;
        
        bool canUse = (skillType == SkillType.Shake) ? CanUseShake() : CanUseWater();
        float cooldownRemaining = GetCooldownRemaining(skillType);
        float maxCooldown = (skillType == SkillType.Shake) ? shakeCooldown : waterFillCooldown;
        
        // Update button interactability
        button.interactable = canUse;
        
        // Update cooldown overlay
        if (overlay != null)
        {
            float progress = cooldownRemaining / maxCooldown;
            overlay.fillAmount = progress;
        }
        
        // Update cooldown text
        if (cooldownText != null)
        {
            if (cooldownRemaining > 0)
            {
                cooldownText.text = cooldownRemaining.ToString("F0");
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
}

// SkillBase.cs - Abstract base class for all skills
public abstract class SkillBase : MonoBehaviour
{
    [Header("Base Skill Settings")]
    [SerializeField] protected float duration = 2f;
    [SerializeField] protected AudioClip skillSound;
    
    public bool IsActive { get; protected set; }
    public abstract void Execute();
    
    protected virtual void PlaySound()
    {
        if (skillSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(skillSound);
        }
    }
}

// ShakeSkill.cs - Shake skill implementation
public class ShakeSkill : SkillBase
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeForce = 5f;
    [SerializeField] private float screenShakeIntensity = 0.3f;
    [SerializeField] private ParticleSystem shakeParticles;
    
    public override void Execute()
    {
        if (IsActive) return;
        
        StartCoroutine(ExecuteShake());
    }
    
    private IEnumerator ExecuteShake()
    {
        IsActive = true;
        PlaySound();
        
        // Apply forces to all fruits
        FruitController[] fruits = FindObjectsOfType<FruitController>();
        foreach (var fruit in fruits)
        {
            Rigidbody2D rb = fruit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomForce = new Vector2(
                    Random.Range(-shakeForce, shakeForce),
                    Random.Range(0, shakeForce * 0.5f)
                );
                rb.AddForce(randomForce, ForceMode2D.Impulse);
            }
        }
        
        // Screen shake effect
        if (EffectsManager.Instance != null)
        {
            EffectsManager.Instance.ShakeCamera(screenShakeIntensity, duration);
        }
        
        // Particle effect
        if (shakeParticles != null)
        {
            shakeParticles.Play();
        }
        
        yield return new WaitForSeconds(duration);
        IsActive = false;
    }
}

// WaterFillSkill.cs - Water fill skill implementation
public class WaterFillSkill : SkillBase
{
    [Header("Water Fill Settings")]
    [SerializeField] private float waterFillDuration = 10f;
    [SerializeField] private float buoyancyForce = 2f;
    [SerializeField] private float maxWaterLevel = 3f;
    [SerializeField] private SpriteRenderer waterSprite;
    [SerializeField] private ParticleSystem waterParticles;
    
    private float currentWaterLevel = 0f;
    
    public override void Execute()
    {
        if (IsActive) return;
        
        StartCoroutine(ExecuteWaterFill());
    }
    
    private IEnumerator ExecuteWaterFill()
    {
        IsActive = true;
        PlaySound();
        
        // Fill phase
        float fillStartTime = Time.time;
        while (Time.time < fillStartTime + waterFillDuration)
        {
            float progress = (Time.time - fillStartTime) / waterFillDuration;
            currentWaterLevel = Mathf.Lerp(0f, maxWaterLevel, progress);
            
            UpdateWaterVisuals();
            ApplyBuoyancy();
            
            yield return null;
        }
        
        // Drain phase
        float drainDuration = 2f;
        float drainStartTime = Time.time;
        while (Time.time < drainStartTime + drainDuration)
        {
            float progress = (Time.time - drainStartTime) / drainDuration;
            currentWaterLevel = Mathf.Lerp(maxWaterLevel, 0f, progress);
            
            UpdateWaterVisuals();
            ApplyBuoyancy();
            
            yield return null;
        }
        
        currentWaterLevel = 0f;
        UpdateWaterVisuals();
        RestoreNormalPhysics();
        IsActive = false;
    }
    
    private void ApplyBuoyancy()
    {
        FruitController[] fruits = FindObjectsOfType<FruitController>();
        
        foreach (var fruit in fruits)
        {
            if (fruit.transform.position.y < currentWaterLevel)
            {
                Rigidbody2D rb = fruit.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float submersionDepth = currentWaterLevel - fruit.transform.position.y;
                    float buoyancy = buoyancyForce * submersionDepth * Time.fixedDeltaTime;
                    rb.AddForce(Vector2.up * buoyancy, ForceMode2D.Force);
                    rb.gravityScale = 0.5f;
                }
            }
        }
    }
    
    private void RestoreNormalPhysics()
    {
        FruitController[] fruits = FindObjectsOfType<FruitController>();
        foreach (var fruit in fruits)
        {
            Rigidbody2D rb = fruit.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.gravityScale = 2f; // Restore normal gravity
        }
    }
    
    private void UpdateWaterVisuals()
    {
        if (waterSprite != null)
        {
            Vector3 pos = waterSprite.transform.position;
            pos.y = currentWaterLevel - (maxWaterLevel / 2f);
            waterSprite.transform.position = pos;
            
            Vector3 scale = waterSprite.transform.localScale;
            scale.y = currentWaterLevel / maxWaterLevel;
            waterSprite.transform.localScale = scale;
            
            // Fade water sprite based on level
            Color color = waterSprite.color;
            color.a = (currentWaterLevel / maxWaterLevel) * 0.6f;
            waterSprite.color = color;
        }
        
        if (waterParticles != null && currentWaterLevel > 0)
        {
            if (!waterParticles.isPlaying)
                waterParticles.Play();
        }
        else if (waterParticles != null && currentWaterLevel <= 0)
        {
            if (waterParticles.isPlaying)
                waterParticles.Stop();
        }
    }
}