using AssemblyCSharp;
using com.shephertz.app42.gaming.multiplayer.client;
using com.shephertz.app42.gaming.multiplayer.client.command;
using com.shephertz.app42.gaming.multiplayer.client.events;
using com.shephertz.app42.gaming.multiplayer.client.SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameControl : MonoBehaviour
{
    #region Singleton Instance
    public static GameControl instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // Ensure that GameControl is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject); // Make sure it's a root object
                Debug.Log("[Awake] GameControl instance created and set as singleton.");
            }
            else
            {
                Debug.LogWarning("[Awake] GameControl is not a root object. It may not persist correctly.");
            }
        }
        else
        {
            Debug.LogWarning("[Awake] Duplicate GameControl instance found. Destroying the new one.");
            Destroy(gameObject);
        }

        // Reset static variables
        player1 = null;
        player2 = null;
        player1DiceSideThrown = 0;
        player2DiceSideThrown = 0;
        player1StartWaypoint = 0;
        player2StartWaypoint = 0;
        isPlayer1Turn = true;
        gameOver = false;
        _unityObjects = new Dictionary<string, GameObject>();
        GameObject[] curObjects = GameObject.FindGameObjectsWithTag("UnityObject");
        foreach (GameObject g in curObjects)
        {
            if (!_unityObjects.ContainsKey(g.name))
                _unityObjects.Add(g.name, g);
        }

        _timerObject = _unityObjects.ContainsKey("Txt_Timer") ? _unityObjects["Txt_Timer"]?.GetComponent<TextMeshProUGUI>() : null;
        Debug.Log("[Awake] Static variables reset and Unity objects initialized.");
    }
    #endregion

    #region Public Variables
    public bool starInteractionComplete = false;
    public GameObject starPromptUI;
    public TextMeshProUGUI player1StarsText;
    public TextMeshProUGUI player2StarsText;
    public Dice diceScript;
    public TextMeshProUGUI player1CoinsText;
    public TextMeshProUGUI player2CoinsText;
    public GameObject dice;
    public GameObject restartButton;
    public GameObject menuButton;
    public GameObject minigameDice;
    public bool MiniGameActive = false;
    private bool _matchIsOver = false;
    public bool finishedMG = false;
    public TextMeshProUGUI minigameStartMessage;
    public bool isPlayer1Turn;
    public GameObject gameParent;
    public TextMeshProUGUI stepsLeftText;
    public TextMeshProUGUI gameStartText;
    private bool _stoppedGame;
    public int maxRounds;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI minigameResultText;
    private bool hasCompletedTurn = false;
    public TextMeshProUGUI starResultText;
    public bool player1TurnCompleted = false;
    public bool player2TurnCompleted = false;
    public string currentTurnId; // Tracks whose turn it is
    public GameObject WinScreen;
    public GameObject minigameUI;
    public TextMeshProUGUI movePromptText;
    #endregion

    #region Private Variables
    [Header("Coin Change Texts")]
    public TextMeshProUGUI player1CoinChangeText;
    public TextMeshProUGUI player2CoinChangeText;

    [Header("Star Change Texts")]
    public TextMeshProUGUI player1StarChangeText;
    public TextMeshProUGUI player2StarChangeText;

    private Dictionary<string, GameObject> _unityObjects;
    public TextMeshProUGUI whoWinsText;
    public static GameObject player1, player2;
    public int player1DiceSideThrown = 0;
    public int player2DiceSideThrown = 0;
    public static int player1StartWaypoint = 0;
    public static int player2StartWaypoint = 0;
    public string localPlayerId;
    public int player1MiniGameRes; // Coins earned/lost by Player 1 in mini-game
    public int player2MiniGameRes; // Coins earned/lost by Player 2 in mini-game
    public static bool gameOver = false;
    public int player1Coins = 5;
    public int player2Coins = 5;
    private int player1Stars = 0;
    private int player2Stars = 0;

    // Variables for AppWarp multiplayer
    public static string player1Id;
    public static string player2Id;
    private string roomId;

    // Reference to Timer (if used)
    private TextMeshProUGUI _timerObject;

    private int currentRound = 1;
    public bool player1InInteraction = false;
    public bool player2InInteraction = false;


    // Variables to store minigame dice roll results
    private int player1MinigameDiceRollResult = 0;
    private int player2MinigameDiceRollResult = 0;
    private bool hasPlayer1RolledMinigameDice = false;
    private bool hasPlayer2RolledMinigameDice = false;


    private AudioSource audioSource;
    public AudioClip winSFX;
    public AudioClip loseSFX;
    public AudioClip starSFX;
    public AudioClip music;
    public AudioClip WinScreenSFX;

    #endregion

    #region Initialization
    public void Init()
    {

        audioSource = GetComponent<AudioSource>();

        audioSource.clip = music; 
        audioSource.Play(); 
        gameParent = gameParent ?? GameObject.Find("Game");

        if (gameParent?.activeInHierarchy == true)
        {
            Debug.Log("Game parent object enabled, searching for children...");
            player1 = gameParent?.transform.Find("Player1")?.gameObject;
            player2 = gameParent?.transform.Find("Player2")?.gameObject;
            dice = gameParent?.transform.Find("Dice")?.gameObject;
            InitializeReferences();
            Debug.Log("[OnEnable] Player1, Player2, and Dice initialized.");
        }
        else
        {
            Debug.LogError("Game parent is not assigned or not enabled.");
        }
    }

    private void OnEnable()
    {
        Listener.OnGameStarted += OnGameStarted;
        Listener.OnMoveCompleted += OnMoveCompleted;
        Listener.OnGameStopped += OnGameStopped;
        Debug.Log("[OnEnable] Event listeners registered.");
    }

    private void OnDisable()
    {
        Listener.OnGameStarted -= OnGameStarted;
        Listener.OnMoveCompleted -= OnMoveCompleted;
        Listener.OnGameStopped -= OnGameStopped;
        Debug.Log("[OnDisable] Event listeners unregistered.");
    }

    private void Start()
    {

        // Call ResetGame to ensure everything starts fresh
    
    }

    private IEnumerator InitializePlayers()
    {
        Debug.Log("[InitializePlayers] Initializing players...");

        // Log current user ID
        Debug.Log($"[InitializePlayers] Current User ID: {GlobalVariables.UserId}");

        // Assign the local player's ID
        localPlayerId = GlobalVariables.UserId;

        // Assign player1Id or player2Id based on who joins first
        if (string.IsNullOrEmpty(player1Id))
        {
            player1Id = localPlayerId;  // Local player is Player 1
            Debug.Log($"[InitializePlayers] Assigned local player as Player 1 with ID: {player1Id}");
        }
        else if (string.IsNullOrEmpty(player2Id))
        {
            player2Id = localPlayerId;  // Local player is Player 2
            Debug.Log($"[InitializePlayers] Assigned local player as Player 2 with ID: {player2Id}");
        }
        else
        {
            Debug.LogError("[InitializePlayers] Both player1Id and player2Id are already assigned.");
        }

        yield return null;
    }

    private void InitializeReferences()
    {
        dice = GameObject.Find("Main_Dice");
        diceScript = dice?.GetComponent<Dice>();
    }

    private void HideUIElements()
    {
        restartButton?.SetActive(false);
        menuButton?.SetActive(false);
        WinScreen?.SetActive(false);
        Debug.Log("[HideUIElements] UI elements hidden.");
    }
    #endregion

    #region Turn Management


    public void SwitchTurn()
    {
        // Determine if we're switching turns after a minigame or a normal turn
        if (finishedMG)
        {
            // Handle turn switching after minigame
            finishedMG = false; // Reset the flag first

            // Decide whether to switch turns or keep the same player based on your game logic
            // For example, if you want to switch to the next player after the minigame:
            if (currentTurnId == player1Id)
            {
                currentTurnId = player2Id;
                Debug.Log("[SwitchTurn] Mini-game finished. It's now Player 2's turn.");
            }
            else
            {
                currentTurnId = player1Id;
                Debug.Log("[SwitchTurn] Mini-game finished. It's now Player 1's turn.");
            }
        }
        else
        {
            // Normal turn switching
            if (currentTurnId == player1Id)
            {
                currentTurnId = player2Id;
                Debug.Log("[SwitchTurn] It's now Player 2's turn.");
            }
            else
            {
                currentTurnId = player1Id;

                Debug.Log("[SwitchTurn] It's now Player 1's turn.");
            }
        }
        isPlayer1Turn = !isPlayer1Turn;
        // Update UI after turn switching
        UpdateTurnUI();

        // Debug log to verify the turn state
        Debug.Log($"[SwitchTurn] isPlayer1Turn now: {isPlayer1Turn}, currentTurnId: {currentTurnId}");
    }


    #endregion

    #region AppWarp Event Handlers
    private void OnGameStarted(string sender, string roomId, string curr_Turn)
    {

        ResetGame();
        Debug.Log("[Start] GameControl Start initiated.");

        // Initialize references and set up UI

        // Start the game in AppWarp multiplayer
        finishedMG = false;
        HideUIElements();

        localPlayerId = GlobalVariables.UserId;
        Debug.Log("[Start] GameControl Start initiated.");

        movePromptText = movePromptText ?? GameObject.Find("MovePromptText")?.GetComponent<TextMeshProUGUI>();

        if (dice == null)
        {
            Debug.LogError("Dice GameObject is not assigned in the Inspector!");
            return;
        }

        dice.SetActive(true);
        player1InInteraction = false;
        player2InInteraction = false;
        player1CoinChangeText?.gameObject.SetActive(false);
        player2CoinChangeText?.gameObject.SetActive(false);
        player1StarChangeText?.gameObject.SetActive(false);
        player2StarChangeText?.gameObject.SetActive(false);
        minigameUI?.SetActive(false);

        InitializeReferences();
        StartCoroutine(InitializePlayers());
        //WarpClient.GetInstance().startGame();
        Debug.Log("[Start] Game initialized and game start signal sent to WarpClient.");
        Debug.Log($"[OnGameStarted] Game started by {sender} in room {roomId}. Current turn: {curr_Turn}");

        movePromptText.text = $"Your Turn";
        // Assign the room ID
        this.roomId = roomId;

        // Request live room info asynchronously, result will be handled in the listener
        WarpClient.GetInstance().GetLiveRoomInfo(roomId);

        // Set the current turn ID based on server instruction
        currentTurnId = curr_Turn;

        UpdateTurnUI();
    }

    private void OnGameStopped(string _Sender, string _RoomId)
    {
        Debug.Log("OnGameStopped " + _Sender + " " + _RoomId);
        _stoppedGame = true;
    }
    private void OnMoveCompleted(MoveEvent moveEvent)
    {
        // Check if the received MoveEvent is null
        if (moveEvent == null)
        {
            Debug.LogError("[OnMoveCompleted] Received a null MoveEvent.");
            ResetGame(); // Reset the game if no event is received
            return;
        }

        Debug.Log($"[OnMoveCompleted] MoveEvent received: {moveEvent}");

        // Retrieve the move data in JSON format
        string jsonData = moveEvent.getMoveData();
        Debug.Log("Received move data: " + (string.IsNullOrEmpty(jsonData) ? "No data" : jsonData));

        // Deserialize JSON data into a dictionary for easy access to keys and values
        Dictionary<string, object> moveData = MiniJSON.Json.Deserialize(jsonData) as Dictionary<string, object>;

        // Check if deserialization was successful
        if (moveData == null)
        {
            Debug.LogWarning("[OnMoveCompleted] Move data deserialization failed. Resetting the game.");
            ResetGame(); // Reset the game if deserialization fails
            return;
        }

        // Proceed only if the 'Action' key exists in the move data
        if (moveData.ContainsKey("Action"))
        {
            string action = moveData["Action"].ToString();
            Debug.Log($"[OnMoveCompleted] Action received: {action}");

            // Special handling for the 'EndTurn' action to prevent double switching
            if (action == "EndTurn")
            {
                // Verify if the action was initiated by the local player
                if (moveData.ContainsKey("PlayerId") && moveData["PlayerId"].ToString() == localPlayerId)
                {
                    Debug.Log("[OnMoveCompleted] EndTurn action sent by local player. Ignoring.");
                    return;  // Exit to avoid processing the action again
                }
            }

            // If the match is already over, stop the game to prevent further actions
            if (_matchIsOver)
            {
                Debug.Log("[OnMoveCompleted] Match is already over. Stopping the game.");
                WarpClient.GetInstance().stopGame();
                return; // Early exit if the match has concluded
            }

            // Handle different actions based on the 'Action' value
            switch (action)
            {
                case "DiceRoll":
                    HandleDiceRoll(moveData); // Process a dice roll action
                    break;
                case "EndTurn":
                    HandleEndTurn(moveData); // Process the end of a turn
                    break;
                case "MiniGame_DiceRoll":
                    HandleMinigameDiceRoll(moveData); // Handle a dice roll within a mini-game
                    break;
                case "EndGame":
                    Debug.Log("[OnMoveCompleted] EndGame action received. Ending the game.");
                    EndGame(); // Finalize and end the game
                    break;
                default:
                    Debug.LogWarning($"[OnMoveCompleted] Unhandled action: {action}"); // Log any unexpected actions
                    break;
            }
        }
        else
        {
            Debug.LogWarning("[OnMoveCompleted] 'Action' key is missing in move data."); // Warn if 'Action' key is absent
        }
    }

    private void EndGame()
    {
        if (_matchIsOver)
        {
            Debug.Log("[EndGame] Match is already over.");
            return;
        }

        _matchIsOver = true; // Set the match over flag

        // Optionally, send a signal to stop the game session
        WarpClient.GetInstance().stopGame();

        WarpClient.GetInstance().startGame();
        OnGameStarted(currentTurnId, roomId, currentTurnId);

        //// Start the coroutine to show the win screen
        //StartCoroutine(ShowWinScreenAfterDelay());

        Debug.Log("[EndGame] Game ending process initiated.");
    }


    private void HandleDiceRoll(Dictionary<string, object> moveData)
    {
        if (moveData.ContainsKey("DiceRoll") && moveData.ContainsKey("IsPlayer1"))
        {
            int diceRoll = int.Parse(moveData["DiceRoll"].ToString());
            bool isPlayer1Roll = bool.Parse(moveData["IsPlayer1"].ToString());

            Debug.Log($"[HandleDiceRoll] Received dice roll: {diceRoll} from {(isPlayer1Roll ? "Player 1" : "Player 2")}");

            // Apply the roll to the correct player
            if (isPlayer1Roll)
            {
                player1DiceSideThrown = diceRoll;
                player1?.GetComponent<FollowThePath>()?.StartPlayerMovement(diceRoll);
                Debug.Log("[HandleDiceRoll] Player 1 movement started.");
            }
            else
            {
                player2DiceSideThrown = diceRoll;
                player2?.GetComponent<FollowThePath>()?.StartPlayerMovement(diceRoll);
                Debug.Log("[HandleDiceRoll] Player 2 movement started.");
            }
        }
        else
        {
            Debug.LogError("Invalid move data received. Missing 'DiceRoll' or 'IsPlayer1' keys.");
        }
    }
    private void HandleEndTurn(Dictionary<string, object> moveData)
    {
        Debug.Log("[HandleEndTurn] Handling end of turn.");
        if (!isPlayer1Turn && !MiniGameActive)
        {
            MiniGameActive = true;
            SwitchTurn();
            StartMiniGame();
        }
        if (!isPlayer1Turn && MiniGameActive)
        {
            DetermineMiniGameWinner();
            SwitchTurn();
        }
        if (isPlayer1Turn)
        {
            SwitchTurn();
        }

    }
    private void StartMiniGame()
    {
        hasPlayer2RolledMinigameDice = false;
        hasPlayer1RolledMinigameDice = false;
        player1MinigameDiceRollResult = 0;
        player2MinigameDiceRollResult = 0;
        StartCoroutine(StartMinigameSequence());
    }

    private IEnumerator StartMinigameSequence()
    {
        if (dice.activeSelf)
        {
            Debug.LogWarning("[Dice] Dice was re-enabled elsewhere.");
        }
        Debug.Log("[StartMiniGame] Player 2 has finished their turn. Starting minigame...");
        dice.SetActive(false);
        minigameStartMessage.gameObject.SetActive(true);
        minigameStartMessage.text = "Starting minigame...";
        yield return new WaitForSeconds(1.5f);
        minigameStartMessage.gameObject.SetActive(false);
        minigameUI.SetActive(true);
        SwitchTurn();
        MinigameController.instance.StartMinigame();
    }

    private void HandleMinigameDiceRoll(Dictionary<string, object> moveData)
    {
        if (moveData.ContainsKey("MiniGame_DiceRoll") && moveData.ContainsKey("IsPlayer1"))
        {
            bool diceParsed = int.TryParse(moveData["MiniGame_DiceRoll"].ToString(), out int diceRoll);
            bool playerParsed = bool.TryParse(moveData["IsPlayer1"].ToString(), out bool isPlayer1Roll);
            Debug.Log($"Dice Parsed: {diceParsed}, Player Parsed: {playerParsed}");
            Debug.Log($"Dice Roll Value: {moveData["MiniGame_DiceRoll"]}, IsPlayer1: {moveData["IsPlayer1"]}");
            if (diceParsed && playerParsed)
            {
                if (isPlayer1Roll)
                {
                    player1MinigameDiceRollResult = diceRoll;
                    hasPlayer1RolledMinigameDice = true;
                    Debug.Log($"MG : Player 1 ROLLED: {diceRoll}");
                    SwitchTurn();
                }
                else
                {
                    player2MinigameDiceRollResult = diceRoll;
                    hasPlayer2RolledMinigameDice = true;
                    Debug.Log($"MG : Player 2 ROLLED: {diceRoll}");
                }
                if (hasPlayer1RolledMinigameDice && hasPlayer2RolledMinigameDice)
                {
                    DetermineMiniGameWinner();
                }
            }
        }
    }
    private void DetermineMiniGameWinner()
    {
        minigameDice.SetActive(false);
        Debug.Log($"Winner is being determined...");
        string resultMessage = $"Determining winner:\nPlayer 1 rolled {player1MinigameDiceRollResult}\nPlayer 2 rolled {player2MinigameDiceRollResult}\n";

        if (player1MinigameDiceRollResult > player2MinigameDiceRollResult)
        {
            // Player 1 wins
            Debug.Log($"Player 1 wins...");
            resultMessage += "Player 1 wins the MiniGame!";
            AdjustCoins(true, 10); // Player 1 gains 10 coins
            AdjustCoins(false, -10); // Player 2 loses 10 coins
        }
        else if (player2MinigameDiceRollResult > player1MinigameDiceRollResult)
        {
            // Player 2 wins
            Debug.Log($"Player 2 wins...");
            resultMessage += "Player 2 wins the MiniGame!";
            AdjustCoins(false, 10); // Player 2 gains 10 coins
            AdjustCoins(true, -10); // Player 1 loses 10 coins
        }
        else
        {
            resultMessage += "The minigame is a tie!";

        }
        if (minigameResultText != null)
        {
            minigameResultText.SetText(resultMessage);
            minigameResultText.gameObject.SetActive(true);
            Debug.Log("Result text set and activated.");
        }
        else
        {
            Debug.LogError("ResultText UI element is not assigned.");
        }
        StartCoroutine(HandlePostMinigameSequence());
    }
    private IEnumerator HandlePostMinigameSequence()
    {
        yield return new WaitForSeconds(2f);

        if (minigameResultText != null)
        {
            minigameResultText.gameObject.SetActive(false);
        }
        minigameUI.SetActive(false);

        currentRound++;
        UpdateRoundUI();
        MiniGameActive = false;
        Debug.Log("Minigame done. Now Player 1 starts.");
        finishedMG = true;
        SwitchTurn();
        dice.SetActive(true); // Activate the main dice
    }

    #endregion

    #region Player Movement
    private void HandlePlayerMovement()
    {
        if (currentTurnId != GlobalVariables.UserId)
        {
            Debug.Log("[HandlePlayerMovement] Not this player's turn. Movement skipped.");
            return;
        }

        var playerPath = currentTurnId == player1Id ? player1?.GetComponent<FollowThePath>() : player2?.GetComponent<FollowThePath>();

        if (playerPath?.isMoving == false && (currentTurnId == player1Id ? player1DiceSideThrown : player2DiceSideThrown) > 0)
        {
            playerPath?.StartPlayerMovement(currentTurnId == player1Id ? player1DiceSideThrown : player2DiceSideThrown);
            Debug.Log("[HandlePlayerMovement] Player movement initiated based on dice roll.");
            // Optionally disable the dice here if needed
        }
    }
    public void Player_MINIGAME_DICE_Complete(GameObject player)
    {
        Debug.Log($"[PlayerMovementComplete] Movement complete for player: {player.name}");
        CompletePlayerMINIGAMETurn(player);
    }


    public void PlayerMovementComplete(GameObject player)
    {
        Debug.Log($"[PlayerMovementComplete] Movement complete for player: {player.name}");
        CompletePlayerTurn(player);
    }

    public void OnDiceRollComplete(int diceValue)
    {
        Debug.Log($"[OnDiceRollComplete] Dice rolled: {diceValue}");

        // Send the dice roll to the server
        Dictionary<string, object> moveData = new Dictionary<string, object>
        {
            { "Action", "DiceRoll" },
            { "DiceRoll", diceValue },
            { "IsPlayer1", isPlayer1Turn }
        };

        SendMove(moveData);
    }
    public void MiniGameDiceRollComplete(int diceValue)
    {
        Debug.Log($"[MiniGameDiceRollComplete] Dice rolled: {diceValue}");

        // Send the dice roll to the server
        Dictionary<string, object> moveData = new Dictionary<string, object>
        {
            { "Action", "MiniGame_DiceRoll" },
            { "MiniGame_DiceRoll", diceValue },
            { "IsPlayer1", isPlayer1Turn }
        };

        SendMove(moveData);
    }
    private void CompletePlayerTurn(GameObject player)
    {
        Debug.Log($"[CompletePlayerTurn] Completing turn for {player.name}");

        if (hasCompletedTurn)
        {
            Debug.Log("[CompletePlayerTurn] Turn already completed.");
            return;
        }

        hasCompletedTurn = true;

        var playerPath = player?.GetComponent<FollowThePath>();
        GameObject currentTile = playerPath?.GetCurrentTile();

        // Adjust coins or stars based on the player's position
        CheckTileAndAdjustCoins(currentTile, player == player1);
        Debug.Log("[CompletePlayerTurn] Coins adjusted based on the current tile.");

        // Send data to switch turns, including PlayerId
        Dictionary<string, object> moveData = new Dictionary<string, object>
        {
            { "Action", "EndTurn" },
            { "PlayerId", localPlayerId }  // Include PlayerId
        };
        SendMove(moveData);

        hasCompletedTurn = false;
        Debug.Log("[CompletePlayerTurn] EndTurn action sent.");
    }
    #endregion

    private void CompletePlayerMINIGAMETurn(GameObject player)
    {
        Debug.Log($"[CompletePlayerMINIGAMETurn] Completing turn for {player.name}");

        if (hasCompletedTurn)
        {
            Debug.Log("[CompletePlayerTurn] Turn already completed.");
            return;
        }
        hasCompletedTurn = true;
        // Send data to switch turns, including PlayerId
        Dictionary<string, object> moveData = new Dictionary<string, object>
        {
            { "Action", "EndTurn" },
            { "PlayerId", localPlayerId }  // Include PlayerId
        };
        SendMove(moveData);

        hasCompletedTurn = false;
        Debug.Log("[CompletePlayerMINIGAMETurn] EndTurn action sent.");
    }
    #region Coin and Tile Management
    public void CheckTileAndAdjustCoins(GameObject currentTile, bool isPlayer1)
    {
        if (currentTile == null)
        {
            Debug.LogWarning("Current tile is null. No coins adjusted.");
            return;
        }

        Debug.Log($"[CheckTileAndAdjustCoins] Checking tile: {currentTile.name} for player: {(isPlayer1 ? "Player1" : "Player2")}");

        // Handle Blue tiles
        if (currentTile.name.StartsWith("B_"))
        {
            Debug.Log("[CheckTileAndAdjustCoins] Blue tile detected. Adding 3 coins.");
            AdjustCoins(isPlayer1, 3);
        }
        // Handle Red tiles
        else if (currentTile.name.StartsWith("R_"))
        {
            Debug.Log("[CheckTileAndAdjustCoins] Red tile detected. Subtracting 3 coins.");
            AdjustCoins(isPlayer1, -3);
        }
        // Handle Star tiles
        else if (currentTile.name.StartsWith("STAR"))
        {
            Debug.Log("[CheckTileAndAdjustCoins] Star tile detected.");
            // Check if the player has more than 20 coins
            int playerCoins = isPlayer1 ? player1Coins : player2Coins;
            if (playerCoins >= 20)
            {
                audioSource.PlayOneShot(starSFX, 0.7f);
                // Deduct 20 coins and increase star count
                AdjustCoins(isPlayer1, -20);
                AdjustStars(isPlayer1, 1);

                // Display message about buying a star
                string playerName = isPlayer1 ? "Player 1" : "Player 2";
                ShowStarPurchaseMessage($"{playerName} bought a star for 20 coins!");
            }
            else
            {
                Debug.Log("[CheckTileAndAdjustCoins] Not enough coins to buy a star.");
            }
        }
        else
        {
            Debug.Log("[CheckTileAndAdjustCoins] Neutral or unknown tile. No coins adjusted.");
        }
    }

    public void AdjustCoins(bool isPlayer1, int amount)
    {
        TextMeshProUGUI changeText = isPlayer1 ? player1CoinChangeText : player2CoinChangeText;

        if (isPlayer1)
        {
            player1Coins = Mathf.Max(0, player1Coins + amount);  // Ensure coins don't go negative
            player1CoinsText.SetText(player1Coins.ToString());   // Update coin text UI for Player 1
        }
        else
        {
            player2Coins = Mathf.Max(0, player2Coins + amount);  // Ensure coins don't go negative
            player2CoinsText.SetText(player2Coins.ToString());   // Update coin text UI for Player 2
        }

        if (changeText != null)
        {
            // Reactivate the change text and reset its color and transparency
            changeText.gameObject.SetActive(true);
            changeText.color = amount > 0 ? Color.green : Color.red;
            changeText.alpha = 1f;  // Reset transparency
            if (amount > 0)
            {
                audioSource.PlayOneShot(winSFX, 0.7f);
            }
            else
            {
                audioSource.PlayOneShot(loseSFX, 0.7f);
            }
            // Display the coin change with a "+" for positive numbers
            string change = amount > 0 ? $"+{amount}" : amount.ToString();
            changeText.SetText(change);

            // Start the fade-out coroutine
            StartCoroutine(HideChangeText(changeText));
        }
        else
        {
            Debug.LogError("ChangeText UI element is not assigned.");
        }

        Debug.Log($"Adjusted coins by {amount}. Player {(isPlayer1 ? "1" : "2")} now has {(isPlayer1 ? player1Coins : player2Coins)} coins.");
    }


    private void AdjustStars(bool isPlayer1, int amount)
    {
        TextMeshProUGUI changeText = isPlayer1 ? player1StarChangeText : player2StarChangeText;

        if (isPlayer1)
        {
            player1Stars += amount;
            player1StarsText.SetText(player1Stars.ToString());   // Update star count UI for Player 1
        }
        else
        {
            player2Stars += amount;
            player2StarsText.SetText(player2Stars.ToString());   // Update star count UI for Player 2
        }

        if (changeText != null)
        {
            // Reactivate the change text and reset its color and transparency
            changeText.gameObject.SetActive(true);
            changeText.color = amount > 0 ? Color.green : Color.red;
            changeText.alpha = 1f;  // Reset transparency

            // Display the star change with a "+" for positive numbers
            string change = amount > 0 ? $"+{amount}" : amount.ToString();
            changeText.SetText(change);

            // Start the fade-out coroutine
            StartCoroutine(HideChangeText(changeText));
        }
        else
        {
            Debug.LogError("ChangeText UI element is not assigned.");
        }

        Debug.Log($"Player {(isPlayer1 ? "1" : "2")} star count increased by {amount}. New star count: {(isPlayer1 ? player1Stars : player2Stars)}");
    }

    private void ShowStarPurchaseMessage(string message)
    {
        // Display the star purchase message on the screen
        starResultText.SetText(message);
        starResultText.gameObject.SetActive(true);

        Debug.Log(message);

        // Optional: Hide the message after a short delay
        StartCoroutine(HideStarPurchaseMessage());
    }

    private IEnumerator HideStarPurchaseMessage()
    {
        yield return new WaitForSeconds(2f);  // Wait for 2 seconds
        starResultText.gameObject.SetActive(false);  // Hide the message
    }

    private IEnumerator HideChangeText(TextMeshProUGUI changeText)
    {
        if (changeText == null)
        {
            Debug.LogError("ChangeText is null. Cannot hide.");
            yield break;
        }
        yield return new WaitForSeconds(0.5f);

        float duration = 0.5f; // Duration of the fade
        float currentTime = 0f;
        Color originalColor = changeText.color;

        // Fade out over the specified duration
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, currentTime / duration);  // Fade alpha from 1 to 0
            changeText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // Ensure the text is fully transparent and deactivate it
        changeText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        changeText.gameObject.SetActive(false);

        Debug.Log("[HideChangeText] Change text hidden after fade out.");
    }



    #endregion

    private void Update()
    {
        CheckWinConditions();

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Dice") && MiniGameActive)
                {
                    SwitchTurn();
                }
            }
        }
    }

    #region UI Updates
    private void UpdateCoinUI()
    {
        player1CoinsText?.SetText(player1Coins.ToString());
        player2CoinsText?.SetText(player2Coins.ToString());
        Debug.Log("[UpdateCoinUI] Coin UI updated for both players.");
    }

    private void UpdateStarUI()
    {
        player1StarsText?.SetText(player1Stars.ToString());
        player2StarsText?.SetText(player2Stars.ToString());
        Debug.Log("[UpdateStarUI] Star UI updated for both players.");
    }

    private void UpdateRoundUI()
    {
        roundText?.SetText($"Round: {currentRound}/{maxRounds}");
        Debug.Log($"[UpdateRoundUI] Round UI updated to: Round {currentRound} of {maxRounds}.");
    }
    #endregion

    #region Networking: Send Move
    public void SendMove(Dictionary<string, object> moveData)
    {
        if (moveData == null || moveData.Count == 0)
        {
            Debug.LogError("Move data is null or empty. Cannot send move.");
            return;
        }

        // Serialize the dictionary into a JSON string
        string json = MiniJSON.Json.Serialize(moveData);
        Debug.Log("Sending move data: " + json);  // Debug log for sent data

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Failed to serialize move data!");
            return;
        }

        // Send the serialized data to the server
        WarpClient.GetInstance().sendMove(json);  // Ensure you're calling the correct method here
        Debug.Log("Move sent successfully: " + json);

    }
    #endregion

    #region Win Conditions
    private void CheckWinConditions()
    {
        Debug.Log($"[CheckWinConditions] Current Round: {currentRound}, Max Rounds: {maxRounds}, Game Over: {gameOver}");
        if (currentRound > maxRounds)
        {
            DetermineWinner();
        }
    }

    private void DetermineWinner()
    {
        audioSource.clip = music;   // Assign the music clip
        audioSource.Stop();         // Stop the currently playing clip
        Debug.Log("[DetermineWinner] Determining the winner...");
        StartCoroutine(ShowWinScreenAfterDelay());
    }

    private IEnumerator ShowWinScreenAfterDelay()
    {
        Debug.Log("[ShowWinScreenAfterDelay] Preparing to show win screen...");

        // Hide move prompt and round text
        movePromptText?.gameObject.SetActive(false);
        roundText?.gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);

        // Determine the winner based on stars and coins
        string winnerText = "Restarting Game...";
        gameOver = true;
        WinScreen.SetActive(true);  // Activate the win screen
        // Activate all child objects of the WinScreen
        if (WinScreen != null)
        {
            foreach (Transform child in WinScreen.transform)
            {
                child.gameObject.SetActive(true);
            }

            // Determine winner based on stars or coins
            if (player1Stars > player2Stars)
            {
                winnerText = "Mario (Player 1) Wins by Stars!";
            }
            else if (player2Stars > player1Stars)
            {
                winnerText = "Luigi (Player 2) Wins by Stars!";

            }
            else
            {
                // If stars are tied, check coins
                if (player1Coins > player2Coins)
                {
                    winnerText = "Mario (Player 1) Wins by Coins!";

                }
                else if (player2Coins > player1Coins)
                {
                    winnerText = "Luigi (Player 2) Wins by Coins!";
                }
                else
                {
                    winnerText = "Restarting Game...";
                    audioSource.Stop();
                   // WinScreen.SetActive(false);  // Deactivate the win screen in case of a tie
                }
            }

            // Update the winner text on the UI
            if (whoWinsText != null)
            {
                whoWinsText.text = winnerText;
            }
            else
            {
                Debug.LogError("[ShowWinScreenAfterDelay] whoWinsText is not assigned!");
            }

            // Activate the buttons
            if (restartButton != null) restartButton.SetActive(true);
            else Debug.LogError("[ShowWinScreenAfterDelay] Restart button is not assigned!");

            if (menuButton != null) menuButton.SetActive(true);
            else Debug.LogError("[ShowWinScreenAfterDelay] Menu button is not assigned!");

            Debug.Log("Game over: " + winnerText);
        }
        else
        {
            Debug.LogError("[ShowWinScreenAfterDelay] WinScreen is not assigned!");
        }
    }


    #endregion

    private void UpdateTurnUI()
    {
        if (movePromptText == null)
        {
            Debug.LogError("MovePromptText is not assigned in the Inspector.");
            return;
        }

        if (localPlayerId == currentTurnId)
        {
            // It's this player's turn
            movePromptText.text = "Your Turn";
            movePromptText.color = Color.green;

            // Enable interaction and set dice to normal color
            if (dice != null)
            {
                Renderer diceRenderer = dice.GetComponent<Renderer>();
                if (diceRenderer != null)
                {
                    diceRenderer.material.color = Color.white; // Set to normal color
                }

                Collider diceCollider = dice.GetComponent<Collider>();
                if (diceCollider != null)
                {
                    diceCollider.enabled = true; // Enable interaction
                }
            }

            if (minigameDice != null)
            {
                Renderer minigameDiceRenderer = minigameDice.GetComponent<Renderer>();
                if (minigameDiceRenderer != null)
                {
                    minigameDiceRenderer.material.color = Color.white; // Set to normal color
                }

                Collider minigameDiceCollider = minigameDice.GetComponent<Collider>();
                if (minigameDiceCollider != null)
                {
                    minigameDiceCollider.enabled = true; // Enable interaction
                }
            }
        }
        else
        {
            // It's the opponent's turn
            movePromptText.text = "Waiting for opponent...";
            movePromptText.color = Color.red;

            // Disable interaction and grey out dice
            if (dice != null)
            {
                Renderer diceRenderer = dice.GetComponent<Renderer>();
                if (diceRenderer != null)
                {
                    diceRenderer.material.color = Color.grey; // Set to greyed-out color
                }

                Collider diceCollider = dice.GetComponent<Collider>();
                if (diceCollider != null)
                {
                    diceCollider.enabled = false; // Disable interaction
                }
            }

            if (minigameDice != null)
            {
                Renderer minigameDiceRenderer = minigameDice.GetComponent<Renderer>();
                if (minigameDiceRenderer != null)
                {
                    minigameDiceRenderer.material.color = Color.grey; // Set to greyed-out color
                }

                Collider minigameDiceCollider = minigameDice.GetComponent<Collider>();
                if (minigameDiceCollider != null)
                {
                    minigameDiceCollider.enabled = false; // Disable interaction
                }
            }
        }

        Debug.Log($"[UpdateTurnUI] CurrentTurnId: {currentTurnId}, LocalPlayerId: {localPlayerId}. UI Updated to: {movePromptText.text}");
    }

    public void ResetGame()
    {
        WinScreen.SetActive(false);
        WinScreen.gameObject.SetActive(false);
        Init();
        InitializePlayers();
        InitializeReferences();
        UpdateTurnUI();
        UpdateRoundUI();
        diceScript.Start();
        movePromptText.gameObject.SetActive( true );
        roundText.gameObject.SetActive( true );
        dice.gameObject.SetActive( true );
        Debug.Log("[ResetGame] Resetting game state...");
        _stoppedGame = false; // Add this line to reset _stoppedGame
        _matchIsOver = false;
        gameOver = false;
        currentRound = 1;
        player1Coins = 5;
        player2Coins = 5;
        player1Stars = 0;
        player2Stars = 0;
        starInteractionComplete = false;

        // Reset dice and minigame states
        player1DiceSideThrown = 0;
        player2DiceSideThrown = 0;
        player1StartWaypoint = 0;
        player2StartWaypoint = 0;
        isPlayer1Turn = true;
        MiniGameActive = false;
        hasPlayer1RolledMinigameDice = false;
        hasPlayer2RolledMinigameDice = false;
        player1MinigameDiceRollResult = 0;
        player2MinigameDiceRollResult = 0;

        // Hide UI elements
  

        // Reset player positions
        if (player1 != null)
        {
            var followPath1 = player1.GetComponent<FollowThePath>();
            if (followPath1 != null)
            {
                followPath1.ResetPlayer();
            }
            else
            {
                Debug.LogError("[ResetGame] FollowThePath component missing on Player1.");
            }
        }
        else
        {
            Debug.LogError("[ResetGame] Player1 is null.");
        }

        if (player2 != null)
        {
            var followPath2 = player2.GetComponent<FollowThePath>();
            if (followPath2 != null)
            {
                followPath2.ResetPlayer();
            }
            else
            {
                Debug.LogError("[ResetGame] FollowThePath component missing on Player2.");
            }
        }
        else
        {
            Debug.LogError("[ResetGame] Player2 is null.");
        }

        // Reset minigame UI
        if (WinScreen != null)
        {
            WinScreen.SetActive(false);
        }
        else
        {
            Debug.LogError("[ResetGame] WinScreen is not assigned.");
        }

        if (minigameUI != null)
        {
            minigameUI.SetActive(false);
        }
        else
        {
            Debug.LogError("[ResetGame] MinigameUI is not assigned.");
        }

        // Reactivate necessary objects
        if (dice != null)
        {
            dice.SetActive(true);
        }
        else
        {
            Debug.LogError("[ResetGame] Dice GameObject is not assigned.");
        }

        Debug.Log("[ResetGame] Game variables and state reset.");
    }


    public void Btn_Restart()
    {
        if (_stoppedGame)
        {
            WarpClient.GetInstance().startGame();
        }
    }

}