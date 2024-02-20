using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;


namespace SER.LastManStanding
{
    [System.Serializable]
    public class ProfileData
    {
        public string username;
        public int level;
        public int xp;

        public ProfileData()
        {
            this.username = "";
            this.level = 1;
            this.xp = 0;
        }

        public ProfileData(string u, int l, int x)
        {
            this.username = u;
            this.level = l;
            this.xp = x;
        }
    }

    [System.Serializable]
    public class MapData
    {
        public string name;
        public int scene;
    }

    public class GameLauncher : MonoBehaviourPunCallbacks
    {
        [HideInInspector] public static GameLauncher instance;
        public List<string> homeTeamPlayers = new List<string>();
        public List<string> awayTeamPlayers = new List<string>();
        public GameObject[] gameScreens;

        public delegate void FunctionDelegate(params object[] args);
        public Dictionary<int, (FunctionDelegate, object[])> functionMap;


        public Slider maximumPlayerInRoomSlider;
        public Text maxPlayersValue;
        public GameCharacters[] characters;


        private int selectedMap = 0;

        public MapData[] maps;

        public Text mapValue;
        public Text timerText;
        public Text roomOptions;
        public Text joinRoomText;
        public bool isRoomOpen = true;
        public Text modeValue;

        public int selectedCharacter = 0;

        public float preGameCountdown = 120f;
        private bool countdownStarted = false;
        private float timer;
        public string username;
        public InputField usernameTextField;

        public GameObject textPrefab; // Assign in the Inspector
        public GameObject homeTeamContainer; // Assign in the Inspector
        public GameObject awayTeamContainer;

        public static ProfileData myProfile = new ProfileData();

        private float syncInterval = 30f;
        private float nextSyncTime;
        public Text messageText;

        private void Awake()
        {
            if (instance == null) instance = this;
            PhotonNetwork.AutomaticallySyncScene = true;
            myProfile = Data.LoadProfile();
            functionMap = new Dictionary<int, (FunctionDelegate, object[])>
        {
            { 3, (SetCreateScreenUIItems, new object[] {}) },
        };
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
            if (!string.IsNullOrEmpty(myProfile.username))
            {
                usernameTextField.text = myProfile.username;
            }
        }

        public void Update()
        {
            if (countdownStarted || PhotonNetwork.IsMasterClient)
            {
                UpdateTimer();
            }
        }

        private System.Collections.IEnumerator TimerCoroutine()
        {
            while (countdownStarted)
            {
                UpdateTimerUI();
                yield return new WaitForSeconds(1f); // Update UI every second
            }
        }

        private void StartTimerCoroutine()
        {
            StartCoroutine(TimerCoroutine());
        }


        private void UpdateTimer()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;

                if (PhotonNetwork.IsMasterClient && Time.time >= nextSyncTime)
                {
                    nextSyncTime = Time.time + syncInterval;
                    SyncTimerToAllClients();
                }
            }
            else
            {
                timer = 0;
                countdownStarted = false;
                timerText.gameObject.SetActive(false);
                StopCoroutine(TimerCoroutine());
                StartGame();
            }
        }

        private void UpdateTimerUI()
        {
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            timerText.text = formattedTime;
        }

        public void UpdateTeamList(string playerName, bool isJoiningHomeTeam)
        {
            if (isJoiningHomeTeam)
            {
                homeTeamPlayers.Add(playerName);
                awayTeamPlayers.Remove(playerName);
            }
            else
            {
                awayTeamPlayers.Add(playerName);
                homeTeamPlayers.Remove(playerName);
            }

            UpdatePhotonRoomProperties();
        }

        private void UpdatePhotonRoomProperties()
        {
            Hashtable roomProperties = new Hashtable();
            roomProperties["HomeTeamPlayers"] = homeTeamPlayers.ToArray();
            roomProperties["AwayTeamPlayers"] = awayTeamPlayers.ToArray();
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }

        private void SyncTimerToAllClients()
        {
            Hashtable props = new Hashtable { { "Timer", timer } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey("Timer"))
            {
                timer = (float)propertiesThatChanged["Timer"];
                countdownStarted = true;
                timerText.gameObject.SetActive(true);
                StartTimerCoroutine();
            }
            if (propertiesThatChanged.ContainsKey("HomeTeamPlayers"))
            {
                homeTeamPlayers = new List<string>((string[])propertiesThatChanged["HomeTeamPlayers"]);
            }
            if (propertiesThatChanged.ContainsKey("AwayTeamPlayers"))
            {
                awayTeamPlayers = new List<string>((string[])propertiesThatChanged["AwayTeamPlayers"]);
            }
        }

        public override void OnConnectedToMaster()
        {
            //Debug.Log("Connected to the game");
            PhotonNetwork.JoinLobby();
            base.OnConnectedToMaster();
        }

        public void CreateRoom()
        {
            string roomName = GenerateRandomRoomName(6);

            RoomOptions roomOptions = new RoomOptions
            {
                IsOpen = isRoomOpen,
                MaxPlayers = (byte)maximumPlayerInRoomSlider.value,
            };

            int currentGameMode = (int)GameSettings.GameMode;
            switch (currentGameMode)
            {
                case 0:
                    roomOptions.CustomRoomProperties = new Hashtable
                    {
                        { "map", selectedMap },
                        { "mode",  (int)GameSettings.GameMode }
                    };
                    break;
                case 1:
                    roomOptions.CustomRoomProperties = new Hashtable
                {
                    { "TeamHomeSize", 0 },
                    { "TeamAwaySize", 0 },
                    { "map", selectedMap },
                    { "mode",  (int)GameSettings.GameMode }
                };
                    break;
                case 2:
                    roomOptions.CustomRoomProperties = new Hashtable
                {
                    { "map", selectedMap },
                    { "mode",  (int)GameSettings.GameMode },
                    { "isPreGameLobby", true }
                };
                    break;
                default:
                    //Debug.LogError("Invalid game mode!");
                    return;
            }
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        private string GenerateRandomRoomName(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("Create room failed: " + message);
            if (message.Contains("already exists"))
            {
                CreateRoom();
            }
        }

        public void JoinRoom()
        {
            PhotonNetwork.JoinRoom(joinRoomText.text);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            VerifyUsername();
            HandleJoinRoomConditions();
            if (PhotonNetwork.IsMasterClient)
            {
                SetMasterClientReady();
                StartCountdown();
            }
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("Timer", out var time))
            {
                timer = (float)time;
                countdownStarted = true;
                timerText.gameObject.SetActive(true);
                StartTimerCoroutine();
                UpdateTimerUI();
            }
            Hashtable defaultCharacterProperties = new Hashtable
            {
                { "CharacterIndex", selectedCharacter }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(defaultCharacterProperties);
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            if (PhotonNetwork.IsMasterClient)
            {
                SetMasterClientReady();
            }
        }

        private void SetMasterClientReady()
        {
            Hashtable masterClientProperties = new Hashtable
            {
                { "IsReady", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(masterClientProperties);
        }

        private void VerifyUsername()
        {
            if (string.IsNullOrEmpty(usernameTextField.text))
            {
                myProfile.username = "RANDOM_USER_" + UnityEngine.Random.Range(100, 1000);
            }
            else
            {
                myProfile.username = usernameTextField.text;
            }
            PhotonNetwork.NickName = myProfile.username;
        }

        public void StartCountdown()
        {
            timer = preGameCountdown;
            countdownStarted = true;
            nextSyncTime = Time.time + syncInterval;
            timerText.gameObject.SetActive(true);
            StartTimerCoroutine();
        }

        public void HandleJoinRoomConditions()
        {
            int gameMode = (int)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
            bool gameStarted = CheckIfGameStarted();
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

            switch (gameMode)
            {
                case 0: // DM
                    if (playerCount <= PhotonNetwork.CurrentRoom.MaxPlayers - 1)
                    {
                        if (gameStarted)
                        {
                            LoadRandomCharacter();
                        }
                        else
                        {
                            HandleDisplayPanelsFromGameMode();
                        }
                    }
                    else PhotonNetwork.LeaveRoom();
                    break;
                case 1: // TDM
                    if (playerCount <= PhotonNetwork.CurrentRoom.MaxPlayers - 1)
                    {
                        if (gameStarted)
                        {
                            AllocateTeamAndLoadPlayer();
                        }
                        else
                        {
                            HandleDisplayPanelsFromGameMode();
                            UpdateTeamUIFromRoomProperties();
                            AllocateTeam();
                        }
                    }
                    else PhotonNetwork.LeaveRoom();
                    break;
                case 2: // BR
                    if (playerCount <= PhotonNetwork.CurrentRoom.MaxPlayers - 1 && IsPreGameLobby())
                    {
                        HandleDisplayPanelsFromGameMode();
                    }
                    else PhotonNetwork.LeaveRoom();
                    break;
                default:
                    //Debug.LogError("Unknown game mode!");
                    PhotonNetwork.LeaveRoom();
                    break;
            }
        }

        private void UpdateTeamUIFromRoomProperties()
        {
            ClearTeamUI();

            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player.CustomProperties.TryGetValue("IsAwayTeam", out object isAwayTeam))
                {
                    string playerName = player.NickName;
                    bool awayTeam = Convert.ToBoolean(isAwayTeam);
                    UpdateTeamUI(awayTeam ? awayTeamContainer : homeTeamContainer, playerName, true);
                }
            }
        }

        private void ClearTeamUI()
        {
            // Clear all children from team containers
            foreach (Transform child in homeTeamContainer.transform) Destroy(child.gameObject);
            foreach (Transform child in awayTeamContainer.transform) Destroy(child.gameObject);
        }

        public void OnChangeTeamButtonClicked()
        {
            bool isCurrentlyAwayTeam = false;

            // Check if the player is currently in the Away team
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsAwayTeam", out object isAwayTeam))
            {
                isCurrentlyAwayTeam = (bool)isAwayTeam;
            }

            // Toggle the team
            bool newTeamAssignment = !isCurrentlyAwayTeam;

            // Update the local player's custom properties to reflect the new team
            Hashtable newProperties = new Hashtable { { "IsAwayTeam", newTeamAssignment } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(newProperties);
            UpdateTeamList(PhotonNetwork.NickName, !newTeamAssignment);

            // Update the UI accordingly
            //UpdateTeamUI(isCurrentlyAwayTeam ? awayTeamContainer : homeTeamContainer, myProfile.username, false);
            //UpdateTeamUI(newTeamAssignment ? awayTeamContainer : homeTeamContainer, myProfile.username, true);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

            if (changedProps.ContainsKey("IsAwayTeam"))
            {
                // Call a method to update the UI for all players
                UpdateTeamUIForAllPlayers();
            }
        }

        private void UpdateTeamUIForAllPlayers()
        {
            // Clear existing team UI
            ClearTeamUI();

            // Iterate through all players in the current room
            foreach (var player in PhotonNetwork.CurrentRoom.Players)
            {
                string playerName = player.Value.NickName; // Ensure NickName is set for each player

                bool isAwayTeam = player.Value.CustomProperties.TryGetValue("IsAwayTeam", out object isAwayTeamObj) && (bool)isAwayTeamObj;

                // Update the UI based on the player's team assignment
                UpdateTeamUI(isAwayTeam ? awayTeamContainer : homeTeamContainer, playerName, true);
            }
        }

        public void UpdateOtherPlayersUI()
        {
            foreach (var player in PhotonNetwork.CurrentRoom.Players)
            {

            }
        }

        public bool IsPreGameLobby()
        {
            return (bool)PhotonNetwork.CurrentRoom.CustomProperties["isPreGameLobby"];
        }

        public void AllocateTeamAndLoadPlayer()
        {
            Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
            int teamHomeSize = roomProperties.ContainsKey("TeamHomeSize") ? (int)roomProperties["TeamHomeSize"] : 0;
            int teamAwaySize = roomProperties.ContainsKey("TeamAwaySize") ? (int)roomProperties["TeamAwaySize"] : 0;

            bool assignToAwayTeam;

            if (teamHomeSize < teamAwaySize)
            {
                assignToAwayTeam = false;
                teamHomeSize++;
            }
            else if (teamAwaySize < teamHomeSize)
            {
                assignToAwayTeam = true;
                teamAwaySize++;
            }
            else
            {
                // Randomly assign to a team if sizes are equal
                assignToAwayTeam = UnityEngine.Random.Range(0, 2) == 0;
                if (assignToAwayTeam)
                    teamAwaySize++;
                else
                    teamHomeSize++;
            }

            // Set the updated team sizes in the room's custom properties
            roomProperties["TeamHomeSize"] = teamHomeSize;
            roomProperties["TeamAwaySize"] = teamAwaySize;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            // Set the local player's team assignment
            Hashtable playerProperties = new Hashtable
            {
                { "IsAwayTeam", assignToAwayTeam }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            LoadRandomCharacter();
            UpdateTeamList(PhotonNetwork.NickName, !assignToAwayTeam);
        }

        public void AllocateTeam()
        {
            Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
            int teamHomeSize = roomProperties.ContainsKey("TeamHomeSize") ? (int)roomProperties["TeamHomeSize"] : 0;
            int teamAwaySize = roomProperties.ContainsKey("TeamAwaySize") ? (int)roomProperties["TeamAwaySize"] : 0;

            bool assignToAwayTeam;

            if (teamHomeSize < teamAwaySize)
            {
                assignToAwayTeam = false;
                teamHomeSize++;
            }
            else if (teamAwaySize < teamHomeSize)
            {
                assignToAwayTeam = true;
                teamAwaySize++;
            }
            else
            {
                // Randomly assign to a team if sizes are equal
                assignToAwayTeam = UnityEngine.Random.Range(0, 2) == 0;
                if (assignToAwayTeam)
                {
                    teamAwaySize++;
                    UpdateTeamUI(awayTeamContainer, myProfile.username, true);
                }
                else
                {
                    teamHomeSize++;
                    UpdateTeamUI(homeTeamContainer, myProfile.username, true);
                }
            }

            // Set the updated team sizes in the room's custom properties
            roomProperties["TeamHomeSize"] = teamHomeSize;
            roomProperties["TeamAwaySize"] = teamAwaySize;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            // Set the local player's team assignment
            Hashtable playerProperties = new Hashtable
            {
                { "IsAwayTeam", assignToAwayTeam }
            };
            UpdateTeamList(PhotonNetwork.NickName, !assignToAwayTeam);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }

        private void UpdateTeamUI(GameObject teamContainer, string playerName, bool isAdding)
        {
            if (isAdding)
            {
                // Add the player's name to the UI
                GameObject newText = Instantiate(textPrefab, teamContainer.transform);
                newText.GetComponent<Text>().text = playerName;
            }
            else
            {
                // Remove the player's name from the UI
                foreach (Transform child in teamContainer.transform)
                {
                    if (child.GetComponent<Text>().text == playerName)
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }

        public void LoadRandomCharacter()
        {
            if (characters == null || characters.Length == 0)
            {
                //Debug.LogError("No characters available to load.");
                return;
            }

            int characterIndex = UnityEngine.Random.Range(0, characters.Length);

            Hashtable playerProperties = new Hashtable
            {
                { "CharacterIndex", characterIndex }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            StartGame();
        }

        public bool CheckIfGameStarted()
        {
            return false;
        }

        private void HandleDisplayPanelsFromGameMode()
        {
            int roomGameMode = (int)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
            int screen;
            switch (roomGameMode)
            {
                case 0:
                    LoadGivenScreen(4);
                    screen = 4;
                    break;
                case 1:
                    LoadGivenScreen(3);
                    screen = 3;
                    break;
                case 2:
                    LoadGivenScreen(5);
                    screen = 5;
                    break;
                default:
                    //Debug.LogError("Invalid game mode!");
                    return;
            }
            if (screen != -1)
            {
                Transform startButton = gameScreens[screen].transform.Find("Start Game");
                Transform readyButton = gameScreens[screen].transform.Find("Ready");
                if (startButton != null && readyButton != null)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        startButton.gameObject.SetActive(true);
                        readyButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        startButton.gameObject.SetActive(false);
                        readyButton.gameObject.SetActive(true);
                    }
                }
                Text roomIDText = gameScreens[screen].transform.Find("ScrollView/RoomName").GetComponent<Text>();
                if (roomIDText != null)
                {
                    roomIDText.text = PhotonNetwork.CurrentRoom.Name;
                    roomIDText.gameObject.SetActive(true);
                }
            }
        }

        public void JoinRandomRoom()
        {
            var expectedCustomRoomProperties = new Hashtable { { "IsOpen", true } };
            PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0);

        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            CreateRoom();
            base.OnJoinRandomFailed(returnCode, message);
        }

        public void ChangeMap()
        {
            selectedMap++;
            if (selectedMap >= maps.Length) selectedMap = 0;
            mapValue.text = "MAP: " + maps[selectedMap].name.ToUpper();
        }

        public void ChangeMode()
        {
            int newMode = (int)GameSettings.GameMode + 1;
            if (newMode >= Enum.GetValues(typeof(GameMode)).Length) newMode = 0;
            GameSettings.GameMode = (GameMode)newMode;
            modeValue.text = "MODE: " + Enum.GetName(typeof(GameMode), newMode);
            if (newMode == 2)
            {
                maximumPlayerInRoomSlider.maxValue = 20;
            }
            else
            {
                maximumPlayerInRoomSlider.maxValue = 10;
            }
            maximumPlayerInRoomSlider.value = maximumPlayerInRoomSlider.maxValue;
            maxPlayersValue.text = Mathf.RoundToInt(maximumPlayerInRoomSlider.value).ToString();
        }

        public void ChangeMaxPlayersSlider(float t_value)
        {
            maxPlayersValue.text = Mathf.RoundToInt(t_value).ToString();
        }

        public void ChangeRoomStatus()
        {
            isRoomOpen = !isRoomOpen;
            roomOptions.text = "ROOM :  " + (isRoomOpen ? "Open" : "Close");
        }

        public void CloseAllScreens()
        {
            foreach (GameObject screen in gameScreens)
            {
                screen.SetActive(false);
            }
        }

        public void LoadGivenScreen(int index)
        {
            CloseAllScreens();
            gameScreens[index].SetActive(true);
            CallFunctionByIndex(index);
        }

        public void CallFunctionByIndex(int index)
        {
            if (functionMap.TryGetValue(index, out var functionInfo))
            {
                functionInfo.Item1(functionInfo.Item2);
            }
            else
            {
                //Debug.Log($"No function mapped for index {index}");
            }
        }

        public void SetCreateScreenUIItems(params object[] args)
        {
            selectedMap = 0;
            mapValue.text = "MAP: " + maps[selectedMap].name.ToUpper();
            roomOptions.text = "ROOM :  " + (isRoomOpen ? "Open" : "Close");

            GameSettings.GameMode = 0;
            modeValue.text = "MODE: " + Enum.GetName(typeof(GameMode), (GameMode)0);

            maximumPlayerInRoomSlider.value = maximumPlayerInRoomSlider.maxValue;
            maxPlayersValue.text = Mathf.RoundToInt(maximumPlayerInRoomSlider.value).ToString();
        }

        public void ChangeCharacter()
        {
            int roomGameMode = (int)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
            int screen;
            Text characterText;
            switch (roomGameMode)
            {
                case 0:
                    LoadGivenScreen(4);
                    screen = 4;
                    break;
                case 1:
                    LoadGivenScreen(3);
                    screen = 3;
                    break;
                case 2:
                    LoadGivenScreen(5);
                    screen = 5;
                    break;
                default:
                    //Debug.LogError("Invalid game mode!");
                    return;
            }
            if (screen != -1)
            {
                characterText = gameScreens[screen].transform.Find("ScrollView/CharacterSelect/Text").GetComponent<Text>();
                selectedCharacter++;
                if (selectedCharacter >= characters.Length) selectedCharacter = 0;
                characterText.text = "Character: " + characters[selectedCharacter].name;
                Hashtable characterProperties = new Hashtable
                {
                    { "CharacterIndex", selectedCharacter }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(characterProperties);
            }
        }

        public void ReadyGame()
        {
            Hashtable playerProperties = new Hashtable
            {
                { "IsReady", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            int roomGameMode = (int)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
            int screen;
            switch (roomGameMode)
            {
                case 0:
                    LoadGivenScreen(4);
                    screen = 4;
                    break;
                case 1:
                    LoadGivenScreen(3);
                    screen = 3;
                    break;
                case 2:
                    LoadGivenScreen(5);
                    screen = 5;
                    break;
                default:
                    //Debug.LogError("Invalid game mode!");
                    return;
            }
            if (screen != -1)
            {
                Transform readyButton = gameScreens[screen].transform.Find("Ready");
                readyButton.gameObject.SetActive(false);
            }
        }

        public void StartGame()
        {
            if (AllPlayersReady() || timer <= 0)
            {
                // Set the IsPreGameLobby property to false
                Hashtable roomProperties = new Hashtable { { "isPreGameLobby", false } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                messageText.text = "";
                // Load the game scene or start the game logic
                // Example: PhotonNetwork.LoadLevel("GameScene");
                Data.SaveProfile(myProfile);
                PhotonNetwork.LoadLevel(maps[(int)PhotonNetwork.CurrentRoom.CustomProperties["map"]].scene);
            }
            else
            {
                messageText.text = "Not all players are ready.";
            }
        }

        private bool AllPlayersReady()
        {
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player.CustomProperties.TryGetValue("IsReady", out object isReady))
                {
                    if (!(bool)isReady)
                        return false;
                }
                else
                {
                    // If the player hasn't set the IsReady property, they are not ready
                    return false;
                }
            }
            return true;
        }
    }
}
