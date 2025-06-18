using TMPro;
using UnityEngine;
using System.Collections;

public class MINIGAME_SNGL : MonoBehaviour
{
    #region Singleton Instance
    public static MINIGAME_SNGL instance;
    public GameObject mainDice;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    #endregion

    public delegate void MinigameCompleted();
    public static event MinigameCompleted OnMinigameCompleted;

    #region Public Variables
    public Sprite[] diceSides; // Array for dice face sprites, assign via Inspector
    public SpriteRenderer playerDiceRenderer; // Reference to player's SpriteRenderer, assign via Inspector
    public SpriteRenderer cpuDiceRenderer; // Reference to CPU's SpriteRenderer, assign via Inspector
    public TextMeshProUGUI resultText; // UI Text to display results
    public GameObject minigameUI; // UI Panel for the minigame
    public GameObject minigameBackground; // Reference to the minigame background, assign via Inspector
    #endregion

    [Header("Audio Clips")]
    public AudioClip diceJumpSFX;
    public AudioClip startSFX;
    public AudioClip winSFX;
    public AudioClip loseSFX;

    private AudioSource audioSource;
    #region Private Variables
    private bool playerHasClicked = false;
    private bool isPlayerRolling = false;
    private bool isCPURolling = false;
    private int playerResult = 0;
    private int cpuResult = 0;
    private Coroutine playerRollCoroutine;
    private Coroutine cpuRollCoroutine;
    #endregion
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Optional: Start the minigame automatically for testing
        // StartMinigame();
    }
    public void StartMinigame()
    {
      // audioSource.PlayOneShot(startSFX, 0.7f);

        // Disable the main dice when the minigame starts
        if (mainDice != null)
        {
            mainDice.SetActive(false);
            Debug.Log("Main dice disabled during minigame.");
        }

        playerHasClicked = false;
        playerResult = 0;
        cpuResult = 0;
        isCPURolling = true;

        Debug.Log("Minigame has started!");

        StartCoroutine(RunMinigame());
    }

    private IEnumerator RunMinigame()
    {
        if (minigameUI != null)
            minigameUI.SetActive(true);

        Debug.Log("Minigame is running. Both dice are rolling...");

        resultText.text = "";
        playerResult = 0;
        cpuResult = 0;

        // Start rolling both dice
        playerRollCoroutine = StartCoroutine(RollPlayerDice());
        cpuRollCoroutine = StartCoroutine(RollCPUDice());

        // Wait for the player to stop rolling
        yield return StartCoroutine(WaitForPlayerToStop());

        // Assign the final player result
        playerResult = Mathf.Clamp(Random.Range(1, 7), 1, 6);
        SetDiceSprite(playerDiceRenderer, playerResult);
        Debug.Log($"Player rolled: {playerResult}");

        // Ensure CPU has finished rolling
        yield return new WaitUntil(() => !isCPURolling);
        Debug.Log($"CPU rolled: {cpuResult}");

        yield return new WaitForSeconds(2f);

        DetermineWinner();

        Debug.Log("Minigame has ended!");

        if (minigameUI != null)
            minigameUI.SetActive(false);

        // Re-enable the main dice after the minigame ends
        if (mainDice != null)
        {
            mainDice.SetActive(true);
            Debug.Log("Main dice re-enabled after minigame.");
        }

        // Trigger the event to notify that the minigame has ended
        OnMinigameCompleted?.Invoke();
    }

    private IEnumerator FlashAndJump(SpriteRenderer diceRenderer)
    {
        if (diceRenderer != null)
        {
            Debug.Log($"FlashAndJump: Starting animation for {diceRenderer.name}.");
            audioSource.PlayOneShot(diceJumpSFX, 0.7f);

        }

        Vector3 basePosition = diceRenderer.transform.position;
        Debug.Log($"FlashAndJump: Initial Base Position: {basePosition}");

        // Simulate the dice jumping up
        diceRenderer.transform.position = new Vector3(basePosition.x, basePosition.y + 0.5f, basePosition.z);
        Debug.Log($"FlashAndJump: Dice jumps to Position: {diceRenderer.transform.position}");
        yield return new WaitForSeconds(0.1f);

        // Return the dice to its original position
        diceRenderer.transform.position = basePosition;
        Debug.Log($"FlashAndJump: Dice returns to base position: {basePosition}");
        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator RollCPUDice()
    {
        isCPURolling = true;
        float elapsedTime = 0f;
        float rollDuration = 1.5f;

        while (elapsedTime < rollDuration)
        {
            int randomDiceSide = Random.Range(0, diceSides.Length);
            cpuDiceRenderer.sprite = diceSides[randomDiceSide];
            Debug.Log($"CPU Dice Rolling... Showing side: {randomDiceSide + 1}");
            yield return new WaitForSeconds(0.05f);
            elapsedTime += 0.05f;
        }

        // Assign final CPU result
        cpuResult = Mathf.Clamp(Random.Range(1, 7), 1, 6);
        SetDiceSprite(cpuDiceRenderer, cpuResult);
        Debug.Log($"CPU final roll: {cpuResult}");

        // Animate CPU dice
        StartCoroutine(FlashAndJump(cpuDiceRenderer));

        isCPURolling = false;
    }

    private IEnumerator WaitForPlayerToStop()
    {
        Debug.Log("Waiting for player to click the dice...");

        while (!playerHasClicked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

                if (hit.collider != null && hit.transform.gameObject == playerDiceRenderer.gameObject)
                {
                    playerHasClicked = true;
                    Debug.Log("Player clicked the dice!");

                    // Stop the rolling coroutine
                    if (playerRollCoroutine != null)
                        StopCoroutine(playerRollCoroutine);

                    // Assign final player result
                    playerResult = Mathf.Clamp(Random.Range(1, 7), 1, 6);
                    SetDiceSprite(playerDiceRenderer, playerResult);
                    Debug.Log($"Player final roll: {playerResult}");

                    // Animate player dice
                    StartCoroutine(FlashAndJump(playerDiceRenderer));
                }
            }
            yield return null;
        }
    }

    private void DetermineWinner()
    {
        string message = "";

        if (GAME_CNTRL_SNGL.instance == null)
        {
            Debug.LogError("GAME_CNTRL_SNGL.instance is null!");
            return; // Early return to avoid further issues
        }

        if (resultText == null)
        {
            Debug.LogError("resultText is not assigned!");
            return;
        }

        if (minigameBackground == null)
        {
            Debug.LogError("minigameBackground is not assigned!");
            return;
        }

        if (playerResult > cpuResult)
        {
            message = "You Win the Minigame!\n<color=green>+10 Coins</color>";
          //  audioSource.PlayOneShot(winSFX, 0.7f);
            GAME_CNTRL_SNGL.instance.AdjustCoins(true, 10);
            GAME_CNTRL_SNGL.instance.AdjustCoins(false, -10);
        }
        else if (cpuResult > playerResult)
        {
            message = "CPU Wins the Minigame!\n<color=red>-10 Coins</color>";
         //   audioSource.PlayOneShot(loseSFX, 0.7f);
            GAME_CNTRL_SNGL.instance.AdjustCoins(false, 10);
            GAME_CNTRL_SNGL.instance.AdjustCoins(true, -10);
        }
        else
        {
            message = "It's a Tie!";
            audioSource.PlayOneShot(loseSFX, 0.7f);
        }

        Debug.Log($"Player result: {playerResult}, CPU result: {cpuResult}");
        Debug.Log("Winner Message: " + message);

        if (!minigameBackground.activeSelf)
        {
            minigameBackground.SetActive(true);
            Debug.Log("minigameBackground activated.");
        }

        resultText.gameObject.SetActive(true);
        resultText.text = message;
        Debug.Log("Result Text Set: " + resultText.text);

        StartCoroutine(HideResultAndPanelAfterDelay(1.5f));
    }


    private IEnumerator HideResultAndPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
            Debug.Log("Result Text Hidden");
        }
        else
        {
            Debug.LogError("resultText is missing when trying to hide it!");
        }

        if (minigameUI != null)
        {
            minigameUI.SetActive(false);
            Debug.Log("Minigame UI Panel Hidden");
        }
    }

    private IEnumerator RollPlayerDice()
    {
        Debug.Log("Player dice rolling starts.");

        isPlayerRolling = true;

        while (isPlayerRolling)
        {
            int randomDiceSide = Random.Range(0, diceSides.Length);
            playerDiceRenderer.sprite = diceSides[randomDiceSide];
            Debug.Log($"Player Dice Rolling... Showing side: {randomDiceSide + 1}");
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("Player dice rolling stopped.");
    }

    private void SetDiceSprite(SpriteRenderer diceRenderer, int result)
    {
        if (diceRenderer != null && diceSides != null && result >= 1 && result <= diceSides.Length)
        {
            diceRenderer.sprite = diceSides[result - 1];
            Debug.Log($"{diceRenderer.name} set to side {result}");
        }
        else
        {
            Debug.LogError("Invalid dice sprite assignment!");
        }
    }
}
