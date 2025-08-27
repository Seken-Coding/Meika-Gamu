// AudioManager.cs - Centralized audio management
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private bool loopMusic = true;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip fruitDropSound;
    [SerializeField] private AudioClip fruitMergeSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip skillActivationSound;
    [SerializeField] private AudioClip newHighScoreSound;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.8f;
    
    public static AudioManager Instance { get; private set; }
    
    // Properties
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;
    public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
    
    // Events
    public System.Action<float> OnMusicVolumeChanged;
    public System.Action<float> OnSFXVolumeChanged;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        LoadVolumeSettings();
        PlayBackgroundMusic();
    }
    
    void InitializeAudio()
    {
        // Create audio sources if not assigned
        if (musicSource == null)
        {
            GameObject musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(transform);
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFXSource");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }
    
    void LoadVolumeSettings()
    {
        if (SaveManager.Instance != null)
        {
            var gameData = SaveManager.Instance.GetGameData();
            if (gameData != null)
            {
                SetMusicVolume(gameData.musicVolume);
                SetSFXVolume(gameData.sfxVolume);
            }
        }
    }
    
    public void PlayBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = loopMusic;
            musicSource.Play();
        }
    }
    
    public void StopBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
    
    public void PauseBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }
    
    public void ResumeBackgroundMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }
    
    // Sound effect methods
    public void PlayFruitDrop()
    {
        PlaySFX(fruitDropSound);
    }
    
    public void PlayFruitMerge()
    {
        PlaySFX(fruitMergeSound);
    }
    
    public void PlayGameOver()
    {
        PlaySFX(gameOverSound);
    }
    
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }
    
    public void PlaySkillActivation()
    {
        PlaySFX(skillActivationSound);
    }
    
    public void PlayNewHighScore()
    {
        PlaySFX(newHighScoreSound);
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }
    
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
        }
    }
    
    // Volume control methods
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAudioSourceVolumes();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
        OnMusicVolumeChanged?.Invoke(musicVolume);
        
        // Save to persistent data
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateSettings(musicVolume, sfxVolume);
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        OnSFXVolumeChanged?.Invoke(sfxVolume);
        
        // Save to persistent data
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateSettings(musicVolume, sfxVolume);
        }
    }
    
    void UpdateAudioSourceVolumes()
    {
        UpdateMusicVolume();
        // SFX volume is handled per-clip in PlaySFX methods
    }
    
    void UpdateMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }
    
    // Fade methods for smooth transitions
    public void FadeInMusic(float duration = 1f)
    {
        if (musicSource != null)
        {
            StartCoroutine(FadeAudio(musicSource, 0f, musicVolume * masterVolume, duration));
        }
    }
    
    public void FadeOutMusic(float duration = 1f)
    {
        if (musicSource != null)
        {
            StartCoroutine(FadeAudio(musicSource, musicSource.volume, 0f, duration));
        }
    }
    
    IEnumerator FadeAudio(AudioSource source, float startVolume, float endVolume, float duration)
    {
        if (source == null) yield break;
        
        float elapsedTime = 0f;
        source.volume = startVolume;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / duration;
            source.volume = Mathf.Lerp(startVolume, endVolume, progress);
            yield return null;
        }
        
        source.volume = endVolume;
        
        // Stop the source if volume reached zero
        if (endVolume <= 0f)
        {
            source.Stop();
        }
    }
    
    // Audio settings validation
    void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        
        if (Application.isPlaying)
        {
            UpdateAudioSourceVolumes();
        }
    }
}

// EffectsManager.cs - Visual and screen effects
public class EffectsManager : MonoBehaviour
{
    [Header("Screen Shake")]
    [SerializeField] private float defaultShakeIntensity = 0.3f;
    [SerializeField] private float defaultShakeDuration = 0.5f;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem mergeParticlesPrefab;
    [SerializeField] private ParticleSystem explosionParticlesPrefab;
    
    // Private fields
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool isShaking = false;
    
    public static EffectsManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeEffects();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeEffects()
    {
        mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
        }
    }
    
    public void ShakeCamera(float intensity = -1f, float duration = -1f)
    {
        if (isShaking) return;
        
        float shakeIntensity = intensity >= 0 ? intensity : defaultShakeIntensity;
        float shakeDuration = duration >= 0 ? duration : defaultShakeDuration;
        
        StartCoroutine(CameraShakeCoroutine(shakeIntensity, shakeDuration));
    }
    
    IEnumerator CameraShakeCoroutine(float intensity, float duration)
    {
        if (mainCamera == null) yield break;
        
        isShaking = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity, intensity);
            
            mainCamera.transform.position = originalCameraPosition + new Vector3(x, y, 0);
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        mainCamera.transform.position = originalCameraPosition;
        isShaking = false;
    }
    
    public void PlayMergeEffect(Vector3 position, Color color)
    {
        if (mergeParticlesPrefab != null)
        {
            ParticleSystem particles = Instantiate(mergeParticlesPrefab, position, Quaternion.identity);
            
            // Customize particle color
            var main = particles.main;
            main.startColor = color;
            
            particles.Play();
            
            // Destroy after playing
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }
        else
        {
            // Fallback simple effect
            CreateSimpleMergeEffect(position, color);
        }
    }
    
    void CreateSimpleMergeEffect(Vector3 position, Color color)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "MergeEffect";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.2f;
        
        // Remove collider
        Destroy(effect.GetComponent<Collider>());
        
        // Set color
        Renderer renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        
        StartCoroutine(AnimateSimpleMergeEffect(effect));
    }
    
    IEnumerator AnimateSimpleMergeEffect(GameObject effect)
    {
        float duration = 0.8f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 endScale = startScale * 4f;
        Renderer renderer = effect.GetComponent<Renderer>();
        Color startColor = renderer.material.color;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (effect == null) break;
            
            float progress = t / duration;
            
            // Scale up
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            // Fade out
            Color color = startColor;
            color.a = 1f - progress;
            renderer.material.color = color;
            
            yield return null;
        }
        
        if (effect != null)
        {
            Destroy(effect);
        }
    }
    
    public void PlayExplosionEffect(Vector3 position)
    {
        if (explosionParticlesPrefab != null)
        {
            ParticleSystem explosion = Instantiate(explosionParticlesPrefab, position, Quaternion.identity);
            explosion.Play();
            
            Destroy(explosion.gameObject, explosion.main.duration + explosion.main.startLifetime.constantMax);
        }
    }
    
    public void FlashScreen(Color flashColor, float duration = 0.2f)
    {
        StartCoroutine(ScreenFlashCoroutine(flashColor, duration));
    }
    
    IEnumerator ScreenFlashCoroutine(Color flashColor, float duration)
    {
        // Create a full-screen flash overlay
        GameObject flashOverlay = new GameObject("FlashOverlay");
        Canvas canvas = flashOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        UnityEngine.UI.Image flashImage = flashOverlay.AddComponent<UnityEngine.UI.Image>();
        flashImage.color = flashColor;
        
        RectTransform rectTransform = flashImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Fade out the flash
        float elapsed = 0f;
        Color startColor = flashColor;
        Color endColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            flashImage.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }
        
        Destroy(flashOverlay);
    }
    
    void Update()
    {
        // Update camera reference if it changes
        if (mainCamera == null)
        {
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.position;
            }
        }
    }
}