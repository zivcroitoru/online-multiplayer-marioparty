using TMPro;
using UnityEngine;
using System.Collections;
using DG.Tweening;

public class FollowThePath : MonoBehaviour
{
    public Transform[] waypoints;
    public int waypointIndex = 0;
    public float moveSpeed = 2f;
    public int stepsRemaining;
    public TextMeshProUGUI stepsLeftText;
    public bool isMoving = false;
    public bool moveAllowed = false;
    private GameControl gameControl;

    // Reference to the dice game object
    public GameObject dice;

    void Start()
    {
        gameControl = GameControl.instance; // Use the singleton instance

        InitializePosition();

        // Disable the steps left text at the start
        if (stepsLeftText != null)
        {
            stepsLeftText.gameObject.SetActive(false);
        }
    }

    private void InitializePosition()
    {
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[waypointIndex].position;
        }
        else
        {
          //  Debug.LogError("Waypoints array is empty!");
        }
    }

    public void ResetPlayer()
    {
        waypointIndex = 0;
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[waypointIndex].position;
        }
        else
        {
          //  Debug.LogError("Waypoints array is empty!");
        }
    }

    public void StartPlayerMovement(int steps)
    {
      //  Debug.Log("START PLAYER MOVEMENT STARTED");

        isMoving = true;
        moveAllowed = true;
        stepsRemaining = steps;  // Set the steps remaining to the rolled number

        // Show the initial steps rolled
        if (stepsLeftText != null)
        {
            stepsLeftText.gameObject.SetActive(true);
            stepsLeftText.text = stepsRemaining.ToString();
        }

        // Disable the dice when movement starts
        if (dice != null)
        {
            dice.SetActive(false);
        }

        StartCoroutine(Move());
    }

    public IEnumerator Move()
    {
        Debug.Log("MOVE STARTED");
        for (; stepsRemaining > 0; stepsRemaining--)
        {
            // Update stepsLeftText with the remaining steps
            if (stepsLeftText != null)
            {
                stepsLeftText.text = stepsRemaining.ToString();
            }

            // Move to the next waypoint
            yield return StartCoroutine(MoveToNextWaypoint());
        }

        FinishMovement();
    }



    public IEnumerator MoveToNextWaypoint()
    {
        if (waypoints.Length == 0)
            yield break;

        bool doneMove = false;

        int nextWaypointIndex = (waypointIndex + 1) % waypoints.Length;
        Vector3 nextPos = waypoints[nextWaypointIndex].position;

        // Calculate the distance between the current position and the next waypoint
        float distance = Vector3.Distance(transform.position, nextPos);

        // Calculate the duration based on the fixed moveSpeed
        float duration = distance / moveSpeed;

        // Move the player to the next waypoint over a constant time duration
        transform.DOMove(nextPos, duration).OnComplete(() => doneMove = true);

        yield return new WaitUntil(() => doneMove);

        // After reaching the waypoint, check if it's a STAR tile (passed by)
        GameObject currentTile = GetCurrentTile();
        if (currentTile != null)
        {
            Debug.Log($"{gameObject.name} passed by or landed on tile: {currentTile.name}");

            // Check if this is a STAR tile
            if (currentTile.name.StartsWith("STAR"))
            {
                Debug.Log($"{gameObject.name} passed by or landed on a STAR tile: {currentTile.name}");

                // Call GameControl to adjust coins and handle STAR tile interaction
                gameControl.CheckTileAndAdjustCoins(currentTile, gameObject.name == "Player1");
            }
        }
        else
        {
            Debug.LogWarning("Player passed by a tile, but it's not recognized.");
        }

        waypointIndex = nextWaypointIndex; // Update the waypoint index
    }


    public GameObject GetCurrentTile()
    {
        if (waypoints.Length > 0 && waypointIndex < waypoints.Length)
        {
            return waypoints[waypointIndex].gameObject;
        }
        return null;
    }

    private void FinishMovement()
    {
        isMoving = false;
        moveAllowed = false;
        if (stepsLeftText != null)
        {
            stepsLeftText.gameObject.SetActive(false); // Hide the steps left UI if present
        }

        // Enable the dice when movement ends
        if (gameControl.MiniGameActive == false)
        {
            dice.SetActive(true);
        }

        // Get the final tile where the player has landed
        GameObject finalTile = GetCurrentTile();
        if (finalTile != null)
        {
           // Debug.Log($"Movement finished. {gameObject.name} landed on final tile: {finalTile.name}");

            // Adjust coins or perform actions based on the tile (if needed)
            // gameControl.CheckTileAndAdjustCoins(finalTile, gameObject);
        }
        else
        {
          //  Debug.LogWarning("Final tile is null. No action taken.");
        }

        // Signal movement completion to GameControl
        if (gameControl != null)
        {
            // Inform GameControl that this player's movement is complete
            gameControl.PlayerMovementComplete(gameObject);
          //  Debug.Log("Movement completion signaled to GameControl.");
        }
        else
        {
            Debug.LogError("GameControl is null. Cannot signal movement completion.");
        }
    }
}
