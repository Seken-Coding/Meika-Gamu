// ImprovedFruitController.cs - Enhanced fruit behavior
using UnityEngine;

public class ImprovedFruitController : MonoBehaviour
{
    private int fruitLevel;
    private FruitProgressionSO progression;
    private bool canMerge = false;
    private bool hasMerged = false;
    
    private Rigidbody2D rb;
    private CircleCollider2D col;
    
    public int FruitLevel => fruitLevel;
    public bool CanMerge => canMerge && !hasMerged;
    
    public void Initialize(int level, FruitProgressionSO fruitProgression)
    {
        fruitLevel = level;
        progression = fruitProgression;
        
        SetupPhysics();
        
        // Enable merging after delay to prevent instant merging
        Invoke(nameof(EnableMerging), 0.5f);
    }
    
    void SetupPhysics()
    {
        // Setup Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        
        rb.gravityScale = 2f;
        rb.drag = 0.5f;
        rb.angularDrag = 5f;
        
        // Setup Collider
        col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        
        // Create physics material
        if (col.sharedMaterial == null)
        {
            PhysicsMaterial2D mat = new PhysicsMaterial2D("FruitPhysics");
            mat.friction = 0.4f;
            mat.bounciness = 0.3f;
            col.sharedMaterial = mat;
        }
    }
    
    void EnableMerging()
    {
        canMerge = true;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        ImprovedFruitController other = collision.gameObject.GetComponent<ImprovedFruitController>();
        
        if (other != null && CanMergeWith(other))
        {
            MergeWith(other);
        }
    }
    
    bool CanMergeWith(ImprovedFruitController other)
    {
        return CanMerge && 
               other.CanMerge && 
               fruitLevel == other.fruitLevel &&
               fruitLevel < progression.fruitLevels.Length - 1; // Not max level
    }
    
    void MergeWith(ImprovedFruitController other)
    {
        hasMerged = true;
        other.hasMerged = true;
        
        // Calculate merge position
        Vector3 mergePosition = (transform.position + other.transform.position) / 2f;
        
        // Create merge effect (simple for now)
        CreateMergeEffect(mergePosition);
        
        // Notify game manager
        if (ImprovedGameManager.Instance != null)
        {
            ImprovedGameManager.Instance.OnFruitMerged(fruitLevel, mergePosition);
        }
        
        Debug.Log($"Merged two level {fruitLevel} fruits into level {fruitLevel + 1}!");
        
        // Destroy both fruits
        Destroy(other.gameObject);
        Destroy(gameObject);
    }
    
    void CreateMergeEffect(Vector3 position)
    {
        // Simple particle effect - you can replace this with proper VFX later
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "MergeEffect";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.5f;
        
        // Remove collider
        Destroy(effect.GetComponent<Collider>());
        Destroy(effect.GetComponent<SphereCollider>());
        
        // Color based on fruit level
        Renderer renderer = effect.GetComponent<Renderer>();
        var fruitData = progression.GetFruitData(fruitLevel);
        renderer.material.color = fruitData.debugColor;
        
        // Animate and destroy
        StartCoroutine(AnimateMergeEffect(effect));
    }
    
    System.Collections.IEnumerator AnimateMergeEffect(GameObject effect)
    {
        float duration = 0.5f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 endScale = startScale * 2f;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (effect == null) break;
            
            float progress = t / duration;
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            // Fade out
            Renderer renderer = effect.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f - progress;
                renderer.material.color = color;
            }
            
            yield return null;
        }
        
        if (effect != null)
            Destroy(effect);
    }
    
    void OnDestroy()
    {
        // Notify game manager that this fruit was destroyed
        if (ImprovedGameManager.Instance != null)
        {
            ImprovedGameManager.Instance.OnFruitDestroyed();
        }
    }
}