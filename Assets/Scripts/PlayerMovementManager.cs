using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using TMPro;
using UnityEngine.UI;

namespace SER.LastManStanding
{
    public class PlayerMovementManager : MonoBehaviourPun
    {
        // Movement variables
        [HideInInspector] public ProfileData playerProfile;
        [HideInInspector] public bool awayTeam;
        [HideInInspector] public static PlayerMovementManager instance;
        public float walkSpeed;
        public float sprintSpeed;
        public float maxVelocityChange;
        public float jumpHeight;
        public float airControl;
        public float gravityIntensity;
        private bool sprinting;
        private bool jumping;
        private bool grounded;
        private Vector2 input;
        private Rigidbody playerRigidbody;
        public Text abilityOneText;
        public Text abilityTwoText;
        public Text abilityThreeText;
        public GameObject camera;

        // Dash varibales
        private int maxDashes;
        private int dashesDone;
        [SerializeField] private ParticleSystem forwardDashAnimation;
        [SerializeField] private ParticleSystem backwardDashAnimation;
        [SerializeField] private ParticleSystem leftDashAnimation;
        [SerializeField] private ParticleSystem rightDashAnimation;
        public float dashDistance;
        public float dashTime;
        public float dashCooldown;
        private float lastDashTime;
        private bool isDashing;


        // Updraft variables
        public bool updraftMode;
        private float lastUpdraft;
        private float updraftHeight;
        private float updraftDelay;
        private int updrafts;
        private int maxUpdrafts;


        // Smoke Variables

        public bool smokeMode = false;

        [SerializeField] GameObject smokeSphere;

        private Transform smokeTransform;

        [SerializeField] Camera mainCamera;

        SmokeProjectile smokeProjectile;

        private float lastSmokeEnded = 0f;
        private float smokeDelay = 0.3f;

        private Vector3 offset;
        private Vector3 newPosition;

        public RectTransform smokeProgressBar;
        public TextMeshProUGUI smokeText;
        private int smokes;
        private int maxSmokes = 5;

        private GameObject uiElementHealthbarObject;
        private Transform uiElementHealthbar;
        private Text uiElementAmmo;
        private Text uiElementMagzine;
        private Text uiElementUsername;
        public Text uiElementTeam;
        private Transform uiElementAbilityOneBar;
        private Text uiElementAbilityOneText;
        private Transform uiElementAbilityTwoBar;
        private Text uiElementAbilityTwoText;
        private Transform uiElementAbilityThreeBar;
        private Text uiElementAbilityThreeText;
        private GameObject pausePanel;

        public GameManager gameManager;


        public bool isBuilding;
        [SerializeField] Transform CamChild;
        [SerializeField] GameObject TransparentFloor;
        [SerializeField] GameObject TransparentWall;
        [SerializeField] GameObject TransparentStair;
        public GameObject floorBuild;
        public GameObject wallBuild;
        public GameObject stairBuild;
        RaycastHit Hit;
        [SerializeField] GameObject Floor;
        [SerializeField] GameObject Wall;
        [SerializeField] GameObject Stair;
        public GameObject currentPrefab;
        public GameObject currentBuild;

        public int maxHealth = 100;
        public int currentHealth;

        public Transform ThirdPersonGunHolder;
        public List<string> homeTeamPlayers = new List<string>();
        public List<string> awayTeamPlayers = new List<string>();

        public bool IsAwayTeam;

        public float bobFrequency = 5f;
        public float bobHorizontalAmplitude = 0.2f;
        public float bobVerticalAmplitude = 0.1f;
        private float defaultYPos = 0;
        private float timer = 0;

        //public Renderer[] teamIndicators;

        // Grenade Variables

        public Transform cameraTransform;
        public Transform attackLocation;
        public GameObject throwingObject;
        public GameObject fragGrenade;
        public GameObject smokeGrenade;
        public GameObject flashBang;

        public int smokeGThrows;
        public int fragThrows;
        public int flashbangThrows;
        public int maxSmokeGThrows;
        public int maxFragThrows;
        public int maxFlashbangThrows;


        public float throwCooldown;
        public KeyCode fragKey = KeyCode.F;
        public KeyCode smokeKey = KeyCode.G;
        public KeyCode flashKey = KeyCode.H;
        public float throwForce;
        public float throwUpwardForce;

        public bool attackMode;

        public TextMeshPro usernameText;

        public AudioSource damageAudio;
        public AudioSource killAudio;

        public AudioSource grenadeAudio;

        public Image flashbangPanel;

        public Material redTeamMaterial;
        public Material blueTeamMaterial;

        void Start()
        {
            if (photonView != null && photonView.IsMine)
            {
                instance = this;
                currentHealth = maxHealth;
                gameManager = GameObject.Find("Manager").GetComponent<GameManager>();
                camera.SetActive(true);
                uiElementHealthbar = GameObject.Find("HUD/Health/Bar").transform;
                uiElementAmmo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
                uiElementMagzine = GameObject.Find("HUD/Magzine/Text").GetComponent<Text>();
                uiElementUsername = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
                uiElementTeam = GameObject.Find("HUD/Team/Text").GetComponent<Text>();
                uiElementUsername.text = GameLauncher.myProfile.username;
                uiElementAbilityOneBar = GameObject.Find("HUD/AbilityOne/Bar").transform;
                uiElementAbilityOneText = GameObject.Find("HUD/AbilityOne/Name").GetComponent<Text>();
                uiElementAbilityTwoBar = GameObject.Find("HUD/AbilityTwo/Bar").transform;
                uiElementAbilityTwoText = GameObject.Find("HUD/AbilityTwo/Name").GetComponent<Text>();
                uiElementAbilityThreeBar = GameObject.Find("HUD/AbilityThree/Bar").transform;
                uiElementAbilityThreeText = GameObject.Find("HUD/AbilityThree/Name").GetComponent<Text>();
                flashbangPanel = GameObject.Find("Flash").GetComponent<Image>();
                flashbangPanel.gameObject.SetActive(false);
                homeTeamPlayers = GameLauncher.instance.homeTeamPlayers;
                awayTeamPlayers = GameLauncher.instance.awayTeamPlayers;

                photonView.RPC("SyncProfile", RpcTarget.All, GameLauncher.myProfile.username, GameLauncher.myProfile.level, GameLauncher.myProfile.xp);
                if (GameSettings.GameMode == GameMode.TDM)
                {
                    if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsAwayTeam", out object isAwayTeam))
                    {
                        photonView.RPC("SyncTeam", RpcTarget.All, gameManager.isAwayTeam);
                        IsAwayTeam = gameManager.isAwayTeam;

                        if (IsAwayTeam)
                        {
                            uiElementTeam.text = "Red team";
                            uiElementTeam.color = Color.red;
                            usernameText.color = Color.red;
                            Renderer playerRenderer = GetComponentInChildren<Renderer>();
                            playerRenderer.material = redTeamMaterial;
                        }
                        else
                        {
                            uiElementTeam.text = "Blue team";
                            uiElementTeam.color = Color.blue;
                            usernameText.color = Color.blue;
                            Renderer playerRenderer = GetComponentInChildren<Renderer>();
                            playerRenderer.material = blueTeamMaterial;
                        }
                    }
                }
                else
                {
                    uiElementTeam.gameObject.SetActive(false);
                }

                playerRigidbody = GetComponent<Rigidbody>();

                isDashing = false;
                dashDistance = 2f;
                dashTime = 0.2f;
                dashCooldown = 5f;
                lastDashTime = 0f;
                maxDashes = 3;
                dashesDone = 0;

                updraftMode = false;
                lastUpdraft = 0f;
                updraftHeight = 20.0f;
                updraftDelay = 0.2f;
                updrafts = 0;
                maxUpdrafts = 3;

                smokeMode = false;
                lastSmokeEnded = 0f;
                smokeDelay = 0.3f;
                smokes = 0;
                maxSmokes = 3;


                isBuilding = false;
                floorBuild = InstantiatePrefabRandomly(TransparentFloor);
                wallBuild = InstantiatePrefabRandomly(TransparentWall);
                stairBuild = InstantiatePrefabRandomly(TransparentStair);
                floorBuild.SetActive(false);
                wallBuild.SetActive(false);
                stairBuild.SetActive(false);
                photonView.RPC("SetActiveStateRPC", RpcTarget.All);
                photonView.RPC("SetActiveStateRPC", RpcTarget.All);
                currentPrefab = Floor;
                currentBuild = floorBuild;


                pausePanel = GameObject.Find("Canvas/Pause");
                pausePanel.SetActive(false);
                ThirdPersonGunHolder.gameObject.SetActive(false);

                defaultYPos = camera.transform.localPosition.y;


                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CharacterIndex", out object characterIndex))
                {
                    int index = (int)characterIndex;
                    switch (index)
                    {
                        case 0:
                            UpdateDashUI();
                            UpdateUpdraftUI();
                            UpdateSmokeUI();
                            break;
                        case 2:
                            UpdateFragGrenadeUI();
                            UpdateFlashBangUI();
                            UpdateSmokeGrenadeUI();
                            break;
                    }
                }

            }
        }

        private void ColorTeamIndicators(Color p_color)
        {
            //foreach (Renderer renderer in teamIndicators) renderer.material.color = p_color;
        }

        // Update is called once per frame
        void Update()
        {
            if (photonView != null)
            {
                if (!photonView.IsMine && PhotonNetwork.IsConnected)
                {
                    //RefreshMultiplayerState();
                    return;
                }

                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                input.Normalize();

                sprinting = Input.GetButton("Sprint");
                jumping = Input.GetButton("Jump");

                HandleAbilities();
                bool pause = Input.GetKeyDown(KeyCode.Escape);
                if (pause)
                {
                    pausePanel.SetActive(true);
                    pausePanel.GetComponent<Pause>()
                              .TogglePause();
                }

                if (Pause.paused)
                {
                    input.x = 0f;
                    input.y = 0f;
                    isDashing = false;
                    sprinting = false;
                    jumping = false;
                    pause = false;
                    grounded = false;
                    isBuilding = false;
                }
                if (camera != null && input.magnitude > 0.1f)
                {
                    timer += Time.deltaTime * bobFrequency * (sprinting ? 1.5f : 1f);
                    HeadBobbing();
                }
                else
                {
                    // Reset camera position when not moving
                    ResetCameraPosition();
                }
                uiElementUsername.transform.LookAt(Camera.main.transform);
                uiElementUsername.text = PhotonNetwork.LocalPlayer.NickName;
            }
        }

        private void HeadBobbing()
        {
            float waveSlice = Mathf.Sin(timer);
            float bobbingAmountY = waveSlice * bobVerticalAmplitude;
            float bobbingAmountX = Mathf.Cos(timer * 2) * bobHorizontalAmplitude;

            Vector3 localPos = camera.transform.localPosition;
            localPos.y = defaultYPos + bobbingAmountY;
            localPos.x = bobbingAmountX;

            camera.transform.localPosition = localPos;
        }

        private void ResetCameraPosition()
        {
            Vector3 localPos = camera.transform.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, defaultYPos, Time.deltaTime * bobFrequency);
            localPos.x = Mathf.Lerp(localPos.x, 0f, Time.deltaTime * bobFrequency);
            camera.transform.localPosition = localPos;
        }

        public void HandleAbilities()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CharacterIndex", out object characterIndex))
            {
                int index = (int)characterIndex;
                switch (index) {
                    case 0:
                        HandleJettAbilities();
                        break;
                    case 1:
                        HandleBuildAbilities();
                        break;
                    case 2:
                        HandleTerroristAbilities();
                        break;
                }
            }
        }

        public void HandleJettAbilities()
        {
            if (Input.GetKeyUp(KeyCode.E) && Time.time > lastDashTime + dashCooldown && dashesDone < maxDashes)
            {
                //photonView.RPC("Dash", RpcTarget.All);
                Dash();
                lastDashTime = Time.time;
            }
            if (!isDashing)
            {
                HandleUpdraft();
                HandleSmoke();
            }
        }

        public void HandleBuildAbilities()
        {
            uiElementAbilityOneBar.gameObject.SetActive(false);
            uiElementAbilityTwoBar.gameObject.SetActive(false);
            uiElementAbilityThreeBar.gameObject.SetActive(false);

            uiElementAbilityOneText.text = "Wall";
            uiElementAbilityTwoText.text = "Floor";
            uiElementAbilityThreeText.text = "Stair";

            if (Input.GetKeyDown(KeyCode.B))
            {
                isBuilding = !isBuilding;
                if (isBuilding)
                {
                    currentBuild.SetActive(true);
                }
                if (photonView != null)
                {
                    photonView.RPC("ToggleBuildingRPC", RpcTarget.All, isBuilding);
                }
            }

            if (isBuilding)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    currentPrefab = Floor;
                    currentBuild = floorBuild;
                    floorBuild.SetActive(true);
                    wallBuild.SetActive(false);
                    stairBuild.SetActive(false);
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    currentPrefab = Wall;
                    currentBuild = wallBuild;
                    floorBuild.SetActive(false);
                    wallBuild.SetActive(true);
                    stairBuild.SetActive(false);
                }
                if (Input.GetKeyDown(KeyCode.C))
                {
                    currentPrefab = Stair;
                    currentBuild = stairBuild;
                    floorBuild.SetActive(false);
                    wallBuild.SetActive(false);
                    stairBuild.SetActive(true);
                }

                if (Physics.Raycast(CamChild.position, CamChild.forward, out Hit, 50f) && Hit.distance >= 0.5f)
                {
                    currentBuild.transform.position = new Vector3(Mathf.RoundToInt(Hit.point.x) != 0 ? Mathf.RoundToInt(Hit.point.x / 5) * 5 : 0,
                        Mathf.RoundToInt(Hit.point.y) + currentBuild.transform.localScale.y,
                        Mathf.RoundToInt(Hit.point.z) != 0 ? Mathf.RoundToInt(Hit.point.z / 5) * 5 : 0);
                    currentBuild.transform.eulerAngles = new Vector3(0, Mathf.RoundToInt(transform.eulerAngles.y) != 0 ? Mathf.RoundToInt(transform.eulerAngles.y / 90f) * 90f : 0, 0);

                    if (Input.GetMouseButtonDown(1))
                    {
                        if (photonView != null)
                        {
                            photonView.RPC("BuildStructureRPC", RpcTarget.All, currentPrefab.name, currentBuild.transform.position, currentBuild.transform.rotation);
                        }
                    }
                }
            }
        }

        [PunRPC]
        void ToggleBuildingRPC(bool buildingState)
        {
            if (photonView != null && PhotonNetwork.IsConnected && photonView.IsMine)
            {
                isBuilding = buildingState;
                currentBuild.SetActive(isBuilding);
            }

        }

        [PunRPC]
        void BuildStructureRPC(string prefabName, Vector3 position, Quaternion rotation)
        {
            if (photonView != null && PhotonNetwork.IsConnected && photonView.IsMine)
            {
                GameObject prefabToBuild = GetPrefabByName(prefabName);
                GameObject newStructure = PhotonNetwork.Instantiate(prefabToBuild.name, position, rotation);
                newStructure.SetActive(true);
            }
        }

        GameObject GetPrefabByName(string name)
        {
            switch (name)
            {
                case "Floor":
                    return Floor;
                case "Wall":
                    return Wall;
                case "Stair":
                    return Stair;
                default:
                    return null;
            }
        }

        public void HandleTerroristAbilities()
        {
            attackMode = true;
            if (Input.GetKeyDown(KeyCode.E) && attackMode && fragThrows < maxFragThrows)
            {
                grenadeAudio.Play();
                SetThrowingObject(1);
                LaunchGrenade();
                UpdateFragGrenadeUI();
                //photonView.RPC("SetThrowingObject", RpcTarget.All, 1);
                //photonView.RPC("LaunchGrenade", RpcTarget.All);

            }
            else if (Input.GetKeyDown(KeyCode.Q) && attackMode && smokeGThrows < maxSmokeGThrows)
            {
                grenadeAudio.Play();
                SetThrowingObject(2);
                LaunchGrenade();
                UpdateSmokeGrenadeUI();
                //photonView.RPC("SetThrowingObject", RpcTarget.All, 2);
                //photonView.RPC("LaunchGrenade", RpcTarget.All);

            }
            else if (Input.GetKeyDown(KeyCode.C) && attackMode && flashbangThrows < maxFlashbangThrows)
            {
                grenadeAudio.Play();
                SetThrowingObject(3);
                LaunchGrenade();
                UpdateFlashBangUI();
                //photonView.RPC("SetThrowingObject", RpcTarget.All, 3);
                //photonView.RPC("LaunchGrenade", RpcTarget.All);

            }
        }

        public void UpdateFragGrenadeUI()
        {
            if (photonView.IsMine)
            {
                uiElementAbilityOneText.text = $"Frags - {(maxFragThrows - fragThrows).ToString() + " / " + maxFragThrows.ToString()}";
                // dashProgressBar.fillAmount = (float)(maxDashes - dashes) / maxDashes;

                uiElementAbilityOneBar.localScale = new Vector3((float)(maxFragThrows - fragThrows) / maxFragThrows, 1, 1);
            }
        }

        public void UpdateSmokeGrenadeUI()
        {
            if (photonView.IsMine)
            {
                uiElementAbilityTwoText.text = $"Smokes - {(maxSmokeGThrows - smokeGThrows).ToString() + " / " + maxSmokeGThrows.ToString()}";
                // dashProgressBar.fillAmount = (float)(maxDashes - dashes) / maxDashes;

                uiElementAbilityTwoBar.localScale = new Vector3((float)(maxSmokeGThrows - smokeGThrows) / maxSmokeGThrows, 1, 1);
            }
        }

        public void UpdateFlashBangUI()
        {
            if (photonView.IsMine)
            {
                uiElementAbilityThreeText.text = $"Flash - {(maxFlashbangThrows - flashbangThrows).ToString() + " / " + maxFlashbangThrows.ToString()}";
                // dashProgressBar.fillAmount = (float)(maxDashes - dashes) / maxDashes;

                uiElementAbilityThreeBar.localScale = new Vector3((float)(maxFlashbangThrows - flashbangThrows) / maxSmokeGThrows, 1, 1);
            }
        }

        [PunRPC]
        private void SetThrowingObject(int key)
        {
            if (key == 1)
            {
                throwingObject = fragGrenade;
            }
            else if (key == 2)
            {
                throwingObject = smokeGrenade;
            }
            else if (key == 3)
            {
                throwingObject = flashBang;
            }
        }

        [PunRPC]
        private void LaunchGrenade()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            attackMode = false;

            Vector3 spawnPosition = transform.position + transform.forward * 3f;


            GameObject grenadeProjectile = PhotonNetwork.Instantiate(throwingObject.name, spawnPosition, cameraTransform.rotation);

            Rigidbody grenadeRigidbody = grenadeProjectile.GetComponent<Rigidbody>();

            Vector3 forceDirection = cameraTransform.transform.forward;

            RaycastHit hit;

            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 50f))
            {
                forceDirection = (hit.point - spawnPosition).normalized;
            }


            Vector3 force = forceDirection * throwForce + transform.up * throwUpwardForce;

            grenadeRigidbody.AddForce(force, ForceMode.Impulse);

            if (throwingObject == fragGrenade)
            {
                fragThrows++;
            }
            if (throwingObject == smokeGrenade)
            {
                smokeGThrows++;
            }
            if (throwingObject == flashBang)
            {
                flashbangThrows++;
            }

            Invoke(nameof(ResetThrow), throwCooldown);


        }

        private void ResetThrow()
        {
            attackMode = true;
        }

        public void TrySync()
        {
            if (!photonView.IsMine) return;

            photonView.RPC("SyncProfile", RpcTarget.All, GameLauncher.myProfile.username, GameLauncher.myProfile.level, GameLauncher.myProfile.xp);

            if (GameSettings.GameMode == GameMode.TDM)
            {
                photonView.RPC("SyncTeam", RpcTarget.All, gameManager.CalculateTeam());
            }
        }

        [PunRPC]
        private void SyncProfile(string p_username, int p_level, int p_xp)
        {
            if (!photonView.IsMine)
            {
                return;
            }
            playerProfile = new ProfileData(p_username, p_level, p_xp);
            uiElementUsername.text = PhotonNetwork.LocalPlayer.NickName;
            usernameText.text = PhotonNetwork.LocalPlayer.NickName;
            //print(PhotonNetwork.LocalPlayer.NickName);
        }

        [PunRPC]
        private void SyncTeam(bool p_awayTeam)
        {
            awayTeam = p_awayTeam;

            if (awayTeam)
            {
                ColorTeamIndicators(Color.red);
                Renderer playerRenderer = GetComponentInChildren<Renderer>();
                playerRenderer.material = redTeamMaterial;
            }
            else
            {
                ColorTeamIndicators(Color.blue);
                Renderer playerRenderer = GetComponentInChildren<Renderer>();
                playerRenderer.material = blueTeamMaterial;
            }
        }


        private void OnTriggerStay(Collider other)
        {
            grounded = true;
        }

        void FixedUpdate()
        {
            if(!photonView.IsMine)
            {
                return;
            }
            if (playerRigidbody == null)
            {
                //Debug.LogError("Player Rigidbody is not assigned!");
            }
            if (!isDashing)
            {
                if (grounded)
                {
                    if (jumping)
                    {
                        float jumpVelocity = Mathf.Sqrt(2 * jumpHeight * -gravityIntensity);
                        playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, jumpVelocity, playerRigidbody.velocity.z);
                    }
                    else if (input.magnitude > 0.5f)
                    {
                        playerRigidbody.AddForce(CalculateMovement(sprinting ? sprintSpeed : walkSpeed), ForceMode.VelocityChange);
                    }
                    else
                    {
                        var velocity1 = playerRigidbody.velocity;
                        velocity1 = new Vector3(velocity1.x * 0.2f * Time.fixedDeltaTime, velocity1.y, velocity1.z * 0.2f * Time.fixedDeltaTime);
                        playerRigidbody.velocity = velocity1;
                    }
                }
                else
                {
                    if (input.magnitude > 0.5f)
                    {
                        playerRigidbody.AddForce(CalculateMovement(sprinting ? sprintSpeed * airControl : walkSpeed * airControl), ForceMode.VelocityChange);
                    }
                    else
                    {
                        var velocity1 = playerRigidbody.velocity;
                        velocity1 = new Vector3(velocity1.x * 0.2f * Time.fixedDeltaTime, velocity1.y, velocity1.z * 0.2f * Time.fixedDeltaTime);
                        playerRigidbody.velocity = velocity1;
                    }
                }

                grounded = false;
            }

        }

        Vector3 CalculateMovement(float _speed)
        {
            Vector3 targetVelocity = new Vector3(input.x, 0, input.y);
            targetVelocity = transform.TransformDirection(targetVelocity);

            targetVelocity *= _speed;

            Vector3 velocity = playerRigidbody.velocity;

            if (input.magnitude > 0.5f)
            {
                Vector3 velocityChange = targetVelocity - velocity;
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);

                velocityChange.y = 0;

                return (velocityChange);
            }
            else
            {
                return new Vector3();
            }
        }

        [PunRPC]
        void Dash()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            isDashing = true;

            // Calculate the target velocity based on the dash distance and direction

            Vector3 dashVelocity = playerRigidbody.velocity.normalized * 30f;

            // Apply the velocity change to the rigidbody
            playerRigidbody.velocity = dashVelocity;

            Vector3 dashDirection = dashVelocity.normalized;

            // Instantiate a particle system based on the dash direction
            if (isDashing)
            {
                if (dashDirection == Vector3.forward)
                {
                    forwardDashAnimation.Play();
                }
                else if (dashDirection == Vector3.back)
                {
                    backwardDashAnimation.Play();
                }
                else if (dashDirection == Vector3.left)
                {
                    leftDashAnimation.Play();
                }
                else if (dashDirection == Vector3.right)
                {
                    rightDashAnimation.Play();
                }
            }

            dashesDone++;
            UpdateDashes();
            StartCoroutine(DashCooldown());
        }

        void UpdateDashes()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            UpdateDashUI();
        }

        IEnumerator DashCooldown()
        {
            yield return new WaitForSeconds(0.75f);
            isDashing = false;
        }

        IEnumerator StopDashParticles(ParticleSystem dashParticles)
        {
            // Wait for the duration you want the particles to play
            yield return new WaitForSeconds(0.2f);

            dashParticles.Stop();
        }

        void HandleUpdraft()
        {
            bool updraftAttempt = Input.GetKeyDown(KeyCode.Q);
            if (Time.time < lastUpdraft + updraftDelay)
            {
                if (updraftMode)
                {
                    if (photonView != null)
                    {
                        //photonView.RPC("EndUpdraft", RpcTarget.All);
                        EndUpdraft();
                    }

                }
                return;
            }

            if (updraftAttempt && updrafts < maxUpdrafts)
            {
                if (photonView != null)
                {
                    //photonView.RPC("BeginUpdraft", RpcTarget.All);

                    //photonView.RPC("Updraft", RpcTarget.All);
                    BeginUpdraft();
                    Updraft();
                }

            }
        }

        [PunRPC]
        void Updraft()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            Vector3 updraftVelocity;
            if (!grounded)
            {
                updraftVelocity = Vector3.up * Mathf.Sqrt((updraftHeight / 2.5f) * 2f * -gravityIntensity);
            }
            else
            {
                updraftVelocity = Vector3.up * Mathf.Sqrt(updraftHeight * 2f * -gravityIntensity);
            }
            playerRigidbody.velocity = updraftVelocity;
            lastUpdraft = Time.time;
        }

        [PunRPC]
        void BeginUpdraft()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            updraftMode = true;
            lastUpdraft = Time.time;
            updrafts++;

        }

        [PunRPC]
        void EndUpdraft()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            updraftMode = false;
            if (photonView.IsMine)
            {
                //photonView.RPC("UpdateUpdraftUI", RpcTarget.All);
                UpdateUpdraftUI();
            }

        }

        void HandleSmoke()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            bool keyPressed = Input.GetKeyDown(KeyCode.C);
            if (keyPressed && Time.time - lastSmokeEnded >= smokeDelay && smokes < maxSmokes)
            {
                LaunchSmoke();
            }

            if (smokeMode)
            {
                bool underControl = Input.GetKey(KeyCode.C);
                smokeProjectile.SetUnderControl(underControl);

                bool loseControl = Input.GetKeyUp(KeyCode.C);
                if (loseControl)
                {
                    //photonView.RPC("EndSmokeLaunch", RpcTarget.All);
                    EndSmokeLaunch();
                }
            }
        }

        [PunRPC]
        void LaunchSmoke()
        {
            if (!photonView.IsMine)
                return;
            smokeMode = true;
            Vector3 newPosition = transform.position + transform.forward * 3f;
            // GameObject spawnedObject = PhotonNetwork.Instantiate("SpawnedObject", newPosition, Quaternion.identity);


            //smokeTransform = new GameObject("TransformInFront").transform;
            //smokeTransform.position = newPosition;
            GameObject _smokeSphere = PhotonNetwork.Instantiate(smokeSphere.name, newPosition, mainCamera.transform.rotation);
            smokeProjectile = _smokeSphere.GetComponent<SmokeProjectile>();
            smokeProjectile.SetInitialValues(false, mainCamera);
        }

        [PunRPC]
        void EndSmokeLaunch()
        {
            if (!photonView.IsMine)
                return;
            lastSmokeEnded = Time.time;
            smokeMode = false;
            smokeProjectile.SetUnderControl(false);
            // smokeProjectile.gameObject.GetComponent<PhotonView>().RPC("SetUnderControl", RpcTarget.All, false);
            smokeProjectile = null;
            smokes++;
            if (photonView.IsMine)
            {
                //photonView.RPC("UpdateSmokeUI", RpcTarget.All);
                UpdateSmokeUI();
            }

        }

        [PunRPC]
        void UpdateDashUI()
        {
            if (photonView.IsMine)
            {
                uiElementAbilityOneText.text = $"Dash - {(maxDashes - dashesDone).ToString() + " / " + maxDashes.ToString()}";
                // dashProgressBar.fillAmount = (float)(maxDashes - dashes) / maxDashes;

                uiElementAbilityOneBar.localScale = new Vector3((float)(maxDashes - dashesDone) / maxDashes, 1, 1);
            }

        }
        [PunRPC]
        void UpdateSmokeUI()
        {
            if (photonView.IsMine)
            {
                uiElementAbilityThreeText.text = $"Smokes - {(maxSmokes - smokes).ToString() + " / " + maxSmokes.ToString()}";
                // smokeProgressBar.fillAmount = (float)(maxSmokes - smokes) / maxSmokes;
                uiElementAbilityThreeBar.localScale = new Vector3((float)(maxSmokes - smokes) / maxSmokes, 1, 1);
            }

        }
        [PunRPC]
        void UpdateUpdraftUI()
        {
            if (photonView.IsMine)
            {
                uiElementAbilityTwoText.text = $"Updraft - {(maxUpdrafts - updrafts).ToString() + " / " + maxUpdrafts.ToString()}";
                uiElementAbilityTwoBar.localScale = new Vector3((float)(maxUpdrafts - updrafts) / maxUpdrafts, 1, 1);
                 //updraftProgressBar.fillAmount = (float)(maxUpdrafts - updrafts) / maxUpdrafts
            }
        }

        [PunRPC]
        void SetActiveStateRPC()
        {
            floorBuild.SetActive(false);
            wallBuild.SetActive(false);
            stairBuild.SetActive(false);
        }

        GameObject InstantiatePrefabRandomly(GameObject prefab)
        {
            float randomDistance = Random.Range(20f, 50f);
            float randomAngle = Random.Range(0f, 2f * Mathf.PI);
            float spawnX = transform.position.x + randomDistance * Mathf.Cos(randomAngle);
            float spawnZ = transform.position.z + randomDistance * Mathf.Sin(randomAngle);
            return Instantiate(prefab, new Vector3(spawnX, transform.position.y, spawnZ), Quaternion.identity);
        }

        [PunRPC]
        public void TakeDamage(int damage, int actor)
        {
            if (photonView.IsMine)
            {
                //print("Damage 1");
                currentHealth -= damage;
                if (currentHealth > 0)
                {
                    damageAudio.Play();
                }
                else
                {
                    killAudio.Play();
                }
                if (currentHealth <= 0)
                {
                    currentHealth = 100;
                    pausePanel.SetActive(true);
                    flashbangPanel.gameObject.SetActive(true);
                    if(GameSettings.GameMode == GameMode.BR)
                    {
                        gameManager.SendReduceAliveEvent();
                    }
                    if(GameSettings.GameMode != GameMode.BR)
                    {
                        gameManager.Spawn();
                    }
                    gameManager.SendChangeStatsEvent(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

                    if (actor >= 0)
                    {
                        gameManager.BroadcastKillFeedEvent(actor, PhotonNetwork.LocalPlayer.NickName);
                        gameManager.SendChangeStatsEvent(actor, 0, 1);
                    }
                    PhotonNetwork.Destroy(gameObject);
                }
                RefreshHealthBar();
            }
        }

        public void IncreaseHealth(int health)
        {
            if (photonView.IsMine)
            {
                currentHealth += health;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                RefreshHealthBar();
            }
        }

        public void RefreshHealthBar()
        {
            float healthRatio = (float)currentHealth / (float)maxHealth;
            uiElementHealthbar.localScale = new Vector3(healthRatio, 1.7625f, 1);
        }

        public void UpdateWeaponStats(string ammo, string magzine)
        {
            uiElementAmmo.text = ammo;
            uiElementMagzine.text = magzine;
        }

        [PunRPC]
        public void SetTPPView(int index)
        {
            if (!photonView.IsMine)
            {
                return;
            }
            foreach (Transform gun in ThirdPersonGunHolder)
            {
                gun.gameObject.SetActive(false);
            }
            ThirdPersonGunHolder.GetChild(index).gameObject.SetActive(true);
        }

        [PunRPC]
        public void ApplyFlashEffect(float duration)
        {
            if (!photonView.IsMine)
            {
                return;
            }
            
            StartCoroutine(FlashbangEffectCoroutine(duration));
        }

        private IEnumerator FlashbangEffectCoroutine(float duration)
        {
            // Make the panel fully opaque
            flashbangPanel.gameObject.SetActive(true);
            flashbangPanel.color = new Color(1f, 1f, 1f, 1f);

            // Gradually fade the panel
            float elapsedTime = 0;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(1 - (elapsedTime / duration));
                flashbangPanel.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            // Ensure the panel is fully transparent after the effect
            flashbangPanel.color = new Color(1f, 1f, 1f, 0f);
            flashbangPanel.gameObject.SetActive(false);
        }
    }
}