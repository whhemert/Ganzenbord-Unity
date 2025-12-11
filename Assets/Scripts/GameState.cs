using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking; // Nodig voor API calls
using System.Text; // Nodig voor Encoding

public class GameState : MonoBehaviour
{
    public static GameState instance;

    [Header("API Configuration")]
    [Tooltip("De URL van je .NET Core backend endpoint")]
    public string apiUrl = "http://localhost:5216/api/game";

    public bool isSaving = false;
    public bool isLoading = false;

    void Awake()
    {
        instance = this;
    }

    public void Save()
    {
        if (!isSaving && !GameController.instance.gameFinished)
        {
            StartCoroutine(SaveToDisk());
        }
    }

    private IEnumerator SaveToDisk()
    {
        isSaving = true;

        GameData gameData = new GameData(
            GameController.instance.id,
            GameController.instance.currentPlayerNumber,
            GameController.instance.numberOfPlayers);

        for (int i = 1; i <= GameController.instance.numberOfPlayers; i++)
        {
            GameObject player = GameController.instance.players[i - 1];
            PlayerState playerState = player.GetComponent<PlayerState>();

            GameData.PlayerData playerData = new GameData.PlayerData
            {
                currentWaypoint = playerState.currentWaypoint,
                numberOfRoundsToWait = playerState.numberOfRoundsToWait
            };

            gameData.AddPlayerData(i, playerData);
        }

        string json = JsonConvert.SerializeObject(gameData, Formatting.Indented);

        // 3. Request opbouwen (POST wordt gebruikt voor Upsert in onze backend)
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. Versturen en wachten
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error saving game: {request.error}");
            }
            else
            {
                Debug.Log($"Game saved successfully! Response: {request.downloadHandler.text}");

                // 5. Het nieuwe ID opslaan dat we terugkrijgen van de backend
                // Als dit een CREATE was, hebben we nu een ID gekregen.
                GameData responseData = JsonConvert.DeserializeObject<GameData>(request.downloadHandler.text);
                if (responseData != null && responseData.id != 0)
                {
                    GameController.instance.id = responseData.id;
                    Debug.Log($"Game ID updated to: {GameController.instance.id}");
                }
            }
        }

        isSaving = false;
    }

    public void Load()
    {
        if (!isLoading)
        {
            StartCoroutine(LoadFromDisk());
        }
    }

    private IEnumerator LoadFromDisk()
    {
        isLoading = true;

        string urlWithId = $"{apiUrl}/{GameController.instance.id}";
        Debug.Log($"Loading game from: {urlWithId}");

        using (UnityWebRequest request = UnityWebRequest.Get(urlWithId))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error loading game: {request.error}");
            }
            else
            {
                string json = request.downloadHandler.text;
                Debug.Log("Data received: " + json);

                try
                {
                    GameData data = JsonConvert.DeserializeObject<GameData>(json);

                    // Update Game Controller
                    GameController.instance.gameFinished = false;
                    GameController.instance.isTurnInProgress = false;
                    
                    GameController.instance.id = data.id;
                    GameController.instance.currentPlayerNumber = data.currentPlayerNumber;
                    GameController.instance.numberOfPlayers = data.numberOfPlayers;

                    for (int i = 1; i <= 6; i++)
                    {
                        GameObject player = GameController.instance.players[i - 1];
                        if (i <= data.numberOfPlayers && player != null)
                        {
                            player.SetActive(true);
                            PlayerState playerState = player.GetComponent<PlayerState>();

                            if (data.players.TryGetValue(i, out GameData.PlayerData playerData))
                            {
                                playerState.currentWaypoint = playerData.currentWaypoint;
                                playerState.numberOfRoundsToWait = playerData.numberOfRoundsToWait;
                                player.transform.position = playerState.waypoints[playerState.currentWaypoint].position;
                            }
                        } else
                        {
                            if (player != null)
                                player.SetActive(false);
                        }
                    }

                    GameController.instance.UpdateUI();
                    Debug.Log("Game state applied to scene.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error parsing save data: {ex.Message}");
                }
            }
        }

        isLoading = false;
    }

    [System.Serializable]
    private class GameData
    {
        public int id;
        public int currentPlayerNumber;
        public int numberOfPlayers;
        public Dictionary<int, PlayerData> players;

        public GameData(int id, int currentPlayerNumber, int numberOfPlayers)
        {
            this.id = id;
            this.currentPlayerNumber = currentPlayerNumber;
            this.numberOfPlayers = numberOfPlayers;
            players = new Dictionary<int, PlayerData>();
        }

        public void AddPlayerData(int playerNumber, PlayerData playerData)
        {
            players[playerNumber] = playerData;
        }

        [System.Serializable]
        public class PlayerData
        {
            public int currentWaypoint;
            public int numberOfRoundsToWait;
        }
    }
}