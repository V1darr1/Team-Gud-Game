using UnityEngine;

public class MovingPath : MonoBehaviour
{
  //Go from A to B
  //Based off number of children in PathPoint1

    public Transform GetPath(int pathIndex)
    {
        return transform.GetChild(pathIndex);
    }
    public int GetNextPathIndex(int currIndex)
    {
        int nextPathIndex = currIndex + 1;
        
        if(nextPathIndex == transform.childCount)
        {
            nextPathIndex = 0;
        } 
        return nextPathIndex;
    }
}
