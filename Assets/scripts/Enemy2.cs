using UnityEngine;

public class Enemy2 : MonoBehaviour
{
    AStarPathfinding Pathfinder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Pathfinder = GetComponent<AStarPathfinding>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
