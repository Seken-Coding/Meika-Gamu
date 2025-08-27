// FruitController.cs - Individual fruit behavior and physics
using UnityEngine;
using System.Collections;

public class FruitController : MonoBehaviour
{
    [Header("Merge Settings")]
    [SerializeField] private float mergeDelay = 0.5f;
    [SerializeField] private LayerMask fruitLayerMask = -1;
    
    // Private fields
    private int fruitLevel;
    private FruitProgressionSO progression;
    private bool canMerge = false;
    private bool hasMerged = false;
    private bool isInitialized = false;
    
    // Components
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private SpriteRenderer spriteRenderer;
    
    // Properties
    public int FruitLevel => fruitLevel;
    public bool CanMerge => canMerge && !hasMerged && isInitialized;
    public bool HasMerged => hasMerged;
    
    // Events
    public System.Action<FruitController> OnMerged;
    public System.Action<FruitController> OnDestroyed;
    
    void Awake()
    {
        SetupComponents();
    }
    
    void SetupComponents()
    {
        // Get or add Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure physics
        rb.gravityScale = 2f;
        rb.drag = 0.5f;
        rb.angularDrag = 5f;
        rb.freezeRotation = false;
        
        // Get or add CircleCollider2D
        col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }
        
        // Set up physics material
        SetupPhysicsMaterial();
        
        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    void SetupPhysicsMaterial()
    {
        if (col.sharedMaterial == null)
        {
            PhysicsMaterial2D physicsMat = new PhysicsMaterial2D("FruitPhysics")
            {
                friction = 0.4f,
                bounciness = 0.3f
            };
            col.sharedMaterial = physicsMat;
        }
    }
    
    public void Initialize(int level, FruitProgressionSO fruitProgression)
    {
        fruitLevel = level;
        progression = fruitProgression;
        isInitialized = true;
        
        // Set up visual appearance
        SetupVisuals();
        
        // Enable merging after delay to prevent immediate merging
        StartCoroutine(EnableMergingAfterDelay());
    }
    
    void SetupVisuals()
    {
        if (progression == null) return;
        
        var fruitData = progression.GetFruitData(fruitLevel);
        if (fruitData == null) return;
        
        // Set sprite if available
        if (spriteRenderer != null && fruitData.prefab != null)
        {
            SpriteRenderer prefabSprite = fruitData.prefab.GetComponent<SpriteRenderer>();
            if (prefabSprite != null && prefabSprite.sprite != null)
            {
                spriteRenderer.sprite = prefabSprite.sprite;
            }
        }
        
        // Set debug color if no sprite
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.color = fruitData.debugColor;
        }
        
        // Set name for debugging
        gameObject.name = $"Fruit_Level{fruitLevel}_{fruitData.name}";
    }
    
    IEnumerator EnableMergingAfterDelay()
    {
        yield return new WaitForSeconds(mergeDelay);
        canMerge = true;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!CanMerge) return;
        
        FruitController otherFruit = collision.gameObject.GetComponent<FruitController>();
        if (otherFruit != null && CanMergeWith(otherFruit))
        {
            InitiateMerge(otherFruit);
        }
    }
    
    bool CanMergeWith(FruitController other)
    {
        if (!other.CanMerge) return false;
        if (fruitLevel != other.fruitLevel) return false;
        if (fruitLevel >= progression.fruitLevels.Length - 1) return false; // Max level
        if (hasMerged || other.hasMerged) return false;
        
        return true;
    }
    
    void InitiateMerge(FruitController other)
    {
        // Prevent both fruits from merging with others
        hasMerged = true;
        other.hasMerged = true;
        
        // Calculate merge position (average of both positions)
        Vector3 mergePosition = (transform.position + other.transform.position) / 2f;
        
        // Create merge visual effect
        CreateMergeEffect(mergePosition);
        
        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFruitMerged(fruitLevel, mergePosition);
        }
        
        // Trigger merge events
        OnMerged?.Invoke(this);
        other.OnMerged?.Invoke(other);
        
        // Play merge sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFruitMerge();
        }
        
        Debug.Log($"Merged two level {fruitLevel} fruits into level {fruitLevel + 1}");
        
        // Destroy both fruits
        DestroyFruit();
        other.DestroyFruit();
    }
    
    void CreateMergeEffect(Vector3 position)
    {
        // Create simple merge effect
        if (EffectsManager.Instance != null)
        {
            var fruitData = progression.GetFruitData(fruitLevel);
            Color effectColor = fruitData?.debugColor ?? Color.white;
            EffectsManager.Instance.PlayMergeEffect(position, effectColor);
        }
        else
        {
            // Fallback simple effect
            CreateSimpleMergeEffect(position);
        }
    }
    
    void CreateSimpleMergeEffect(Vector3 position)
    {
        // Create a simple expanding circle effect
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "MergeEffect";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.3f;
        
        // Remove unnecessary components
        Destroy(effect.GetComponent<Collider>());
        Destroy(effect.GetComponent<SphereCollider>());
        
        // Set color
        Renderer renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            var fruitData = progression.GetFruitData(fruitLevel);
            renderer.material.color = fruitData?.debugColor ?? Color.white;
        }
        
        // Animate and destroy
        StartCoroutine(AnimateSimpleEffect(effect));
    }
    
    IEnumerator AnimateSimpleEffect(GameObject effect)
    {
        if (effect == null) yield break;
        
        float duration = 0.5f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 endScale = startScale * 3f;
        Renderer renderer = effect.GetComponent<Renderer>();
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (effect == null) break;
            
            float progress = t / duration;
            
            // Scale up
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            // Fade out
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f - progress;
                renderer.material.color = color;
            }
            
            yield return null;
        }
        
        if (effect != null)
        {
            Destroy(effect);
        }
    }
    
    public void ApplyForce(Vector2 force, ForceMode2D forceMode = ForceMode2D.Force)
    {
        if (rb != null)
        {
            rb.AddForce(force, forceMode);
        }
    }
    
    public void SetGravityScale(float scale)
    {
        if (rb != null)
        {
            rb.gravityScale = scale;
        }
    }
    
    public void SetPhysicsEnabled(bool enabled)
    {
        if (rb != null)
        {
            rb.simulated = enabled;
        }
    }
    
    public void DestroyFruit()
    {
        // Notify about destruction
        OnDestroyed?.Invoke(this);
        
        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFruitDestroyed();
        }
        
        // Destroy the game object
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        // Clean up any remaining effects or references
        StopAllCoroutines();
    }
    
    // Utility methods for debugging
    public Vector2 GetVelocity()
    {
        return rb?.velocity ?? Vector2.zero;
    }
    
    public float GetMass()
    {
        return rb?.mass ?? 1f;
    }
    
    public bool IsGrounded()
    {
        if (col == null) return false;
        
        // Simple ground check - cast a small ray downward
        float rayDistance = col.radius + 0.1f;
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, 
            col.radius * 0.8f, 
            Vector2.down, 
            rayDistance, 
            fruitLayerMask
        );
        
        return hit.collider != null;
    }
    
    void OnDrawGizmos()
    {
        // Draw merge detection radius in scene view
        if (col != null)
        {
            Gizmos.color = canMerge ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, col.radius);
            
            // Draw level indicator
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * col.radius, 
                                      $"Lv.{fruitLevel}");
            #endif
        }
    }
}