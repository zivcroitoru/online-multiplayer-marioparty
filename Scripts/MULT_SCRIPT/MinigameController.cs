using TMPro;
using UnityEngine;
using System.Collections;

public class MinigameController : MonoBehaviour
{
    #region Singleton Instance
    public static MinigameController instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    #endregion

    #region Public Variables
    [Header("Dice Settings")]
    public Sprite[] diceSides; // Array for dice face sprites, assign via Inspector
    public SpriteRenderer minigameDiceRenderer; // Reference to the SpriteRenderer for the dice
    public GameControl gameControl;

    [Header("UI Elements")]
    public TextMeshProUGUI resultText; // UI Text to display results
    public TextMeshProUGUI timerText;  // Assign this in the Inspector
    public GameObject minigameUI; // UI Panel for the minigame
    public bool CanRoll = true;
    private Coroutine rollingCoroutine = null;

    [Header("Audio Clips")]
    public AudioClip diceJumpSFX;
    public AudioClip startSFX;
    public AudioClip winSFX;
    public AudioClip loseSFX;

    //// Tracking if players have rolled
    private AudioSource audioSource;
    #endregion

    #region Private Variables
    private SpriteRenderer rend;
    private Vector3 basePosition;
    private const float timerDuration = 10f;  // 10 seconds
    private Coroutine timerCoroutine = null;
    #endregion

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Detect left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Cast a ray from the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            // Check if the ray hits something
            if (hit.collider != null)
            {
                GameObject clickedObject = hit.collider.gameObject;
                Debug.Log("Clicked on: " + clickedObject.name);

                // Check if the clicked object is the dice
                if (clickedObject == minigameDiceRenderer.gameObject)
                {
                    OnDiceClicked(clickedObject);  // Handle the dice click
                }
            }
        }
    }

    /// <summary>
    /// Initializes and starts the minigame for Player 1.
    /// </summary>
    public void StartMinigame()
    {
        // Initialize audio source
        audioSource.PlayOneShot(startSFX, 0.7f);

        // Reset minigame state
        minigameUI.SetActive(true);
        resultText.text = "";
        rend = minigameDiceRenderer; // Assign the SpriteRenderer reference
        basePosition = minigameDiceRenderer.transform.position;  // Initialize base position

        // Make dice active
        minigameDiceRenderer.gameObject.SetActive(true);

        Debug.Log("Minigame started: Player 1");

        // Start rolling the dice with animation and timer
        StartRolling();  // The dice starts rolling here continuously
    }

    /// <summary>
    /// Handles the event when the dice is clicked.
    /// </summary>
    /// <param name="clickedDice">The dice GameObject that was clicked.</param>
    public void OnDiceClicked(GameObject clickedDice)
    {
        Debug.Log("Dice clicked: " + clickedDice.name);
        // Stop the rolling animation and timer, then finalize the roll
        StopRollingAnimation();
        StopTimer();
        StartCoroutine(FinalizeDiceRoll(clickedDice));
    }

    #region Dice Rolling Logic

    /// <summary>
    /// Initiates the rolling animation and starts the timer.
    /// </summary>
    public void StartRolling()
    {
        if (GameControl.gameOver)
        {
            minigameDiceRenderer.gameObject.SetActive(false);
            return;
        }

        if (rollingCoroutine == null)
        {
            rollingCoroutine = StartCoroutine(RollingAnimation());  // Start the rolling animation
            Debug.Log("Rolling animation started.");

            // Start the timer when rolling starts
            StartTimer();
        }
    }

    /// <summary>
    /// Stops the rolling animation coroutine.
    /// </summary>
    public void StopRollingAnimation()
    {
        if (rollingCoroutine != null)
        {
            StopCoroutine(rollingCoroutine);
            rollingCoroutine = null;
            Debug.Log("Rolling animation stopped.");
        }
    }

    /// <summary>
    /// Continuously changes the dice sprite to simulate rolling.
    /// </summary>
    /// <returns>IEnumerator for coroutine.</returns>
    private IEnumerator RollingAnimation()
    {
        while (true)  // Infinite loop until stopped
        {
            int randomDiceSide = Random.Range(0, diceSides.Length);
            minigameDiceRenderer.sprite = diceSides[randomDiceSide];
            yield return new WaitForSeconds(0.05f);  // Control the speed of dice rolling animation
        }
    }

    #endregion

    #region Dice Roll Finalization

    /// <summary>
    /// Finalizes the dice roll by selecting a random side, performing animations, and notifying the GameControl.
    /// </summary>
    /// <param name="diceObject">The dice GameObject to finalize.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    private IEnumerator FinalizeDiceRoll(GameObject diceObject)
    {
        // Ensure the diceObject is valid
        if (diceObject == null)
        {
            Debug.LogError("[FinalizeDiceRoll] diceObject is null!");
            yield break;
        }

        // Check if diceSides is assigned
        if (diceSides == null || diceSides.Length == 0)
        {
            Debug.LogError("[FinalizeDiceRoll] diceSides is not assigned or is empty.");
            yield break;
        }

        // Get the SpriteRenderer
        SpriteRenderer diceRenderer = diceObject.GetComponent<SpriteRenderer>();
        if (diceRenderer == null)
        {
            Debug.LogError("[FinalizeDiceRoll] diceObject does not have a SpriteRenderer component.");
            yield break;
        }

        // Select the final dice side
        int randomDiceSide = Random.Range(0, diceSides.Length);
        diceRenderer.sprite = diceSides[randomDiceSide];
        int diceResult = randomDiceSide + 1;
        Debug.Log($"[FinalizeDiceRoll] Dice rolled: {diceResult}");

        // Play flash and jump animation
        yield return StartCoroutine(FlashAndJump(diceObject));

        // Play dice jump sound effect
        audioSource.PlayOneShot(diceJumpSFX, 0.7f);

        // Optional: Wait before sending the result
        yield return new WaitForSeconds(1f);

        // Send dice result to GameControl
        gameControl.MiniGameDiceRollComplete(diceResult);

        // Optionally, reset the minigame or proceed to next steps
        // For example, hide the minigame UI after the roll
        // minigameUI.SetActive(false);
    }

    #endregion

    #region Utility: Flash and Jump Animation

    /// <summary>
    /// Provides visual feedback by flashing the dice color and making it jump.
    /// </summary>
    /// <param name="diceObject">The dice GameObject to animate.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    private IEnumerator FlashAndJump(GameObject diceObject)
    {
        if (!CanRoll)
        {
            Debug.Log("Not the player's turn, skipping flash and jump.");
            yield break;  // Exit the coroutine early if it's not the player's turn
        }

        SpriteRenderer diceRenderer = diceObject.GetComponent<SpriteRenderer>();
        Color originalColor = diceRenderer.color;

        // Flash effect
        diceRenderer.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        diceRenderer.color = originalColor;

        // Jump up
        Vector3 originalPosition = diceObject.transform.position;
        diceObject.transform.position = originalPosition + new Vector3(0, 0.5f, 0);  // Jump up
        yield return new WaitForSeconds(0.1f);

        // Return to base position
        diceObject.transform.position = originalPosition;
    }

    #endregion

    #region Timer Logic

    /// <summary>
    /// Starts the countdown timer for the dice roll.
    /// </summary>
    private void StartTimer()
    {
        if (timerText == null)
        {
            Debug.LogError("Timer Text is not assigned in the Inspector!");
            return;
        }

        timerText.gameObject.SetActive(true);
        timerCoroutine = StartCoroutine(TimerCountdown());
    }

    /// <summary>
    /// Stops the countdown timer.
    /// </summary>
    private void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            Debug.Log("Timer stopped.");
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Handles the countdown logic and automatic dice roll when time is up.
    /// </summary>
    /// <returns>IEnumerator for coroutine.</returns>
    private IEnumerator TimerCountdown()
    {
        float remainingTime = timerDuration;

        while (remainingTime > 0)
        {
            timerText.SetText($"{Mathf.CeilToInt(remainingTime)}");
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        // Time's Up
        timerText.SetText("Time's Up!");
        Debug.Log("Time's Up!");

        // Play lose sound effect
        audioSource.PlayOneShot(loseSFX, 0.7f);

        // Wait for 1 second before finalizing the dice roll
        yield return new WaitForSeconds(1f);

        // Finalize the dice roll automatically
        StartCoroutine(FinalizeDiceRoll(minigameDiceRenderer.gameObject));

        timerText.gameObject.SetActive(false);
    }

    #endregion
}
