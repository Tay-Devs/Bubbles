using System.Collections.Generic;
using UnityEngine;

public class LevelPathRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    public LineRenderer lineRenderer;
    public int curveResolution = 10;
    public float lineWidth = 10f;
    
    [Header("Sorting")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;
    
    private List<Vector3> pathPoints = new List<Vector3>();
    private Camera renderCamera;
    
    void Start()
    {
        renderCamera = Camera.main;
        
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        SetupLineRenderer();
    }
    
    // Configures LineRenderer settings for consistent appearance.
    private void SetupLineRenderer()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingLayerName = sortingLayerName;
        lineRenderer.sortingOrder = sortingOrder;
        
        // Simple white material if none assigned
        if (lineRenderer.material == null)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
        }
    }
    
    // Updates the path to connect all visible level nodes.
    // Creates smooth bezier curves between consecutive nodes.
    public void UpdatePath(Dictionary<int, LevelNode> activeNodes, int lowestLevel, int highestLevel)
    {
        if (lineRenderer == null) return;
        
        pathPoints.Clear();
        
        List<Vector3> nodePositions = new List<Vector3>();
        
        for (int level = lowestLevel; level <= highestLevel; level++)
        {
            if (activeNodes.ContainsKey(level))
            {
                // Convert to world position for LineRenderer
                Vector3 worldPos = activeNodes[level].transform.position;
                nodePositions.Add(worldPos);
            }
        }
        
        if (nodePositions.Count < 2)
        {
            ClearPath();
            return;
        }
        
        for (int i = 0; i < nodePositions.Count - 1; i++)
        {
            Vector3 start = nodePositions[i];
            Vector3 end = nodePositions[i + 1];
            
            Vector3 control1 = start + Vector3.up * (end.y - start.y) * 0.5f;
            Vector3 control2 = end - Vector3.up * (end.y - start.y) * 0.5f;
            
            for (int j = 0; j <= curveResolution; j++)
            {
                float t = (float)j / curveResolution;
                Vector3 point = CalculateBezierPoint(t, start, control1, control2, end);
                pathPoints.Add(point);
            }
        }
        
        RenderPath();
    }
    
    // Calculates a point on a cubic bezier curve at parameter t (0-1).
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector3 point = uuu * p0;
        point += 3f * uu * t * p1;
        point += 3f * u * tt * p2;
        point += ttt * p3;
        
        return point;
    }
    
    // Applies path points to the LineRenderer.
    private void RenderPath()
    {
        lineRenderer.positionCount = pathPoints.Count;
        lineRenderer.SetPositions(pathPoints.ToArray());
    }
    
    // Clears the path.
    private void ClearPath()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }
    
    // Sets line color at runtime.
    public void SetColor(Color color)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
    
    // Sets line width at runtime.
    public void SetWidth(float width)
    {
        lineWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }
}