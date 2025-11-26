using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class HexGridOld : MonoBehaviour
{
    public int width = 4;
    public int height = 6;
    private List<List<Bubble>> gridData;
    //private List<Bubble>gridData;

    public GameObject bubblePrefab;

    private List<Vector2Int> adjectionPointsShort = new List<Vector2Int>()
    {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.up,
        new Vector2Int(1,-1), //bottom right
        new Vector2Int(1,1), //bottom right
    };
    private List<Vector2Int> adjectionPointsLong = new List<Vector2Int>()
    {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.up,
        new Vector2Int(-1,-1), //bottom right
        new Vector2Int(-1,1), //bottom right
    };
    
    void Start()
    {
        Populate();
    }

    private void Populate()
    {
        gridData = new List<List<Bubble>>();
        for (int i = 0; i < height; i++)
        {
            List<Bubble> row = CreateRow(i); // count width 
            gridData.Add(row);
        }

        PositionBubbles();
    }

    private void PositionBubbles()
    {
        for (int y = 0; y < height; y++)
        {
            bool isShortRow = y % 2 != 0;
            int numbeOfBubbles = isShortRow ? width - 1: width ;
            for (int x = 0; x < numbeOfBubbles; x++)
            {
                Bubble bubble = gridData[y][x];
                float offset = isShortRow? 0.5f : 0; 
                bubble.transform.localPosition = new Vector3(x + offset, -y * 0.9f) ;
            }    
        }
    }

    private List<Bubble> CreateRow(int y)
    {
        List<Bubble> newRow = new List<Bubble>();
        int numbeOfBubbles = y%2 == 0 ? width : width - 1;
        //Debug.Log(numbeOfBubbles);
        for (int x = 0; x < numbeOfBubbles; x++)
        {
            GameObject go = Instantiate(bubblePrefab,transform);
            Bubble newBubble = go.GetComponent<Bubble>();
            int randomIndex = Random.Range(0, Enum.GetValues(typeof(BubbleType)).Length);
            int tempX = x;
            BubbleType randomType = (BubbleType)randomIndex;
            newBubble.SetType(randomType);
            
            newBubble.onTouch += () =>
            {
                //Debug.Log("Touched" + tempX + " " + y);
                ExploadAllBubblesAt(tempX, y);
            };
            newRow.Add(newBubble);
        }
        return newRow;
    }

    private void ExploadAllBubblesAt(int x, int y)
    {
        Bubble startBubble = gridData[y][x];
        Debug.Log(startBubble.type);
        Vector2Int startPosition = new Vector2Int(x, y);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> result = GetRelevantBubbles(startBubble.type,startPosition,visited);
        DestroyConnectdBubbles(result);
        
    }

    private void DestroyConnectdBubbles(List<Vector2Int> result)
    {
        result.ForEach(positionToDestroy =>
        {
            Bubble bubble = GetBubbleAt(positionToDestroy);
            bubble.Explode();
            gridData[positionToDestroy.y][positionToDestroy.x] = null;
        });
    }

    private List<Vector2Int> GetRelevantBubbles(BubbleType type, Vector2Int position,HashSet<Vector2Int> visited)
    {
         List<Vector2Int> relevantPositions= new List<Vector2Int>();
         Bubble currentBubble = GetBubbleAt(position);
         
         // Check if current position is relevant
         // if not , return empty list
         if (currentBubble == null || currentBubble.type != type || visited.Contains(position))
         {
             return relevantPositions;
         }
         
         visited.Add(position);
         relevantPositions.Add(position);
         // check neighbors for additional bubbles
         List<Vector2Int> neighbors = position.y %2==0 ?adjectionPointsLong:adjectionPointsShort;
         neighbors.ForEach(addition =>
         {
             List<Vector2Int> leftRelevantPositions = GetRelevantBubbles(type, position + addition,visited);
             relevantPositions.AddRange(leftRelevantPositions);
         });
         
         return relevantPositions;
    }

    private Bubble GetBubbleAt(Vector2Int position)
    {
        if (position.y >= height ||  position.y<0)
        {
            return null;
        }
        
        int numbeOfBubbles = position.y%2 == 0 ? width : width - 1;
        if (position.x >= numbeOfBubbles ||  position.x<0)
        {
            return null;
        }
        return gridData[position.y][position.x];
    }
}