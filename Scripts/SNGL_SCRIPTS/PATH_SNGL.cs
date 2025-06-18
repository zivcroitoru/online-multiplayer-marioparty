using TMPro;
using UnityEngine;
using System.Collections;
using DG.Tweening;
public class PATH_SNGL : MonoBehaviour
{
    public Transform[] waypoints;
    public int waypointIndex = 0;
    public float moveSpeed = 2f;
    public int stepsRemaining;
    public TextMeshProUGUI stepsLeftText;
    public bool isMoving = false;
    public bool moveAllowed = false;
    private GAME_CNTRL_SNGL gameControl;

    void Start()
    {

        gameControl = GAME_CNTRL_SNGL.instance; // Use the singleton instance

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
            Debug.LogError("Waypoints array is empty!");
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
            Debug.LogError("Waypoints array is empty!");
        }
    }

    public void StartPlayerMovement(int steps)
    {
        isMoving = true;
        moveAllowed = true;
        stepsRemaining = steps;  // Set the steps remaining to the rolled number

        // Show the initial steps rolled
        stepsLeftText?.gameObject.SetActive(true);
        stepsLeftText.text = stepsRemaining.ToString();

        StartCoroutine(Move());
    }

    public IEnumerator Move()
    {
        for (; stepsRemaining > 0; stepsRemaining--)
        {
            // Update stepsLeftText with the remaining steps
            stepsLeftText.text = stepsRemaining.ToString();

            // Move to next waypoint
            yield return StartCoroutine(MoveToNextWaypoint());

            // Wait until star tile interaction is complete
            yield return StartCoroutine(CheckIfStarTile());
        }

        FinishMovement();
    }

    private IEnumerator CheckIfStarTile()
    {
        // Check if the player landed on a STAR tile
        GameObject currentTile = GetCurrentTile();
        if (currentTile != null && currentTile.name.StartsWith("STAR"))
        {
            // Notify GameControl about the STAR interaction
            gameControl.StartStarTileInteraction(gameObject);

            // Wait until the interaction is complete
            yield return new WaitUntil(() => gameControl.starInteractionComplete);

            // Reset the interaction completion flag
            gameControl.starInteractionComplete = false;
        }
    }

    public IEnumerator MoveToNextWaypoint()
    {
        if (waypoints.Length == 0) yield return null;
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

        waypointIndex = nextWaypointIndex;

        // Debug log for when the player reaches the final tile in the movement
        GameObject currentTile = GetCurrentTile();
        if (currentTile != null)
        {
            Debug.Log($"Player landed on tile: {currentTile.name}");
        }
        else
        {
            Debug.LogWarning("Player landed on a tile, but it's not recognized.");
        }
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
        stepsLeftText?.gameObject.SetActive(false); // Hide the steps left UI if present

        // Get the final tile where the player or CPU has landed
        GameObject finalTile = GetCurrentTile(); // Implement your own logic
        if (finalTile != null)
        {
            Debug.Log($"Movement finished. Player landed on final tile: {finalTile.name}");

            // Check if it's Player 1 or CPU, and adjust coins accordingly
            bool isPlayer = (gameObject == GAME_CNTRL_SNGL.player1);
            //  gameControl.CheckTileAndAdjustCoins(finalTile, isPlayer); // Adjust coins based on the tile
        }
        else
        {
            Debug.LogWarning("Final tile is null. No coin adjustment.");
        }

        // Signal movement completion to GameControl
        if (gameControl != null)
        {
            // Inform GameControl that this player's movement is complete
            gameControl.PlayerMovementComplete(gameObject);
            Debug.Log("Movement completion signaled to GameControl.");
        }
        else
        {
            Debug.LogError("GameControl is null. Cannot signal movement completion.");
        }
    }




}
