using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BubblesGrid : MonoBehaviour
{
    public int width = 4;
    public int height = 6;
    private List<List<Bubble>> gridData;
    //private List<Bubble> gridData;
    [SerializeField] private GameObject bubblePrefab;

    private List<Vector2Int> adjacentPoints = new List<Vector2Int>()
    {
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down
    };
    void Start()
    {
        Populate();
    }

    private void Populate()
    {
        gridData = new List<List<Bubble>>();
        for (int y = 0; y < height; y++)
        {
            List<Bubble> row = CreateRow(y);
            gridData.Add(row);
       
        }
        PositionBubbles();
    }

    private void PositionBubbles()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
              Bubble bubble = gridData[y][x];
              bubble.transform.localPosition = new Vector3(x, -y);
            }
        }   
    }
    private List<Bubble> CreateRow(int y)
    {
        List<Bubble> newRow = new List<Bubble>();
        for (int x = 0; x < width; x++)
        {
            GameObject go = Instantiate(bubblePrefab, transform);
            Bubble newBubble = go.GetComponent<Bubble>();
            int randomIndex = Random.Range(0, Enum.GetValues(typeof(BubbleType)).Length);
            BubbleType randomType = (BubbleType)randomIndex;
            newBubble.SetType(randomType);
            int tempX = x;
            newBubble.onTouch += () =>
            {
                Debug.Log("Touched" + tempX + y);
                ExplodeAllBubblesAt(tempX, y);
            };
            newRow.Add(newBubble);
        }
        return newRow;
    }

    private void ExplodeAllBubblesAt(int x, int y)
    {
        Bubble startBubble = gridData[y][x];
        Debug.Log(startBubble.type);
        Vector2Int startingPosition = new Vector2Int(x, y);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> result = GetRelevantBubbles(startBubble.type, startingPosition, visited);
        result.ForEach(pos =>
        {
            Debug.Log(pos);
        });
        GetRelevantBubbles(startBubble.type ,startingPosition, visited);
    }

    private void DestroyConnectedBubbles(List<Vector2Int> result)
    {
        result.ForEach(positionToDestroy =>
        {
            Bubble bubble = GetBubbleAt(positionToDestroy);
            bubble.Explode();
            gridData[positionToDestroy.y][positionToDestroy.x] = null;
        });
       
        
    }
    private List<Vector2Int> GetRelevantBubbles(BubbleType type, Vector2Int position, HashSet<Vector2Int> visited)
    { 
        List<Vector2Int> relevantPostions = new List<Vector2Int>();
        Bubble currentBubble = GetBubbleAt(position);
        if (currentBubble == null || currentBubble.type != type || visited.Contains(position)) 
        {
            return relevantPostions;
        }
       
        visited.Add(position);
        relevantPostions.Add(position);
        adjacentPoints.ForEach(addition =>
        {
            List<Vector2Int> relevantPosition = GetRelevantBubbles(type, position, visited);
            relevantPostions.AddRange(relevantPosition);
        });
        return relevantPostions;
    }

    private Bubble GetBubbleAt(Vector2Int position)
    {
        if (position.y >= height || position.y < 0)
        {
            return null;
        }
        if (position.x >= width || position.x < 0)
        {
            return null;
        }
        return gridData[position.y][position.x];
    }
}
