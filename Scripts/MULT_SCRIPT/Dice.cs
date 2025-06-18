
using UnityEngine;
using System.Collections;
using TMPro;  // Make sure to include TextMeshPro namespace

public class Dice : MonoBehaviour
{
    #region Variables
    [Header("Dice Settings")]
    public Sprite[] diceSides;
    private SpriteRenderer rend;
    private Coroutine rollingCoroutine = null;
    private AudioSource audioSource;
    public AudioClip diceJumpSFX;
    // Base position for absolute jumping
    private Vector3 basePosition;
    public bool CanRoll = true;

    // Reference to GameControl
    public GameControl gameControl;

    [Header("Timer Settings")]
    public TextMeshProUGUI timerText;  // Assign this in the Inspector
    private Coroutine timerCoroutine = null;
    private const float timerDuration = 10f;  // 10 seconds
    #endregion

    #region Initialization
    public void Start()
    {
        audioSource = GetComponent<AudioSource>();
        gameControl = GameControl.instance;

        if (gameControl == null)
        {
            Debug.LogError("GameControl.instance is not set. Ensure that GameControl is initialized before Dice.");
            return;
        }

        rend = GetComponent<SpriteRenderer>();
        basePosition = transform.position;  // Initialize base position
        StartTimer();
        StartRolling();
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
        StopRollingAnimation();
        StopTimer();  // Ensure timer is stopped when dice is disabled
    }
    #endregion

    #region Player Interaction
    private void OnMouseDown()
    {
        // Log the current player's turn status
        Debug.Log($"Is Player 1's Turn: {gameControl.isPlayer1Turn}");


        Debug.Log("It's the player's turn. Rolling the dice...");
        StartCoroutine(RollTheDice());

        // Stop and hide the timer when dice is clicked
        StopTimer();
    }


    #endregion

    #region Dice Rolling Logic
    public void StartRolling()
    {
        if (GameControl.gameOver)
        {
            gameObject.SetActive(false);
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

    public void StopRollingAnimation()
    {
        if (rollingCoroutine != null)
        {
            StopCoroutine(rollingCoroutine);
            rollingCoroutine = null;
            Debug.Log("Rolling animation stopped.");
        }
    }

    public IEnumerator RollingAnimation()
    {
        while (true)  // Infinite loop until stopped
        {
            int randomDiceSide = Random.Range(0, diceSides.Length);
            rend.sprite = diceSides[randomDiceSide];
            yield return new WaitForSeconds(0.05f);
        }
    }
    #endregion

    #region Dice Roll Coroutine
    public IEnumerator RollTheDice()
    {
        StopRollingAnimation();

        // Final result of the dice roll
        if (diceSides == null || diceSides.Length == 0)
        {
            Debug.LogError("Dice sides are not assigned.");
            yield break;
        }

        int randomDiceSide = Random.Range(0, diceSides.Length);
        rend.sprite = diceSides[randomDiceSide];
        int diceResult = randomDiceSide + 1;
        // Flash and jump animation after rolling
        yield return StartCoroutine(FlashAndJump());

        // Optional: Wait before sending the result
        yield return new WaitForSeconds(1f);

        // Send dice result to GameControl
        gameControl.OnDiceRollComplete(diceResult);
    }
    #endregion

    #region Utility: Flash and Jump Animation
    public IEnumerator FlashAndJump()
    {
        if (!CanRoll)
        {
            Debug.Log("Not the player's turn, skipping flash and jump.");
            yield break;  // Exit the coroutine early if it's not the player's turn
        }
        audioSource.PlayOneShot(diceJumpSFX, 0.7f);
        Color originalColor = rend.color;

        // Flash effect
        rend.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        rend.color = originalColor;

        // Jump up
        transform.position = basePosition + new Vector3(0, 0.5f, 0);  // Jump up
        yield return new WaitForSeconds(0.1f);

        // Return to base position
        transform.position = basePosition;
    }
    #endregion

    #region Timer Logic
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

        // Wait for 1 second before hiding the message
        yield return new WaitForSeconds(1f);
        StartCoroutine(RollTheDice());
        timerText.gameObject.SetActive(false);
    }
    #endregion
}
