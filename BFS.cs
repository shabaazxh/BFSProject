using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BFS : MonoBehaviour
{
    public GridGenerator GridData;

    Queue<Node> queue;
    HashSet<Vector3> visited;

    // Keep track of visited notes + which nodes did we get from
    // Necessary later for building the path
    Dictionary<Node, Node> cellParents;

    private void Start()
    {
        queue = new Queue<Node>();
        visited = new HashSet<Vector3>();
        cellParents = new Dictionary<Node, Node>();
    }

    public void Search()
    {
        ClearData();

        queue.Enqueue(GridData.StartPosition);
        visited.Add(GridData.StartPosition.Position);

        while (queue.Count > 0)
        {
            var currentCell = queue.Dequeue();

            if(currentCell.Position == GridData.EndPosition.Position)
            {
                Debug.Log("Destination reached!");
                GridData.VisualizePath(cellParents);
                return;
            }

            var neighbours = GridData.GetNeighbours(currentCell);
            foreach (var neighbour in neighbours)
            {
                if(!visited.Contains(neighbour.Position))
                {
                    queue.Enqueue(neighbour);
                    visited.Add(neighbour.Position);
                    cellParents[neighbour] = currentCell;
                }
            }
        }

        Debug.Log("Could not reach the destination.");
    }

    private void ClearData()
    {
        queue.Clear();
        visited.Clear();
        cellParents.Clear();
    }
}
