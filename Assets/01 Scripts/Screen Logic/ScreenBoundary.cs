using UnityEngine;

public class ScreenBoundary : MonoBehaviour
{
    [Header("Physics Material")]
    public PhysicsMaterial2D bouncyMaterial; // Assign in inspector or create at runtime
    
    [Header("Boundary Settings")]
    public float boundaryThickness = 0.5f;
    public bool createCeiling = true;
    
    void Start()
    {
        CreateBoundaries();
    }
    
    void CreateBoundaries()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // Get screen bounds in world space
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
        
        // Left wall
        CreateWall("LeftWall", 
            new Vector3(camPos.x - screenWidth/2 - boundaryThickness/2, camPos.y, 0),
            new Vector2(boundaryThickness, screenHeight));
        
        // Right wall
        CreateWall("RightWall", 
            new Vector3(camPos.x + screenWidth/2 + boundaryThickness/2, camPos.y, 0),
            new Vector2(boundaryThickness, screenHeight));
        
        // Top wall (ceiling) not sure if needed
        /*if (createCeiling)
        {
            CreateWall("Ceiling", 
                new Vector3(camPos.x, camPos.y + screenHeight/2 + boundaryThickness/2, 0),
                new Vector2(screenWidth + boundaryThickness * 2, boundaryThickness));
        }*/
    }
    
    void CreateWall(string name, Vector3 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = transform;
        wall.transform.position = position;
        wall.layer = LayerMask.NameToLayer("Default"); // Or create a "Boundary" layer
        
        // Add collider
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.sharedMaterial = bouncyMaterial;
        
        // Make it static for performance
        wall.isStatic = true;
    }
}