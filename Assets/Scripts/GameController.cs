using UnityEngine;
using System.Collections;
using TMPro; // Zorg dat je deze hebt voor TextMeshPro

public class GameController : MonoBehaviour
{
    // Singleton instance zodat andere scripts er makkelijk bij kunnen
    public static GameController instance;

    public GameObject[] players = new GameObject[6]; // Array om spelers vast te houden
    public GameObject dice1, dice2;
    public TextMeshProUGUI gameStatusText; // Sleep dit in de inspector of laat de Find staan

    public int id = 0;
    public int currentPlayerNumber = 1;
    public int numberOfPlayers = 6;

    public bool isTurnInProgress = false; // Voorkomt dat je kunt klikken tijdens het lopen
    public bool gameFinished = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Het is beter om referenties in de Inspector te slepen, maar Find werkt ook:
        dice1 = GameObject.Find("Dice1");
        dice2 = GameObject.Find("Dice2");

        players[0] = GameObject.Find("Player1");
        players[1] = GameObject.Find("Player2");
        players[2] = GameObject.Find("Player3");
        players[3] = GameObject.Find("Player4");
        players[4] = GameObject.Find("Player5");
        players[5] = GameObject.Find("Player6");

        GameObject statusObj = GameObject.Find("GameStatus");
        if (statusObj != null)
            gameStatusText = statusObj.GetComponent<TextMeshProUGUI>();

        // Zet het aantal spelers correct bij de start, minimaal 2 spelers
        numberOfPlayers = Mathf.Max(numberOfPlayers, 2);
        for (int i = numberOfPlayers + 1; i <= 6; i++)
        {
            GameObject playerObj = GameObject.Find("Player" + i);
            if (playerObj != null)
                playerObj.SetActive(false);
        }

        UpdateUI();
    }

    // Deze wordt aangeroepen vanuit InputHandler
    public void OnDiceClicked()
    {
        if (!isTurnInProgress && !gameFinished)
        {
            StartCoroutine(PlayTurn());
        }
    }

    private IEnumerator PlayTurn()
    {
        isTurnInProgress = true;
        PlayerState currentPlayer = players[currentPlayerNumber - 1].GetComponent<PlayerState>();

        if (currentPlayer.numberOfRoundsToWait > 0)
        {
            // Speler moet wachten, dus sla beurt over
            currentPlayer.numberOfRoundsToWait--;
            gameStatusText.text = $"Player {currentPlayerNumber} skips a turn!";
            yield return new WaitForSeconds(1f);

            SetNextPlayer();
            yield break; // Stop de coroutine
        }

        gameStatusText.text = $"Player {currentPlayerNumber} is rolling...";

        // 1. Rol de dobbelstenen
        RollDice r1 = dice1.GetComponent<RollDice>();
        RollDice r2 = dice2.GetComponent<RollDice>();

        StartCoroutine(r1.RollTheDice());
        StartCoroutine(r2.RollTheDice());

        // Wacht tot de dobbelstenen klaar zijn met rollen
        yield return new WaitUntil(() => !r1.isRolling && !r2.isRolling);

        // 2a. Bereken initieel target en beweeg daar naartoe
        int initialTarget = DetermineNextWaypoint(currentPlayer.currentWaypoint, r1.diceValue, r2.diceValue);
        if (initialTarget > 63)
        {
            Debug.Log("Overshoot detected!");
            yield return StartCoroutine(currentPlayer.MoveToWaypoint(63));

            int overshoot = initialTarget - 63; // Bijv. 65 - 63 = 2 stappen teveel
            int finalDestination = 63 - overshoot; // 63 - 2 = 61

            if (IsGooseTile(finalDestination))
            {
                Debug.Log("Gans! Nog eens " + (r1.diceValue + r2.diceValue) + " stappen terug!");
                finalDestination = finalDestination - (r1.diceValue + r2.diceValue);
            }

            yield return StartCoroutine(currentPlayer.MoveToWaypoint(finalDestination));
        }
        else
        {
            yield return StartCoroutine(currentPlayer.MoveToWaypoint(initialTarget));
        }

        // 2b. Behandel speciale vakjes (brug, doolhof, dood)
        int finalTarget = HandleSpecialTiles(currentPlayer.currentWaypoint);
        if (finalTarget != initialTarget)
        {
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(currentPlayer.MoveToWaypoint(finalTarget));
        }

        // 2c. Controleer speciale vakjes voor het overslaan van rondes
        int roundsToWait = DetermineRoundsToWait(finalTarget);
        if (roundsToWait > 0)
        {
            currentPlayer.numberOfRoundsToWait = roundsToWait;
        }

        // 3. als er precies op 63 wordt geëindigd, is het spel voorbij
        if (initialTarget == 63)
        {
            gameFinished = true;
            gameStatusText.text = $"Player {currentPlayerNumber} wins!";
            isTurnInProgress = false;
            yield break; // Stop de coroutine
        }

        // 4. Beurt wisselen
        SetNextPlayer();
    }

    private void SetNextPlayer()
    {
        currentPlayerNumber++;
        if (currentPlayerNumber > numberOfPlayers)
        {
            currentPlayerNumber = 1;
        }

        isTurnInProgress = false;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (gameStatusText != null)
            gameStatusText.text = $"Player {currentPlayerNumber}'s turn";
    }

    private int DetermineNextWaypoint(int currentWaypoint, int dice1Value, int dice2Value)
    {
        int nextWaypoint = currentWaypoint + dice1Value + dice2Value;

        if (currentWaypoint == 0 && (dice1Value + dice2Value) == 9)
        {
            // Wie bij de eerste worp een 5 en een 4 gooit, gaat meteen door naar 53. 
            if ((dice1Value == 5 && dice2Value == 4) || dice1Value == 4 && dice2Value == 5)
            {
                Debug.Log("Eerste worp 5 en 4! Naar 53!");
                return 53;
            }

            // Wie bij de eerste worp een 6 en een 3 gooit, gaat door naar 26.
            if ((dice1Value == 6 && dice2Value == 3) || dice1Value == 3 && dice2Value == 6)
            {
                Debug.Log("Eerste worp 6 en 3! Naar 26!");
                return 26;
            }
        }

        // Gans logica (Recursief)
        if (IsGooseTile(nextWaypoint))
        {
            Debug.Log("Gans! Nog eens " + (dice1Value + dice2Value));
            return DetermineNextWaypoint(nextWaypoint, dice1Value, dice2Value);
        }

        return nextWaypoint;
    }

    private int HandleSpecialTiles(int currentWaypoint)
    {
        // Brug
        if (currentWaypoint == 6)
        {
            Debug.Log("Brug! Naar 12!");
            return 12;
        }

        // Doolhof
        if (currentWaypoint == 42)
        {
            Debug.Log("Doolhof! Naar 37!");
            return 37;
        }

        // Dood
        if (currentWaypoint == 58)
        {
            Debug.Log("Dood! Terug naar start!");
            return 0;
        }

        return currentWaypoint; // Geen verandering
    }

    private bool IsGooseTile(int index)
    {
        return index == 5 || index == 9 || index == 14 || index == 18 || index == 23 ||
               index == 27 || index == 32 || index == 36 || index == 41 || index == 45 ||
               index == 50 || index == 54 || index == 59;
    }

    private int DetermineRoundsToWait(int nextWaypoint)
    {
        // Herberg
        if (nextWaypoint == 19)
        {
            Debug.Log("Herberg! Wacht 1 ronde!");
            return 1;
        }

        // Put
        if (nextWaypoint == 31)
        {
            Debug.Log("Put! Wacht 2 rondes!");
            return 2;
        }

        // Gevangenis
        if (nextWaypoint == 52)
        {
            Debug.Log("Gevangenis! Wacht 2 rondes!");
            return 2;
        }

        return 0; // Geen wachtrondes
    }
}