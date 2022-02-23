using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CellType
{ 
    Water,
    Land,
    Sand
}

public class Node
{
    public Vector3 Position { get; set; }
    public CellType CellType { get; set; }
    public int Cost { get; set; }

    public Node(Vector3 position, CellType cellType, int cost)
    {
        Position = position;
        CellType = cellType;
        Cost = cost;
    }
}

public class GridGenerator : MonoBehaviour
{
    public int Width = 20;
    public int Depth = 20;
    public int NumberOfObstacles = 15;
    public GameObject ObstaclePrefab;
    public GameObject Player;
    [HideInInspector]
    public Node StartPosition;
    public GameObject Destination;
    [HideInInspector]
    public Node EndPosition;
    public Transform Ground;
    [Header("Visualize Path")]
    public Transform PathCells;
    public GameObject PathPrefab;

    public HashSet<Vector3> Obstacles;
    public HashSet<Node> WalkableCells;

    bool shouldPlayerMove = false;
    List<Node> playerPath;
    GameObject playerInstance;
    int pathIndex = 0;

    private void Start()
    {
        Obstacles = new HashSet<Vector3>();
        WalkableCells = new HashSet<Node>();

        GenerateGrid();
    }

    public void GenerateGrid()
    {
        ClearData();
        ClearPath();
        // Position Ground based on the size of the grid
        // Default Size of Plane mesh is 10, 1, 10 so that's why we are dividing by 10
        Ground.position = new Vector3(Width / 2f, 0, Depth / 2f);
        Ground.localScale = new Vector3(Width / 10f, 1, Depth / 10f);
        Camera.main.transform.position = new Vector3(Ground.position.x, 5f * (Width / 10f) + (Width / 10f), Ground.position.z - Depth / 2f - Depth / 4f - (Depth / 10f));

        PlaceObstacles();
        StartPosition = PlaceObject(Player);
        EndPosition = PlaceObject(Destination);

        LocateWalkableCells();
    }

    private void LocateWalkableCells()
    {
        for (int z = 0; z < Depth; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!IsCellOccupied(new Node(new Vector3(x, 0, z), CellType.Land, 1)))
                {
                    WalkableCells.Add(new Node(new Vector3(x, 0, z), CellType.Land, 1));
                }
            }
        }
    }

    public List<Node> GetNeighbours(Node currentCell)
    {
        var neighbours = new List<Node>()
        {
            new Node(new Vector3(currentCell.Position.x - 1, 0, currentCell.Position.z), CellType.Land, 1), // Up
            new Node(new Vector3(currentCell.Position.x + 1, 0, currentCell.Position.z), CellType.Land, 1), // Down
            new Node(new Vector3(currentCell.Position.x, 0, currentCell.Position.z - 1), CellType.Land, 1), // Left
            new Node(new Vector3(currentCell.Position.x, 0, currentCell.Position.z + 1), CellType.Land, 1) // Right
        };

        var walkableNeighbours = new List<Node>();
        foreach (var neighbour in neighbours)
        {
            if (!IsCellOccupied(neighbour) && IsInLevelBounds(neighbour))
                walkableNeighbours.Add(neighbour);
        }

        return walkableNeighbours;
    }

    private bool IsInLevelBounds(Node neighbour)
    {
        if (neighbour.Position.x > 0 && neighbour.Position.x <= Width - 1 && neighbour.Position.z > 0 && neighbour.Position.z <= Depth - 1)
            return true;

        return false;
    }

    public void ClearPath()
    {
        foreach (Transform pathCell in PathCells)
        {
            Destroy(pathCell.gameObject);
        }
    }

    private Node PlaceObject(GameObject gameObjectToPlace)
    {
        while (true)
        {
            var positionX = UnityEngine.Random.Range(1, Width);
            var positionZ = UnityEngine.Random.Range(1, Depth);

            // Y must be 0 otherwise even if x and z match we would still get multiple objects placed at the same location because they have different Y
            var cellPosition = new Node(new Vector3(positionX, 0, positionZ), CellType.Land, 1); 

            if (!IsCellOccupied(cellPosition))
            {
                Node objectPosition = cellPosition;
                objectPosition.Position = new Vector3(objectPosition.Position.x, gameObjectToPlace.transform.position.y, objectPosition.Position.z);

                if(gameObjectToPlace.name == "PlayerHolder")
                    playerInstance = Instantiate(gameObjectToPlace, objectPosition.Position, Quaternion.identity, transform);
                else
                    Instantiate(gameObjectToPlace, objectPosition.Position, Quaternion.identity, transform);

                return cellPosition;
            }
        }
    }

    private void ClearData()
    {
        DeleteChildren(transform);
        Obstacles.Clear();
        WalkableCells.Clear();
    }

    private void DeleteChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    private void PlaceObstacles()
    {
        var obstaclesToGenerate = NumberOfObstacles;

        while (obstaclesToGenerate > 0)
        {
            var positionX = UnityEngine.Random.Range(1, Width);
            var positionZ = UnityEngine.Random.Range(1, Depth);

            // Y must be 0 otherwise even if x and z match we would still get multiple objects placed at the same location because they have different Y
            var cellPosition = new Node(new Vector3(positionX, 0, positionZ), CellType.Water, 1);

            if (!IsCellOccupied(cellPosition))
            {
                Obstacles.Add(cellPosition.Position);

                var objectPosition = cellPosition;
                objectPosition.Position = new Vector3(objectPosition.Position.x, ObstaclePrefab.transform.position.y, objectPosition.Position.z);


                Instantiate(ObstaclePrefab, objectPosition.Position, Quaternion.identity, transform);
                obstaclesToGenerate--;
            }
        }
    }

    public void VisualizePath(Dictionary<Node, Node> cellParents)
    {
        var path = new List<Node>();
        var current = cellParents[EndPosition];
        
        path.Add(EndPosition);

        while(current.Position != StartPosition.Position)
        {
            path.Add(current);
            current = cellParents[current];
        }

        for (int i = 1; i < path.Count; i++)
        {
            var pathCellPosition = path[i];
            pathCellPosition.Position = new Vector3(pathCellPosition.Position.x, PathPrefab.transform.position.y, pathCellPosition.Position.z);

            Instantiate(PathPrefab, pathCellPosition.Position, Quaternion.identity, PathCells);
        }

        MovePlayer(path);
    }

    private void MovePlayer(List<Node> path)
    {
        Debug.Log("Move Player!");
        shouldPlayerMove = true;
        playerPath = path;
        pathIndex = playerPath.Count - 1;

        Debug.Log("Destination cell: " + StartPosition.Position);
    }

    private void Update()
    {
        if (shouldPlayerMove)
        {
            var nextCellToVisit = playerPath[pathIndex].Position;
            Debug.Log("Player move in Update: " + nextCellToVisit);


            playerInstance.transform.position = Vector3.MoveTowards(playerInstance.transform.position, nextCellToVisit, 2 * Time.deltaTime);
            playerInstance.transform.LookAt(nextCellToVisit);

            if (playerInstance.transform.position == nextCellToVisit)
                pathIndex--;

            if(pathIndex < 0)
            {
                shouldPlayerMove = false;
                playerPath.Clear();
            }
        }
    }

    private bool IsCellOccupied(Node node)
    {
        if (Obstacles.Contains(node.Position))
            return true;

        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(Width, 1, Depth));
        if(WalkableCells != null)
        {
            foreach(Node n in WalkableCells)
            {

                Gizmos.color = Color.green;
                Gizmos.DrawCube(n.Position, Vector3.one * ((0.5f * 2.0f) - .1f));
            }

/*            foreach(Node n in Obstacles)
            {
                if(n.CellType == CellType.Water)
                {
                    Gizmos.color = Color.blue;
                }
                Gizmos.DrawCube(n.Position, Vector3.one * ((0.5f * 2.0f) - .1f));
            }*/
        }
    }


}
