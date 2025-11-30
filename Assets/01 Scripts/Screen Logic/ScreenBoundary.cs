using UnityEngine;

public class ScreenBoundary : MonoBehaviour
{
    [Header("Physics Material")]
    public PhysicsMaterial2D bouncyMaterial;
    
    [Header("Boundary Settings")]
    public float boundaryThickness = 0.5f;
    public bool createCeiling = true;
    
    [Header("Ceiling Position")]
    public float ceilingYOffset = 0f; // Adjust if grid doesn't start at screen top
    
    void Start()
    {
        CreateBoundaries();
    }
    
    void CreateBoundaries()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        float screenHeight = 2f * cam.orthographicSize;
        float screenWidth = screenHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;
        
        // Create bouncy material if not assigned
        if (bouncyMaterial == null)
        {
            bouncyMaterial = new PhysicsMaterial2D("BouncyWalls");
            bouncyMaterial.bounciness = 1f;
            bouncyMaterial.friction = 0f;
        }
        
        // Left wall - bouncy
        CreateWall("LeftWall", 
            new Vector3(camPos.x - screenWidth/2 - boundaryThickness/2, camPos.y, 0),
            new Vector2(boundaryThickness, screenHeight),
            bouncyMaterial,
            "Untagged");
        
        // Right wall - bouncy
        CreateWall("RightWall", 
            new Vector3(camPos.x + screenWidth/2 + boundaryThickness/2, camPos.y, 0),
            new Vector2(boundaryThickness, screenHeight),
            bouncyMaterial,
            "Untagged");
        
        // Ceiling - not bouncy, tagged for detection
        if (createCeiling)
        {
            CreateWall("Ceiling", 
                new Vector3(camPos.x, camPos.y + screenHeight/2 + boundaryThickness/2 + ceilingYOffset, 0),
                new Vector2(screenWidth + boundaryThickness * 2, boundaryThickness),
                null, // No bounce material
                "Ceiling");
        }
    }
    
    void CreateWall(string name, Vector3 position, Vector2 size, PhysicsMaterial2D material, string tag)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = transform;
        wall.transform.position = position;
        wall.tag = tag;
        
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
        if (material != null)
            collider.sharedMaterial = material;
        
        wall.isStatic = true;
    }
}