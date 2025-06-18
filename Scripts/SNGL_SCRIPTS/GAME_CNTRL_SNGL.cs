
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class GAME_CNTRL_SNGL : MonoBehaviour
{
    #region Singleton Instance
    public static GAME_CNTRL_SNGL instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        // Reset static variables
        whoWinsText = null;
        player1MoveText = null;
        player2MoveText = null;
        player1 = null;
        player2 = null;
        player1DiceSideThrown = 0;
        player2DiceSideThrown = 0;
        player1StartWaypoint = 0;
        player2StartWaypoint = 0;
        gameOver = false;
    }

    #endregion

    #region Public Variables
    public bool starInteractionComplete = false;
    public GameObject starPromptUI;
    public TextMeshProUGUI playerStarsText;
    public TextMeshProUGUI cpuStarsText;
    public DICE_SNGL diceScript;
    public TextMeshProUGUI playerCoinsText;
    public TextMeshProUGUI cpuCoinsText;
    public GameObject dice;
    public GameObject restartButton;
    public GameObject menuButton;
    public TextMeshProUGUI stepsLeftText;
    public TextMeshProUGUI gameStartText;
    public int maxRounds = 10;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI starResultText;
    public GameObject WinScreen;
    // Minigame UI
    public GameObject minigameUI; // Assign this in the Inspector

    private AudioSource audioSource;
    public AudioClip winSFX;
    public AudioClip loseSFX;
    public AudioClip starSFX;
    #endregion

    #region Private Variables
    [Header("Coin Change Texts")]
    public TextMeshProUGUI playerCoinChangeText;
    public TextMeshProUGUI cpuCoinChangeText;

    [Header("Star Change Texts")]
    public TextMeshProUGUI playerStarChangeText;  // New for player star changes
    public TextMeshProUGUI cpuStarChangeText;     // New for CPU star changes

    private static GameObject whoWinsText, player1MoveText, player2MoveText;
    public static GameObject player1, player2;
    public static int player1DiceSideThrown = 0;
    public static int player2DiceSideThrown = 0;
    public static int player1StartWaypoint = 0;
    public static int player2StartWaypoint = 0;
    public static bool gameOver = false;
    public int playerCoins = 5;
    public int cpuCoins = 5;
    private int playerStars = 0;
    private int cpuStars = 0;
    [SerializeField] public bool player1TurnFinished = false;
    [SerializeField] public bool player2TurnFinished = false;
    private int currentRound = 1;
    private GameObject currentPlayer;
    public bool player1InInteraction = false;
    public bool player2InInteraction = false;
    private bool isTurnLocked = false;  // Locks the turn during interactions or player movements
    #endregion

    #region Initialization
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        dice.SetActive(true);
        player1InInteraction = false;
        player2InInteraction = false;
        // Subscribe to the Minigame Completed event
        if (MINIGAME_SNGL.instance != null)
        {
            MINIGAME_SNGL.OnMinigameCompleted += OnMinigameFinished;
        }
        else
        {
            Debug.LogError("MinigameController instance not found in the scene!");
        }

        // Initialize UI elements
        if (playerCoinChangeText != null)
            playerCoinChangeText.gameObject.SetActive(false);
        if (cpuCoinChangeText != null)
            cpuCoinChangeText.gameObject.SetActive(false);
        if (playerStarChangeText != null)
            playerStarChangeText.gameObject.SetActive(false);  // Disable Player 1 star change text
        if (cpuStarChangeText != null)
            cpuStarChangeText.gameObject.SetActive(false);     // Disable CPU star change text
        if (minigameUI != null)
            minigameUI.SetActive(false); // Ensure minigame UI is hidden at start

        // Initialize other game settings...
        UpdateCoinUI();
        UpdateStarUI();
        UpdateRoundUI();
        StartCoroutine(InitializePlayers());
        StartCoroutine(CountdownCoroutine());

        if (gameStartText != null) StartCoroutine(CountdownCoroutine());
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (MINIGAME_SNGL.instance != null)
        {
            MINIGAME_SNGL.OnMinigameCompleted -= OnMinigameFinished;
        }
    }

    private IEnumerator InitializePlayers()
    {
        yield return new WaitForEndOfFrame();

        player1 = GameObject.Find("Player1");
        player2 = GameObject.Find("Player2");

        if (player1 == null || player2 == null)
        {
            Debug.LogError("Player1 or Player2 GameObject not found in the scene.");
            yield break;
        }
        if (gameOver == true)

            dice.SetActive(false);
        gameOver = false;

        player1.GetComponent<PATH_SNGL>().ResetPlayer();
        player2.GetComponent<PATH_SNGL>().ResetPlayer();
        player1StartWaypoint = 1;
        player2StartWaypoint = 1;
        player1DiceSideThrown = 0;
        player2DiceSideThrown = 0;

        InitializeReferences();
        HideUIElements();
        HandlePlayerMovement();
    }

    private void InitializeReferences()
    {
        whoWinsText = GameObject.Find("WhoWinsText");
        player1MoveText = GameObject.Find("Player1MoveText");
        player2MoveText = GameObject.Find("Player2MoveText");

        GameObject diceObject = GameObject.Find("Dice");
        if (diceObject != null) diceScript = diceObject.GetComponent<DICE_SNGL>();
    }

    private void HideUIElements()
    {
        whoWinsText?.SetActive(false);
        player1MoveText?.SetActive(false);
        player2MoveText?.SetActive(false);
        restartButton?.SetActive(false);
        menuButton?.SetActive(false);
        WinScreen.SetActive(false);
    }
    #endregion

    #region Update Methods
    private void Update()
    {

        HandlePlayerMovement();
        CheckWinConditions();

        if (Input.GetKeyDown(KeyCode.S)) // Press "S" to toggle slow motion
        {
            if (Time.timeScale == 1f)
            {
                Time.timeScale = 0.1f; // Slow down the game even more
                Debug.Log("Time scale set to 0.1 (Slow Motion)");
            }
            else
            {
                Time.timeScale = 1f; // Return to normal speed
                Debug.Log("Time scale set to 1 (Normal Speed)");
            }
        }

        if (Input.GetKeyDown(KeyCode.F)) // Press "F" to toggle fast motion
        {
            if (Time.timeScale == 1f)
            {
                Time.timeScale = 20f; // Speed up the game
                Debug.Log("Time scale set to 20 (Fast Motion)");
            }
            else
            {
                Time.timeScale = 1f; // Return to normal speed
                Debug.Log("Time scale set to 1 (Normal Speed)");
            }
        }
    }

    private void HandlePlayerMovement()
    {
        if (player1 == null || player2 == null) return;

        // Attempt to unlock the turn
        UnlockTurn();

        // Respect the turn lock
        if (isTurnLocked) return;

        var player1Path = player1.GetComponent<PATH_SNGL>();
        var player2Path = player2.GetComponent<PATH_SNGL>();

        // Ensure we have a SpriteRenderer component on the dice
        SpriteRenderer diceSpriteRenderer = dice.GetComponent<SpriteRenderer>();
        if (diceSpriteRenderer == null)
        {
            Debug.LogError("Dice GameObject does not have a SpriteRenderer component!");
            return;
        }

        // Handle Player 1 Movement
        if (!player1TurnFinished)
        {
            if (player1DiceSideThrown == 0 && diceScript.canRollDice)
            {
                // Wait for player to roll the dice
                return;
            }

            if (!player1Path.isMoving && player1DiceSideThrown > 0 && !player1InInteraction)
            {
                // Start Player 1's movement
                isTurnLocked = true;    // Lock the turn while the player is moving
                player1Path.StartPlayerMovement(player1DiceSideThrown);  // Start movement
                Debug.Log("Player 1 starts moving with dice value: " + player1DiceSideThrown);

                // Hide dice after movement starts
                dice.SetActive(false);
                diceSpriteRenderer.enabled = false;  // Disable sprite rendering
            }
        }
        // Handle CPU's Turn after Player 1 finishes
        else if (!player2TurnFinished)
        {
            // Existing code...

            if (player2DiceSideThrown == 0 && !diceScript.isCPURolling)
            {
                // Start CPU dice roll
                StartCoroutine(diceScript.RollTheDiceForCPU());
                Debug.Log("CPU is rolling the dice.");
            }
            // Prevent multiple dice roll starts
            if (diceScript.isCPURolling)
            {
                // CPU is already rolling, wait
                return;
            }

            if (player2DiceSideThrown == 0)
            {
                // Start CPU dice roll and ensure the animation is visible
                StartCoroutine(diceScript.RollTheDiceForCPU());
                diceScript.RollingAnimation();
                Debug.Log("CPU is rolling the dice.");
            }
            else if (!player2Path.isMoving && !player2InInteraction)
            {
                // Start CPU movement after rolling dice
                isTurnLocked = true;    // Lock the turn while the CPU is moving
                player2Path.StartPlayerMovement(player2DiceSideThrown);  // Start movement
                Debug.Log("Player 2 starts moving with dice value: " + player2DiceSideThrown);

            }
        }
        else
        {
            // Both players have finished their turns, proceed to end of the round
            CheckEndRound();
        }
    }


    /// <summary>
    /// Attempts to unlock the turn. Returns true if the turn was successfully unlocked.
    /// </summary>
    /// <returns>True if the turn was unlocked, false otherwise.</returns>
    public bool UnlockTurn()
    {
        // Only unlock the turn if there are no ongoing interactions and no players are moving
        if (!player1InInteraction && !player2InInteraction &&
            !player1.GetComponent<PATH_SNGL>().isMoving &&
            !player2.GetComponent<PATH_SNGL>().isMoving)
        {
            if (isTurnLocked)
            {
                isTurnLocked = false;
                Debug.Log("Turn unlocked");
                return true;
            }
        }
        return false;
    }




    /// <summary>
    /// Called when a player's movement is complete.
    /// </summary>
    /// <param name="player">The player GameObject whose movement completed.</param>
    /// <summary>
    /// Called when a player's movement is complete.
    /// </summary>
    /// <param name="player">The player GameObject whose movement completed.</param>
    public void PlayerMovementComplete(GameObject player)
    {
        Debug.Log($"PlayerMovementComplete called for {player.name}");

        if (player == player1)
        {
            Debug.Log("Player 1 movement completed. Proceeding to complete turn.");
        }
        else if (player == player2)
        {
            Debug.Log("Player 2 movement completed. Proceeding to complete turn.");
        }

        CompletePlayerTurn(player);
    }

    private void CompletePlayerTurn(GameObject player)
    {
        Debug.Log($"CompletePlayerTurn called for {player.name}");

        var playerPath = player.GetComponent<PATH_SNGL>();
        GameObject currentTile = playerPath.GetCurrentTile();

        if (player == player1 && !player1TurnFinished)
        {
            Debug.Log("Finishing Player 1's turn.");
            FinishPlayer1Turn(currentTile);
        }
        else if (player == player2 && !player2TurnFinished)
        {
            Debug.Log("Finishing Player 2's turn.");
            FinishPlayer2Turn(currentTile);
        }
        else
        {
            Debug.LogWarning($"Attempted to finish turn for {player.name}, but turn is already marked as finished.");
        }
    }

    private void FinishPlayer1Turn(GameObject currentTile)
    {
        Debug.Log("FinishPlayer1Turn started.");
        CheckTileAndAdjustCoins(currentTile, true);

        player1StartWaypoint = player1.GetComponent<PATH_SNGL>().waypointIndex;
        player1TurnFinished = true; // Mark the turn as finished
        player1DiceSideThrown = 0;

        // Prevent the player from rolling the dice during CPU's turn
        diceScript.canRollDice = false;
        diceScript.isRolling = false;
        diceScript.isCPURolling = false;
        player1MoveText?.SetActive(false);
        player2MoveText?.SetActive(true);

        Debug.Log("Player 1 turn finished.");
        if (dice != null)
        {
            dice.SetActive(true);

            var spriteRenderer = dice.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;

            var collider = dice.GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = true;
        }
        // Start CPU's dice roll
        StartCoroutine(diceScript.RollTheDiceForCPU());

        // No need to reset isTurnLocked here; it will be managed in HandlePlayerMovement
    }



    private void FinishPlayer2Turn(GameObject currentTile)
    {
        Debug.Log("FinishPlayer2Turn started.");
        CheckTileAndAdjustCoins(currentTile, false);

        player2StartWaypoint = player2.GetComponent<PATH_SNGL>().waypointIndex;
        player2TurnFinished = true;
        player2DiceSideThrown = 0;

        if (player2MoveText != null) player2MoveText.SetActive(false);
        if (player1MoveText != null) player1MoveText.SetActive(true);
        if (!gameOver && diceScript != null) dice.SetActive(true);

        Debug.Log("Player 2 turn finished.");

        CheckEndRound();
    }
    #endregion

    #region Star Tile Interaction
    /// <summary>
    /// Initiates the STAR tile interaction for the specified player.
    /// </summary>
    /// <param name="player">The player GameObject interacting with the STAR tile.</param>
    public void StartStarTileInteraction(GameObject player)
    {
        dice.SetActive(false);
        currentPlayer = player;
        diceScript.isPlayerRolling = false;
        // Lock the turn as soon as STAR interaction starts
        isTurnLocked = true;

        if (currentPlayer == GAME_CNTRL_SNGL.player1)
        {
            player1InInteraction = true;
            if (starPromptUI != null)
            {
                // Enable the prompt when the player lands on the STAR tile
                starPromptUI.SetActive(true);
                dice.SetActive(false);

                // Pause player movement
                var path = currentPlayer.GetComponent<PATH_SNGL>();
                path.moveAllowed = false;
            }
            else
            {
                Debug.LogError("STAR prompt UI is not assigned!");
            }
        }
        else if (currentPlayer == GAME_CNTRL_SNGL.player2)
        {
            player2InInteraction = true;
            bool buyStar = cpuCoins >= 20; // CPU buys star if it has enough coins
            OnStarPurchaseDecision(buyStar);
        }
    }

    /// <summary>
    /// Handles the player's decision to purchase a STAR tile.
    /// </summary>
    /// <param name="buyStar">True if the player chooses to buy a STAR, false otherwise.</param>
    public void OnStarPurchaseDecision(bool buyStar)
    {
        // Hide the dice at the start of the interaction
        if (dice != null)
        {
            dice.SetActive(false);
            Debug.Log("Dice hidden at the start of star interaction.");
        }

        int starCost = 20; // Example cost for a star
        string resultMessage = "";

        // Hide the "Steps Left" text when the star interaction starts
        if (stepsLeftText != null)
        {
            stepsLeftText.gameObject.SetActive(false);
        }

        if (buyStar)
        {
            audioSource.PlayOneShot(starSFX, 0.7f);
            // Deduct coins from the current player
            if (currentPlayer == GAME_CNTRL_SNGL.player1)
            {
                if (playerCoins >= starCost)
                {
                    playerCoins -= starCost;
                    playerStars++;
                    resultMessage = "Player 1 got a Star!";

                    Debug.Log($"Player 1 bought a star! Coins left: {playerCoins}, Stars: {playerStars}");

                    // Update Player 1 Stars Text
                    if (playerStarsText != null)
                    {
                        playerStarsText.text = playerStars.ToString();
                        Debug.Log($"Player 1 stars updated: {playerStars}");
                    }

                    // Update Player 1 Coins Text
                    if (playerCoinsText != null)
                    {
                        playerCoinsText.text = playerCoins.ToString();
                        Debug.Log($"Player 1 coins updated: {playerCoins}");
                    }

                    // Show coin deduction in red using playerCoinChangeText
                    Debug.Log($"Player 1 showing coin deduction: -{starCost}");
                    StartCoroutine(ShowChangeText(playerCoinChangeText, "-" + starCost.ToString(), Color.red));

                    // Show star gained in yellow using playerStarChangeText
                    Debug.Log($"Player 1 showing star gain: +1");
                    StartCoroutine(ShowChangeText(playerStarChangeText, "+1", new Color(1f, 0.84f, 0f))); // Yellow

                }
                else
                {
                    resultMessage = "Player 1 doesn't have enough coins to buy a Star!";
                    Debug.Log(resultMessage);
                }
            }
            else if (currentPlayer == GAME_CNTRL_SNGL.player2)
            {
                if (cpuCoins >= starCost)
                {
                    cpuCoins -= starCost;
                    cpuStars++;
                    resultMessage = "Player 2 got a Star!";

                    Debug.Log($"Player 2 bought a star! Coins left: {cpuCoins}, Stars: {cpuStars}");

                    // Update CPU Stars Text
                    if (cpuStarsText != null)
                    {
                        cpuStarsText.text = cpuStars.ToString();
                        Debug.Log($"Player 2 stars updated: {cpuStars}");
                    }

                    // Update CPU Coins Text
                    if (cpuCoinsText != null)
                    {
                        cpuCoinsText.text = cpuCoins.ToString();
                        Debug.Log($"Player 2 coins updated: {cpuCoins}");
                    }

                    // Show coin deduction in red using cpuCoinChangeText
                    Debug.Log($"Player 2 showing coin deduction: -{starCost}");
                    StartCoroutine(ShowChangeText(cpuCoinChangeText, "-" + starCost.ToString(), Color.red));

                    // Show star gained in yellow using cpuStarChangeText
                    Debug.Log($"Player 2 showing star gain: +1");
                    StartCoroutine(ShowChangeText(cpuStarChangeText, "+1", new Color(1f, 0.84f, 0f))); // Yellow
                }
                else
                {
                    resultMessage = "Player 2 doesn't have enough coins to buy a Star!";
                    Debug.Log(resultMessage);
                }
            }
        }
        else
        {
            // If no star is bought, indicate it in the result message
            if (currentPlayer == GAME_CNTRL_SNGL.player1)
            {
                resultMessage = "Player 1 chose not to buy a Star.";
                Debug.Log(resultMessage);
            }
            else if (currentPlayer == GAME_CNTRL_SNGL.player2)
            {
                resultMessage = "Player 2 chose not to buy a Star.";
                Debug.Log(resultMessage);
            }
        }

        // Disable the STAR prompt UI (only for Player 1)
        if (starPromptUI != null && currentPlayer == GAME_CNTRL_SNGL.player1)
        {
            starPromptUI.SetActive(false);
        }

        // Call the coroutine to show the decision message with a delay
        StartCoroutine(ShowStarDecisionMessageAndContinue(message: resultMessage));
    }

    // Coroutine to show the decision message and ensure the dice remains hidden
    private IEnumerator ShowStarDecisionMessageAndContinue(string message)
    {
        // Hide the result text initially
        starResultText.gameObject.SetActive(false);

        // Ensure dice stays hidden during the interaction
        if (dice != null)
        {
            dice.SetActive(false);
            Debug.Log("Ensured dice is hidden.");
        }

        // Small delay before showing the decision message (to avoid race conditions)
        yield return new WaitForSeconds(0.1f); // Adjust this delay as necessary

        // Show the decision message
        starResultText.text = message;
        starResultText.gameObject.SetActive(true);
        Debug.Log("Displaying star decision message: " + message);

        // Wait for 2 seconds while the message is displayed
        yield return new WaitForSeconds(1.5f);

        // Hide the result message after 2 seconds
        starResultText.gameObject.SetActive(false);
        Debug.Log("Star decision message hidden.");

        // Re-enable steps left text after interaction is complete
        if (stepsLeftText != null)
        {
            stepsLeftText.gameObject.SetActive(true);
        }

        // Reset interaction flags
        if (currentPlayer == GAME_CNTRL_SNGL.player1)
        {
            player1InInteraction = false;
        }
        else if (currentPlayer == GAME_CNTRL_SNGL.player2)
        {
            player2InInteraction = false;
        }

        // Interaction is complete
        starInteractionComplete = true;

        // Continue the game and complete the player's turn
        yield return new WaitForSeconds(0.5f); // Optional: Small delay to ensure everything is processed
        CompletePlayerTurn(currentPlayer);

        // Attempt to unlock the turn after the interaction is completed
        UnlockTurn();
    }


    private IEnumerator ShowChangeText(TextMeshProUGUI changeText, string message, Color color)
    {
        Debug.Log("Attempting to show text: " + message); // Debugging the text message

        // Set the message and color
        changeText.text = message;
        changeText.color = color;

        // Make the text visible
        changeText.gameObject.SetActive(true);
        Debug.Log(changeText.name + " is now active!"); // Log when the text becomes active

        // Wait for 1 second before fading out
        yield return new WaitForSeconds(1f);

        // Fade out over 0.5 seconds
        float duration = 0.5f;
        float currentTime = 0f;
        Color originalColor = changeText.color;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(originalColor.a, 0f, currentTime / duration);
            changeText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // Reset color and hide the text
        changeText.color = originalColor;
        changeText.gameObject.SetActive(false);
        Debug.Log(changeText.name + " is now hidden."); // Log when the text becomes inactive
    }

    /// <summary>
    /// Displays the STAR interaction result message and resumes the game.
    /// </summary>
    /// <param name="message">The result message to display.</param>
    /// <returns></returns>

    #endregion
    #region Round and Game Logic
    /// <summary>
    /// Checks if the round should end and initiates end round if conditions are met.
    /// </summary>


    private IEnumerator ResetTurnFlagsWithDelay()
    {
        yield return new WaitForSeconds(1f); // Wait 1 second
        player1TurnFinished = false;
        player2TurnFinished = false;
        Debug.Log("player1TurnFinished set to false");
        Debug.Log("player2TurnFinished set to false");
    }

    /// <summary>
    /// Ends the current round and initiates the minigame.
    /// </summary>
    private void EndRound()
    {
        Debug.Log("Ending current round and starting minigame...");

        // Disable the main dice to prevent rolling during the minigame
        if (dice != null)
            dice.SetActive(false);

        // Lock player actions during minigame
        isTurnLocked = true;

        // Show and activate the minigame UI
        if (minigameUI != null)
        {
            minigameUI.SetActive(true);
            player1InInteraction = true;
            player2InInteraction = true;
            Debug.Log("Minigame UI activated.");
        }
        else
        {
            Debug.LogError("Minigame UI GameObject is not assigned in the Inspector!");
        }

        // Start the minigame
        if (MINIGAME_SNGL.instance != null)
        {
            MINIGAME_SNGL.instance.StartMinigame();
            Debug.Log("Minigame started.");
        }
        else
        {
            Debug.LogError("MinigameController instance not found!");
            // If MinigameController is not found, proceed to next round without minigame
            ProceedToNextRound();
        }
    }


    /// <summary>
    /// Checks if the round should end and initiates end round if conditions are met.
    /// </summary>
    private void CheckEndRound()
    {
        if (player1TurnFinished && player2TurnFinished && !player2InInteraction && !player2.GetComponent<PATH_SNGL>().isMoving)
        {
            EndRound();
        }
    }

    /// <summary>
    /// Called when the minigame is completed.
    /// </summary>
    private void OnMinigameFinished()
    {
        Debug.Log("Minigame finished. Proceeding to the next round...");

        // Hide the minigame UI
        if (minigameUI != null)
        {
            minigameUI.SetActive(false);
            player1InInteraction = false;
            player2InInteraction = false;
            Debug.Log("Minigame UI hidden.");
        }

        // Reset dice results to prevent unwanted movement
        player1DiceSideThrown = 0;
        player2DiceSideThrown = 0;

        // Reset turn flags
        player1TurnFinished = false;
        player2TurnFinished = false;

        // Unlock the turn to allow player movement
        isTurnLocked = false;

        // Check if the game is over before enabling the dice
        if (!gameOver)
        {
            // Start the coroutine to enable the dice after a 1.5-second delay
            StartCoroutine(EnableDiceAfterDelay(1.5f));

            diceScript.ResetGameVariables();  // Reset the game variables in Dice.cs

            // Proceed to the next round
            ProceedToNextRound();
        }
    }



    // Coroutine to enable dice after a delay
    private IEnumerator EnableDiceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Enable the dice GameObject
        if (dice != null)
        {
            dice.SetActive(true);  // Enable the GameObject
            Debug.Log("Dice GameObject enabled.");

            // Enable the SpriteRenderer to make the dice visible
            var spriteRenderer = dice.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = 5;  // Ensure it's in front of other UI elements
                Debug.Log("Dice SpriteRenderer enabled.");
            }
            else
            {
                Debug.LogError("Dice does not have a SpriteRenderer component!");
            }

            // Enable the BoxCollider for dice interaction
            var boxCollider = dice.GetComponent<BoxCollider2D>(); // Or BoxCollider for 3D
            if (boxCollider != null)
            {
                boxCollider.enabled = true;
                Debug.Log("Dice BoxCollider enabled.");
            }
            else
            {
                Debug.LogError("Dice does not have a BoxCollider component!");
            }
        }
    }


    /// <summary>
    /// Proceeds to the next round by incrementing the round counter and updating the UI.
    /// Also re-enables the main dice.
    /// </summary>
    private void ProceedToNextRound()
    {
        // Increment the round counter
        currentRound++;
        UpdateRoundUI();
        Debug.Log($"Proceeding to Round {currentRound}.");

        // Check if we've reached the maximum number of rounds
        if (currentRound > maxRounds)
        {
            Debug.Log($"Max rounds reached: {maxRounds}. Determining the winner...");

            // Hide the dice when the game is over
            if (dice != null)
            {
                dice.SetActive(false);
            }

            // Determine the winner since we've reached the final round
            DetermineWinner();
            return;  // Exit early to prevent increasing the round further
        }

        // Reset turn flags
        player1TurnFinished = false;
        player2TurnFinished = false;

        // Unlock the turn to allow player movement
        isTurnLocked = false;

        // Start rolling the dice for the player's turn
        if (dice != null)
        {
            dice.SetActive(true);
            diceScript.StartRolling();
            diceScript.canRollDice = true;
        }
    }




    /// <summary>
    /// Checks if the game has reached the maximum number of rounds and determines the winner if so.
    /// </summary>
    private void CheckWinConditions()
    {
        if (currentRound > maxRounds && !gameOver)
        {
            DetermineWinner();
        }
    }

    /// <summary>
    /// Determines the winner based on stars and coins.
    /// </summary>
    /// 
    private void DetermineWinner()
    {
        currentRound--;
        UpdateRoundUI();
        StartCoroutine(ShowWinScreenAfterDelay());
    }

    private IEnumerator ShowWinScreenAfterDelay()
    {
        // Hide player move texts
        player1MoveText.SetActive(false);
        player2MoveText.SetActive(false);

        if (gameOver) yield break;

        diceScript.canRollDice = false;
        diceScript.isRolling = false;

        Debug.Log("Hiding the dice in DetermineWinner...");
        dice.SetActive(false);  // Log this action
        Debug.Log("Dice should now be hidden.");

        // Wait for 2 seconds
        yield return new WaitForSeconds(1.5f);

        // Determine the winner based on stars and coins
        string winnerText = "";
        gameOver = true;
        dice.SetActive(false);
        if (playerStars > cpuStars)
        {
            winnerText = "Mario (Player) Wins by Stars!";
        }
        else if (cpuStars > playerStars)
        {
            winnerText = "Luigi (CPU) Wins by Stars!";
        }
        else
        {
            // If stars are tied, check coins
            if (playerCoins > cpuCoins)
            {
                winnerText = "Mario (Player) Wins by Coins!";
            }
            else if (cpuCoins > playerCoins)
            {
                winnerText = "Luigi (CPU) Wins by Coins!";
            }
            else
            {
                winnerText = "It's a Tie!";
            }
        }

        // Show the WinScreen and all its child objects
        WinScreen.SetActive(true);

        // Update the whoWinsText to display the winner
        if (whoWinsText != null)
        {
            var winnerTextComponent = whoWinsText.GetComponent<TextMeshProUGUI>();
            if (winnerTextComponent != null)
            {
                winnerTextComponent.text = winnerText;
                Debug.Log($"Winner displayed: {winnerText}");
            }
        }

        // Loop through all children of the WinScreen and activate them
        foreach (Transform child in WinScreen.transform)
        {
            child.gameObject.SetActive(true);
        }

        Debug.Log("WinScreen and all its children are now active.");

        // Set game over state and activate the buttons
        gameOver = true;
        if (restartButton != null) restartButton.SetActive(true);
        if (menuButton != null) menuButton.SetActive(true);

        Debug.Log("Game over: " + winnerText);
    }


    #endregion

    #region Coin and Tile Management
    /// <summary>
    /// Checks the current tile and adjusts coins based on tile type.
    /// </summary>
    /// <param name="currentTile">The tile the player landed on.</param>
    /// <param name="isPlayer">True if the player is Player1, false if CPU.</param>
    public void CheckTileAndAdjustCoins(GameObject currentTile, bool isPlayer)
    {
        Debug.Log($"Checking tile: {currentTile.name} for {(isPlayer ? "Player1" : "CPU")}");

        if (currentTile == null) return;

        if (currentTile.name.StartsWith("B_"))
        {
            AdjustCoins(isPlayer, 3);
        }
        else if (currentTile.name.StartsWith("R_"))
        {
            AdjustCoins(isPlayer, -3);
        }
    }

    /// <summary>
    /// Adjusts the coins for the player or CPU.
    /// </summary>
    /// <param name="isPlayer">True if adjusting Player1's coins, false if CPU.</param>
    /// <param name="amount">The amount to adjust the coins by.</param>
    /// <summary>
    /// Adjusts the coins for the player or CPU and marks the turn as finished.
    /// </summary>
    /// <param name="isPlayer">True if adjusting Player1's coins, false if CPU.</param>
    /// <param name="amount">The amount to adjust the coins by.</param>
    /// <summary>
    /// Adjusts the coins for the player or CPU and updates the UI.
    /// </summary>
    /// <param name="isPlayer">True if adjusting Player1's coins, false if CPU.</param>
    /// <param name="amount">The amount to adjust the coins by.</param>
    public void AdjustCoins(bool isPlayer, int amount)
    {
        Debug.Log($"AdjustCoins called. isPlayer: {isPlayer}, amount: {amount}");

        // Define custom colors
        Color customGreen = new Color(0.2f, 0.7f, 0.2f); // Example green color
        Color customRed = new Color(0.9f, 0.2f, 0.2f);    // Example red color

        int coins;
        TextMeshProUGUI coinsText;
        TextMeshProUGUI coinChangeText;

        if (isPlayer)
        {
            coins = playerCoins = Mathf.Max(0, playerCoins + amount);
            coinsText = playerCoinsText;
            coinChangeText = playerCoinChangeText;
        }
        else
        {
            coins = cpuCoins = Mathf.Max(0, cpuCoins + amount);
            coinsText = cpuCoinsText;
            coinChangeText = cpuCoinChangeText;
        }

        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
            Debug.Log($"{(isPlayer ? "Player 1" : "Player 2")} coins adjusted by {amount}. New total: {coins}");

            // Show the coin change text with custom colors
            if (coinChangeText != null)
            {
                coinChangeText.text = (amount > 0 ? "+" : "") + amount.ToString();
                coinChangeText.color = (amount > 0) ? customGreen : customRed;
                if (amount >0)
                {
                    audioSource.PlayOneShot(winSFX, 0.7f);
                }
                else
                {
                    audioSource.PlayOneShot(loseSFX, 0.7f);
                }
                coinChangeText.gameObject.SetActive(true);

                // Hide the text after a delay using the HideChangeText coroutine
                StartCoroutine(HideChangeText(coinChangeText));
            }
        }
        else
        {
            Debug.LogError($"{(isPlayer ? "Player 1" : "Player 2")} coins text is null! Unable to update UI.");
        }

        Debug.Log($"{(isPlayer ? "Player 1" : "Player 2")}'s turn is finished.");
    }


    private IEnumerator HideChangeText(TextMeshProUGUI changeText)
    {
        // Wait for 1 second
        yield return new WaitForSeconds(1f);

        // Fade out over 0.5 seconds
        float duration = 0.5f;
        float currentTime = 0f;
        Color originalColor = changeText.color;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(originalColor.a, 0f, currentTime / duration);
            changeText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // Reset color and hide the text
        changeText.color = originalColor;
        changeText.gameObject.SetActive(false);
        Debug.Log(changeText.name + " is now hidden."); // Log when the text becomes inactive
    }
    #endregion

    #region UI Updates
    /// <summary>
    /// Updates the coin UI elements.
    /// </summary>
    private void UpdateCoinUI()
    {
        if (playerCoinsText != null) playerCoinsText.text = playerCoins.ToString();
        if (cpuCoinsText != null) cpuCoinsText.text = cpuCoins.ToString();
    }

    /// <summary>
    /// Updates the star UI elements.
    /// </summary>
    private void UpdateStarUI()
    {
        if (playerStarsText != null) playerStarsText.text = playerStars.ToString();
        if (cpuStarsText != null) cpuStarsText.text = cpuStars.ToString();
    }

    /// <summary>
    /// Updates the round UI element.
    /// </summary>
    private void UpdateRoundUI()
    {
        if (roundText != null) roundText.text = $"Round: {currentRound}/{maxRounds}";
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Initiates player movement based on the dice roll.
    /// </summary>
    /// <param name="playerToMove">1 for Player1, 2 for CPU.</param>
    public static void MovePlayer(int playerToMove)
    {
        switch (playerToMove)
        {
            case 1:
                if (GAME_CNTRL_SNGL.player1 != null && !GAME_CNTRL_SNGL.instance.player1InInteraction)
                {
                    int steps = player1DiceSideThrown;
                    var path = GAME_CNTRL_SNGL.player1.GetComponent<PATH_SNGL>();
                    path.StartPlayerMovement(steps);
                    Debug.Log("Player 1 initiated movement.");
                }
                break;
            case 2:
                if (GAME_CNTRL_SNGL.player2 != null && !GAME_CNTRL_SNGL.instance.player2InInteraction)
                {
                    int steps = player2DiceSideThrown;
                    var path = GAME_CNTRL_SNGL.player2.GetComponent<PATH_SNGL>();
                    path.StartPlayerMovement(steps);
                    Debug.Log("Player 2 initiated movement.");
                }
                break;
        }
    }

    /// <summary>
    /// Restarts the game after a short delay.
    /// </summary>
    public void RestartGame()
    {
        StartCoroutine(RestartGameWithDelay());
    }

    private IEnumerator RestartGameWithDelay()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Navigates back to the main menu.
    /// </summary>
    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    #endregion

    #region Coroutines
    /// <summary>
    /// Displays a countdown at the start of the game.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CountdownCoroutine()
    {
        dice.SetActive(false);
        gameStartText.gameObject.SetActive(true);
        gameStartText.text = "Ready.....?";
        Debug.Log("Countdown: Ready.....?");
        yield return new WaitForSeconds(1f);

        gameStartText.text = "Mario Start!";
        Debug.Log("Countdown: Mario Start!");
        yield return new WaitForSeconds(1f);

        gameStartText.gameObject.SetActive(false);
        if (dice != null) dice.SetActive(true);
        Debug.Log("Countdown complete. Dice is now active.");
    }
    #endregion
}
