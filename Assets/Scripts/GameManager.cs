using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Linq;

namespace SER.LastManStanding
{
    public class PlayerInformation
    {
        public ProfileData profile;
        public int actor;
        public short kills;
        public short deaths;
        public bool awayTeam;

        public PlayerInformation(ProfileData p, int a, short k, short d, bool t)
        {
            this.profile = p;
            this.actor = a;
            this.kills = k;
            this.deaths = d;
            this.awayTeam = t;
        }
    }

    public enum StateOfGame
    {
        Waiting = 0,
        Starting = 1,
        Playing = 2,
        Ending = 3
    }

    public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        #region Fields

        public int totalAlive;
        public int mainMenuSceneIndex = 0;
        public int maxCountOfKills = 10;
        public int matchLengthDM = 180;
        public int matchLengthTDM = 240;
        public int matchLengthBR = 300;
        public int waitStartGameLength = 30;
        public bool perpetual = false;

        public GameObject mapCameraObject;

        public GameCharacters[] gameCharacters;

        public Transform[] spawnPositions;
        private Dictionary<Transform, float> spawnPositionLastUsed = new Dictionary<Transform, float>();

        public List<PlayerInformation> playerInformation = new List<PlayerInformation>();

        public int myPlayersIndex;
        private bool isPlayerAdded;

        private Text uiElementMykills;
        private Text uiElementMydeaths;
        private Text uiElementTimer;
        private Text uiElementTotalAlive;
        public GameObject killFeedPanel;
        public GameObject killFeedTextPrefab;
        private Transform uiElementLeaderBoard;
        private Transform uiElementEndGame;

        private int currentTimeOfMatch;
        private Coroutine timerCoroutine;

        private StateOfGame state = StateOfGame.Waiting;
        public bool isAwayTeam;

        public AudioSource killConfirmed;

        #endregion

        #region Codes

        public enum EventCodes : byte
        {
            NewPlayer,
            UpdatePlayers,
            ChangeMatchStats,
            UpdateKillFeed,
            NewMatch,
            SPAWN_POSITION_UPDATE_EVENT_CODE,
            KILL_FEED_EVENT_CODE,
            RefreshTimer,
            ReduceAliveEvent
        }

        #endregion

        #region MB Callbacks

        private void Start()
        {
            mapCameraObject.SetActive(false);

            ValidateConnection();
            totalAlive = PhotonNetwork.PlayerList.Length;
            InitializeUI();
            InitializeTimer();
            SendNewPlayerEvent(GameLauncher.myProfile);

            if (PhotonNetwork.IsMasterClient)
            {
                isPlayerAdded = true;
                Spawn();
            }
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsAwayTeam", out object isAwayTeam))
            {
                this.isAwayTeam = (bool)isAwayTeam;
            }
        }

        private void Update()
        {
            if (state == StateOfGame.Ending)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (uiElementLeaderBoard.gameObject.activeSelf) uiElementLeaderBoard.gameObject.SetActive(false);
                else Leaderboard(uiElementLeaderBoard);
            }
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        #endregion

        #region Photon

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code >= 200) return;

            EventCodes e = (EventCodes)photonEvent.Code;
            object[] o = (object[])photonEvent.CustomData;

            switch (e)
            {
                case EventCodes.NewPlayer:
                    RecieveNewPlayerEvent   (o);
                    break;

                case EventCodes.UpdatePlayers:
                    RecieveUpdatePlayersEvent(o);
                    break;

                case EventCodes.ChangeMatchStats:
                    RecieveChangeStatsEvent(o);
                    break;

                case EventCodes.NewMatch:
                    RecieveNewMatchEvent();
                    break;
                case EventCodes.SPAWN_POSITION_UPDATE_EVENT_CODE:
                    UpdateSpawnPosition(o);
                    break;
                case EventCodes.RefreshTimer:
                    RecieveRefreshTimerEvent(o);
                    break;
                case EventCodes.KILL_FEED_EVENT_CODE:
                    UpdateKillFeed((string)o[0], (string)o[1]);
                    break;
                case EventCodes.ReduceAliveEvent:
                    RecieveReduceAliveEvent((int)o[0]);
                    break;
            }
        }


        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            SceneManager.LoadScene(mainMenuSceneIndex);
        }

        #endregion

        #region Methods

        public void Spawn()
        {
            if (spawnPositionLastUsed.Count == 0)
            {
                foreach (Transform spawnPoint in spawnPositions)
                {
                    spawnPositionLastUsed.Add(spawnPoint, Time.time - 100f);
                }
            }

            //print(PhotonNetwork.IsMasterClient);

            Transform availableSpawnPosition = FindAvailableSpawnPosition();

            if (availableSpawnPosition != null)
            {
                GameObject characterToSpawn = GetCharacterPrefab();
                if (characterToSpawn != null)
                {
                    if (PhotonNetwork.IsConnected)
                    {
                        PhotonNetwork.Instantiate(characterToSpawn.name, availableSpawnPosition.position, availableSpawnPosition.rotation);
                        UpdateSpawnPositionUsage(availableSpawnPosition);
                    }
                    else
                    {
                        Instantiate(characterToSpawn, availableSpawnPosition.position, availableSpawnPosition.rotation);
                    }

                    spawnPositionLastUsed[availableSpawnPosition] = Time.time;
                }
                else
                {
                    //Debug.LogError("Character prefab not found for the selected index.");
                }
            }
            else
            {
                //Debug.LogWarning("No spawn position available currently!");
            }
        }

        private Transform FindAvailableSpawnPosition()
        {
            foreach (Transform spawnPoint in spawnPositions)
            {
                if (Time.time - spawnPositionLastUsed[spawnPoint] >= 5f)
                {
                    return spawnPoint;
                }
            }

            return null;
        }

        private void UpdateSpawnPositionUsage(Transform spawnPoint)
        {
            object[] content = new object[] { spawnPoint.position, Time.time };
            PhotonNetwork.RaiseEvent((byte)EventCodes.SPAWN_POSITION_UPDATE_EVENT_CODE, content, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
        }

        public void UpdateSpawnPosition(object[] data)
        {
            if (spawnPositionLastUsed.Count == 0)
            {
                foreach (Transform spawnPoint in spawnPositions)
                {
                    spawnPositionLastUsed.Add(spawnPoint, Time.time - 100f);
                }
            }

            Vector3 position = (Vector3)data[0];
            float lastUsedTime = (float)data[1];

            foreach (Transform spawnPoint in spawnPositions)
            {
                if (spawnPoint.position == position)
                {
                    spawnPositionLastUsed[spawnPoint] = lastUsedTime;
                    break;
                }
            }
        }

        private GameObject GetCharacterPrefab()
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CharacterIndex", out object characterIndex))
            {
                int index = (int)characterIndex;
                if (index >= 0 && index < gameCharacters.Length)
                {
                    return gameCharacters[index].characterPrefab;
                }
            }

            return null;
        }

        private void InitializeUI()
        {
            uiElementMykills = GameObject.Find("HUD/Stats/Panel/Kills/Text").GetComponent<Text>();
            uiElementMydeaths = GameObject.Find("HUD/Stats/Panel/Deaths/Text").GetComponent<Text>();
            uiElementTimer = GameObject.Find("HUD/Timer/Text").GetComponent<Text>();
            uiElementLeaderBoard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
            uiElementEndGame = GameObject.Find("Canvas").transform.Find("End Game").transform;
            int gameMode = (int)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
            GameSettings.GameMode = (GameMode)gameMode;
            if (gameMode == 2)
            {
                GameObject.Find("HUD/Stats/Panel/TotalAlive").SetActive(true);
                uiElementTotalAlive = GameObject.Find("HUD/Stats/Panel/TotalAlive/Text").GetComponent<Text>();
                uiElementTotalAlive.gameObject.SetActive(true);
            }
            RefreshMyStats();
        }



        private void RefreshMyStats()
        {
            if (playerInformation.Count > myPlayersIndex)
            {
                uiElementMykills.text = $"{playerInformation[myPlayersIndex].kills} kills";
                uiElementMydeaths.text = $"{playerInformation[myPlayersIndex].deaths} deaths";
            }
            else
            {
                uiElementMykills.text = "0 kills";
                uiElementMydeaths.text = "0 deaths";
            }
            int gameMode = (int)PhotonNetwork.CurrentRoom.CustomProperties["mode"];
            if (gameMode == 2)
            {
                uiElementTotalAlive.text = $"{totalAlive} Alive";
            }
        }

        private void Leaderboard(Transform playerLeaderboard)
        {
            if (GameSettings.GameMode == GameMode.DM) playerLeaderboard = playerLeaderboard.Find("DM");
            if (GameSettings.GameMode == GameMode.TDM) playerLeaderboard = playerLeaderboard.Find("TDM");
            if (GameSettings.GameMode == GameMode.BR) playerLeaderboard = playerLeaderboard.Find("BR");

            for (int i = 2; i < playerLeaderboard.childCount; i++)
            {
                Destroy(playerLeaderboard.GetChild(i).gameObject);
            }

            playerLeaderboard.Find("Header/Mode").GetComponent<Text>().text = System.Enum.GetName(typeof(GameMode), GameSettings.GameMode);
            playerLeaderboard.Find("Header/Map").GetComponent<Text>().text = SceneManager.GetActiveScene().name;

            if (GameSettings.GameMode == GameMode.TDM)
            {
                playerLeaderboard.Find("Header/Score/Home").GetComponent<Text>().text = "0";
                playerLeaderboard.Find("Header/Score/Away").GetComponent<Text>().text = "0";
            }

            GameObject playercard = playerLeaderboard.GetChild(1).gameObject;
            playercard.SetActive(false);

            List<PlayerInformation> sorted = SortPlayers(playerInformation);

            bool displayAlternateColorsForTeam = false;
            foreach (PlayerInformation a in sorted)
            {
                GameObject newcard = Instantiate(playercard, playerLeaderboard) as GameObject;

                if (GameSettings.GameMode == GameMode.TDM)
                {
                    newcard.transform.Find("Home").gameObject.SetActive(!a.awayTeam);
                    newcard.transform.Find("Away").gameObject.SetActive(a.awayTeam);
                }

                if (displayAlternateColorsForTeam) newcard.GetComponent<Image>().color = new Color32(0, 0, 0, 180);
                displayAlternateColorsForTeam = !displayAlternateColorsForTeam;

                newcard.transform.Find("Level").GetComponent<Text>().text = a.profile.level.ToString("00");
                newcard.transform.Find("Username").GetComponent<Text>().text = a.profile.username;
                newcard.transform.Find("Score Value").GetComponent<Text>().text = (a.kills * 100).ToString();
                newcard.transform.Find("Kills Value").GetComponent<Text>().text = a.kills.ToString();
                newcard.transform.Find("Deaths Value").GetComponent<Text>().text = a.deaths.ToString();

                newcard.SetActive(true);
            }

            playerLeaderboard.gameObject.SetActive(true);
            playerLeaderboard.parent.gameObject.SetActive(true);
        }

        private List<PlayerInformation> SortPlayers(List<PlayerInformation> playerInfo)
        {
            if (GameSettings.GameMode == GameMode.DM)
            {
                // Sort players in Descending order based on kills
                return playerInfo.OrderByDescending(p => p.kills).ToList();
            }
            else if (GameSettings.GameMode == GameMode.TDM)
            {
                // First, sort players based on team and then kills within each team
                return playerInfo.OrderByDescending(p => p.awayTeam)
                                 .ThenByDescending(p => p.kills)
                                 .ToList();
            }
            else
            {
                return playerInfo;
            }
        }

        private void ValidateConnection()
        {
            if (PhotonNetwork.IsConnected) return;
            SceneManager.LoadScene(mainMenuSceneIndex);
        }

        private void StateCheck()
        {
            if (state == StateOfGame.Ending)
            {
                EndGame();
            }
        }

        private void ScoreCheck()
        {
            bool didSomeoneWin = false;

            foreach (PlayerInformation aPlayer in playerInformation)
            {
                if (aPlayer.kills >= maxCountOfKills && GameSettings.GameMode == GameMode.DM)
                {
                    didSomeoneWin = true;
                    break;
                }
            }

            if(GameSettings.GameMode == GameMode.BR && totalAlive == 1)
            {
                didSomeoneWin = true;
            }

            if (didSomeoneWin)
            {
                if (PhotonNetwork.IsMasterClient && state != StateOfGame.Ending)
                {
                    SendUpdatePlayersEvent((int)StateOfGame.Ending, playerInformation);
                }
            }
        }

        private void InitializeTimer()
        {
            currentTimeOfMatch = (GameSettings.GameMode == GameMode.DM ? matchLengthDM : GameSettings.GameMode == GameMode.TDM ? matchLengthTDM : matchLengthBR);
            RefreshTimerUI();

            if (PhotonNetwork.IsMasterClient)
            {
                timerCoroutine = StartCoroutine(Timer());
            }
        }

        private void RefreshTimerUI()
        {
            string minutes = (currentTimeOfMatch / 60).ToString("00");
            string seconds = (currentTimeOfMatch % 60).ToString("00");
            uiElementTimer.text = $"{minutes}:{seconds}";
        }

        private void EndGame()
        {
            state = StateOfGame.Ending;

            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            currentTimeOfMatch = 0;
            RefreshTimerUI();

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();

                if (!perpetual)
                {
                    PhotonNetwork.CurrentRoom.IsVisible = false;
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                }
            }
            mapCameraObject.SetActive(true);

            uiElementEndGame.gameObject.SetActive(true);
            Leaderboard(uiElementEndGame.Find("Design/Leaderboard"));
            StartCoroutine(End(6f));
        }

        public bool CalculateTeam()
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsAwayTeam", out object isAwayTeam))
            {
                return (bool)isAwayTeam;
            }
            return false;
        }

        #endregion

        #region Events

        public void SendNewPlayerEvent(ProfileData playerProfile)
        {
            object[] package = new object[7];

            package[0] = playerProfile.username;
            package[1] = playerProfile.level;
            package[2] = playerProfile.xp;
            package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
            package[4] = (short)0;
            package[5] = (short)0;
            package[6] = CalculateTeam();

            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.NewPlayer,
                package,
                new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
                new SendOptions { Reliability = true }
            );
        }

        public void RecieveNewPlayerEvent(object[] data)
        {
            PlayerInformation playerProfile = new PlayerInformation(
                new ProfileData(
                    (string)data[0],
                    (int)data[1],
                    (int)data[2]
                ),
                (int)data[3],
                (short)data[4],
                (short)data[5],
                (bool)data[6]
            );

            playerInformation.Add(playerProfile);

            foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
            {
                //gameObject.GetComponent<Player>().TrySync();
            }

            SendUpdatePlayersEvent((int)state, playerInformation);
        }

        public void SendUpdatePlayersEvent(int state, List<PlayerInformation> playerInfoRecieved)
        {
            object[] package = new object[playerInfoRecieved.Count + 1];

            package[0] = state;
            for (int i = 0; i < playerInfoRecieved.Count; i++)
            {
                object[] piece = new object[7];

                piece[0] = playerInfoRecieved[i].profile.username;
                piece[1] = playerInfoRecieved[i].profile.level;
                piece[2] = playerInfoRecieved[i].profile.xp;
                piece[3] = playerInfoRecieved[i].actor;
                piece[4] = playerInfoRecieved[i].kills;
                piece[5] = playerInfoRecieved[i].deaths;
                piece[6] = playerInfoRecieved[i].awayTeam;

                package[i + 1] = piece;
            }

            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.UpdatePlayers,
                package,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
            );
        }

        public void RecieveUpdatePlayersEvent(object[] data)
        {
            state = (StateOfGame)data[0];

            if (playerInformation.Count < data.Length - 1)
            {
                foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
                {
                    //if so, resync our local player information
                    gameObject.GetComponent<PlayerMovementManager>().TrySync();
                }
            }

            playerInformation = new List<PlayerInformation>();

            for (int i = 1; i < data.Length; i++)
            {
                object[] extract = (object[])data[i];

                PlayerInformation p = new PlayerInformation(
                    new ProfileData(
                        (string)extract[0],
                        (int)extract[1],
                        (int)extract[2]
                    ),
                    (int)extract[3],
                    (short)extract[4],
                    (short)extract[5],
                    (bool)extract[6]
                );

                playerInformation.Add(p);

                if (PhotonNetwork.LocalPlayer.ActorNumber == p.actor)
                {
                    myPlayersIndex = i - 1;

                    //if we have been waiting to be added to the game then spawn us in
                    if (!isPlayerAdded)
                    {
                        isPlayerAdded = true;
                        GameSettings.IsAwayTeam = p.awayTeam;
                        Spawn();
                    }
                }
            }

            StateCheck();
        }

        public void SendChangeStatsEvent(int actor, byte stat, byte amt)
        {
            object[] package = new object[] { actor, stat, amt };

            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.ChangeMatchStats,
                package,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
            );
        }

        public void RecieveChangeStatsEvent(object[] data)
        {
            int actor = (int)data[0];
            byte stat = (byte)data[1];
            byte amt = (byte)data[2];

            string killerName = "";
            string victimName = "";

            for (int i = 0; i < playerInformation.Count; i++)
            {
                if (playerInformation[i].actor == actor)
                {
                    switch (stat)
                    {
                        case 0: // Kills
                            playerInformation[i].kills += amt;
                            //killerName = playerInformation[i].profile.username;
                            break;

                        case 1: // Deaths
                            playerInformation[i].deaths += amt;
                            //victimName = playerInformation[i].profile.username;
                            break;
                    }

                    if (i == myPlayersIndex) RefreshMyStats();
                    if (uiElementLeaderBoard.gameObject.activeSelf) Leaderboard(uiElementLeaderBoard);

                    break;
                }
            }
            //print(killerName);
            //print(victimName);
            //if (stat == 0 && !string.IsNullOrEmpty(killerName) && !string.IsNullOrEmpty(victimName))
            //{
            //    UpdateKillFeed(killerName, victimName);
            //    BroadcastKillFeedEvent(killerName, victimName);
            //}
            ScoreCheck();
        }

        private void UpdateKillFeed(string killerName, string victimName)
        {
            GameObject killFeedItem = Instantiate(killFeedTextPrefab, killFeedPanel.transform);
            killFeedItem.GetComponent<Text>().text = $"{killerName} killed {victimName}";
            if (playerInformation[myPlayersIndex].profile.username == killerName)
            {
                killConfirmed.Play();
            }
            Destroy(killFeedItem, 5f);
        }

        public void BroadcastKillFeedEvent(int killerActor, string victimActor)
        {
            string killerName = "";
            string victimName = victimActor;

            for (int i = 0; i < playerInformation.Count; i++)
            {
                if (playerInformation[i].actor == killerActor)
                {
                    killerName = playerInformation[i].profile.username;
                }
            }
            byte eventCode = (byte)EventCodes.KILL_FEED_EVENT_CODE;
            object[] content = new object[] { killerName, victimName };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public void SendReduceAliveEvent()
        {
            byte eventCode = (byte)EventCodes.ReduceAliveEvent;
            object[] content = new object[] { totalAlive - 1 };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public void RecieveReduceAliveEvent(int newTotalAlive)
        {
            totalAlive = newTotalAlive;
            RefreshMyStats();
            ScoreCheck();
        }

        public void SendNewMatchEvent()
        {
            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.NewMatch,
                null,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
            );
        }
        public void RecieveNewMatchEvent()
        {
            state = StateOfGame.Waiting;

            mapCameraObject.SetActive(false);

            uiElementEndGame.gameObject.SetActive(false);

            foreach (PlayerInformation p in playerInformation)
            {
                p.kills = 0;
                p.deaths = 0;
            }

            RefreshMyStats();

            InitializeTimer();

            Spawn();
        }

        public void SendRefreshTimerEvent()
        {
            object[] package = new object[] { currentTimeOfMatch };

            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.RefreshTimer,
                package,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
            );
        }
        public void RecieveRefreshTimerEvent(object[] data)
        {
            currentTimeOfMatch = (int)data[0];
            RefreshTimerUI();
        }

        #endregion

        #region Coroutines

        private IEnumerator Timer()
        {
            yield return new WaitForSeconds(1f);

            currentTimeOfMatch -= 1;

            if (currentTimeOfMatch <= 0)
            {
                timerCoroutine = null;
                SendUpdatePlayersEvent((int)StateOfGame.Ending, playerInformation);
            }
            else
            {
                SendRefreshTimerEvent();
                timerCoroutine = StartCoroutine(Timer());
            }
        }

        private IEnumerator End(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);

            if (perpetual)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    SendNewMatchEvent();
                }
            }
            else
            {
                PhotonNetwork.AutomaticallySyncScene = false;
                PhotonNetwork.LeaveRoom();
            }
        }
        #endregion
    }
}