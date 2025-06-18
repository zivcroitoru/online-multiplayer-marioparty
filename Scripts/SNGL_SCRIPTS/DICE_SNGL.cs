using UnityEngine;
using System.Collections;

public class DICE_SNGL : MonoBehaviour
{
    #region Variables
    public Sprite[] diceSides;
    private SpriteRenderer rend;
    public int whosTurn = 1; // 1 for Player, -1 for CPU
    public bool canRollDice = true;
    public bool isRolling = false;
    private AudioSource audioSource;
    public AudioClip diceJumpSFX;

    // Flags to prevent multiple coroutine invocations
    public bool isPlayerRolling = false;
    public bool isCPURolling = false;

    // Base position for absolute jumping
    private Vector3 basePosition;
    #endregion

    #region Initialization
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        basePosition = transform.position;  // Initialize base position
        ResetGameVariables();
        rend = GetComponent<SpriteRenderer>();
        canRollDice = true;
    }

    public void ResetGameVariables()
    {
        canRollDice = true;
        isRolling = false;
        whosTurn = 1;

        // Disable player movements at the start
        if (GAME_CNTRL_SNGL.player1 != null)
            GAME_CNTRL_SNGL.player1.GetComponent<PATH_SNGL>().moveAllowed = false;

        if (GAME_CNTRL_SNGL.player2 != null)
            GAME_CNTRL_SNGL.player2.GetComponent<PATH_SNGL>().moveAllowed = false;
    }

    private void OnEnable()
    {
        Debug.Log("Dice enabled.");
        rend = GetComponent<SpriteRenderer>();
        basePosition = transform.position;  // Update base position when enabled
        StartRolling();
    }

    private void OnDisable()
    {
        Debug.Log("Dice disabled.");
    }
    #endregion

    #region Player Interaction
    private void OnMouseDown()
    {
        // Disable the dice if the game is over
        if (GAME_CNTRL_SNGL.gameOver)
        {
            Debug.Log("Game is over. Disabling the dice.");
            gameObject.SetActive(false); // Disable the dice object
            return; // Exit the method
        }

        // Check conditions before allowing dice roll for player
        if (!GAME_CNTRL_SNGL.gameOver && canRollDice && whosTurn == 1 && !isPlayerRolling)
        {
            Debug.Log("Player clicked the dice. Stopping dice roll.");
            StopRolling();
            StartCoroutine(RollTheDice());
        }
    }
    #endregion

    #region Dice Rolling Logic
    public void StartRolling()
    {
        // Disable the dice if the game is over
        if (GAME_CNTRL_SNGL.gameOver)
        {
            Debug.Log("Game is over. Disabling the dice.");
            gameObject.SetActive(false); // Disable the dice object
            return; // Exit the method
        }

        // Start the rolling animation only if not already rolling
        if (!isRolling)
        {
            Debug.Log("Starting rolling animation.");
            isRolling = true;
            StartCoroutine(RollingAnimation());
        }
        else
        {
            Debug.Log("Rolling not started. Either already rolling or coroutine not allowed.");
        }
    }

    public void StopRolling()
    {
        if (isRolling)
        {
            Debug.Log("Stopping rolling animation.");
            isRolling = false;  // Stop the rolling animation
        }
    }

    public IEnumerator RollingAnimation()
    {
        Debug.Log("Rolling animation started.");
        int randomDiceSide = 0;

        // Keep updating dice sprite until stopped
        while (isRolling)
        {
            randomDiceSide = Random.Range(0, diceSides.Length);
            rend.sprite = diceSides[randomDiceSide];
            yield return new WaitForSeconds(0.05f);  // Speed of the dice roll animation
        }

        Debug.Log("Rolling animation stopped.");
    }
    #endregion

    #region Player Dice Roll Coroutine
    public IEnumerator RollTheDice()
    {
        // Disable the dice if the game is over
        if (GAME_CNTRL_SNGL.gameOver)
        {
            Debug.Log("Game is over. Disabling the dice.");
            gameObject.SetActive(false); // Disable the dice object
            yield break; // Exit the coroutine
        }

        Debug.Log("RollTheDice started");

        canRollDice = false;  // Disable further interaction while rolling
        isPlayerRolling = true;    // Indicate player is rolling

        StopRolling(); // Stop the rolling animation to get final result

        // Select final dice result
        int randomDiceSide = Random.Range(0, diceSides.Length);
        rend.sprite = diceSides[randomDiceSide];
        int diceResult = randomDiceSide + 1;

        Debug.Log($"Player dice result: {diceResult}");

        // Validate the result
        if (diceResult < 1 || diceResult > 6)
        {
            Debug.LogError($"Invalid dice result: {diceResult}");
            canRollDice = true;
            isPlayerRolling = false;
            yield break;
        }

        yield return StartCoroutine(FlashAndJump());  // Flash and jump animation after rolling
        yield return new WaitForSeconds(1f);          // Wait a moment after rolling

        // Move player if it's player's turn
        if (whosTurn == 1)
        {
            GAME_CNTRL_SNGL.player1DiceSideThrown = diceResult;
            rend.enabled = false;
            GetComponent<Collider2D>().enabled = false;

            GAME_CNTRL_SNGL.MovePlayer(1);
            yield return new WaitUntil(() => !GAME_CNTRL_SNGL.player1.GetComponent<PATH_SNGL>().moveAllowed);

            whosTurn = -1;  // Switch to CPU's turn
            Debug.Log("Player's turn ended. Switching to CPU's turn.");

            rend.enabled = true;
            GetComponent<Collider2D>().enabled = true;

            StartCoroutine(RollTheDiceForCPU());  // Start CPU's turn
        }

        isPlayerRolling = false; // Reset flag
        canRollDice = true; // Allow interaction again
    }
    #endregion

    #region CPU Dice Roll Coroutine
    public IEnumerator RollTheDiceForCPU()
    {
        // Disable the dice if the game is over
        if (GAME_CNTRL_SNGL.gameOver)
        {
            Debug.Log("Game is over. Disabling the dice.");
            gameObject.SetActive(false); // Disable the dice object
            yield break; // Exit the coroutine
        }

        // Ensure Player 1 has finished their turn before CPU can start rolling
        if (!GAME_CNTRL_SNGL.instance.player1TurnFinished)
        {
            Debug.LogWarning("Player 1 has not finished their turn yet. CPU cannot roll.");
            yield break;  // Exit the coroutine early if Player 1's turn is not finished
        }

        // Prevent multiple roll requests
        if (isCPURolling)
        {
            Debug.LogWarning("CPU is already rolling. Ignoring additional roll request.");
            yield break;
        }

        Debug.Log("RollTheDiceForCPU started");
        canRollDice = false;  // Disable further interaction during CPU's turn
        isCPURolling = true;  // Indicate CPU is rolling

        rend.color = Color.gray;
        StartRolling();  // Start dice roll animation
        yield return new WaitForSeconds(2f);  // Let the dice roll for 2 seconds

        StopRolling();
        Debug.Log("CPU has stopped the dice.");

        // Select final dice result
        int randomDiceSide = Random.Range(0, diceSides.Length);
        rend.sprite = diceSides[randomDiceSide];
        int diceResult = randomDiceSide + 1;

        Debug.Log($"Dice result for CPU movement: {diceResult}");
        yield return StartCoroutine(FlashAndJump());  // Flash and jump after rolling
        yield return new WaitForSeconds(1f);          // Wait after showing the result

        rend.color = Color.white;
        rend.enabled = false;
        GetComponent<Collider2D>().enabled = false;

        // Move Player 2 (CPU) based on dice result
        GAME_CNTRL_SNGL.player2DiceSideThrown = diceResult;
        GAME_CNTRL_SNGL.MovePlayer(2);

        // Wait for Player 2 (CPU) to finish moving
        yield return new WaitUntil(() => !GAME_CNTRL_SNGL.player2.GetComponent<PATH_SNGL>().moveAllowed);

        // Notify GameControl that CPU movement is complete
        GAME_CNTRL_SNGL.instance.PlayerMovementComplete(GAME_CNTRL_SNGL.player2);
        isRolling = false;
        isCPURolling = false;
        // End CPU's turn
        whosTurn = 1;  // Set back to player's turn
        isCPURolling = false;  // Reset flag

        // Do not start rolling again here; GameControl will handle starting the player's turn
        Debug.Log("CPU's turn ended.");
    }
    #endregion

    #region Utility: Flash and Jump Animation
    private IEnumerator FlashAndJump()
    {

        audioSource.PlayOneShot(diceJumpSFX, 0.7f);
        // Store the original color
        Color originalColor = rend.color;

        // Jump effect: Move the dice up to basePosition + (0, 0.5, 0)
        transform.position = basePosition + new Vector3(0, 0.5f, 0);  // Jump up
        yield return new WaitForSeconds(0.1f);                       // Hold in the air for a moment

        // Return to the exact original position
        transform.position = basePosition;
        rend.color = originalColor;

        yield return new WaitForSeconds(0.4f);  // Slight delay after the effect
    }

    #endregion
}
