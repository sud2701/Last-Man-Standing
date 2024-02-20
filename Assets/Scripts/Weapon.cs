using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using Photon.Pun.UtilityScripts;

namespace SER.LastManStanding
{
    public class Weapon : MonoBehaviourPun
    {
        public Camera playerCamera;
        public int weapon_hit_damage;
        public float fire_speed;
        private float nextRound;

        public GameObject hitEffect;

        public int current_magzine;

        public bool isReload;

        public int ammunition_capacity;

        public int mag_ammo;

        public int remaining_bullets;

        //[Range(0, 1)]
        //public float recoilStrength = 0.3f;
        [Range(0, 2)]
        public float recoveryStrength = 0.7f;

        public float upsideMovement = 1f;
        public float backMovement = 0.5f;

        private Vector3 currentPosition;
        private Vector3 recoilSpeed = Vector3.zero;
        private bool isRecoil;
        private bool isRecover;
        private float recoilLength;
        private float recoverLength;

        //public TextMeshProUGUI magzineText;

        //public TextMeshProUGUI ammunitionText;

        private int og_remaining_bullets;

        private int og_mag_ammo;

        private bool first_use = true;

        public GameObject bulletholePrefab;

        public AudioSource audioSource;

        private void Start()
        {
            if (photonView != null && photonView.IsMine)
            {
                og_mag_ammo = mag_ammo;
                og_remaining_bullets = remaining_bullets;
                WeaponRestart();
                first_use = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (photonView != null && photonView.IsMine)
            {
                if (nextRound > 0)
                {
                    nextRound -= Time.deltaTime;
                }
                if (photonView.IsMine && Input.GetButton("Fire1") && nextRound <= 0 && ammunition_capacity > 0 && !isReload && remaining_bullets > 0)
                {
                    nextRound = 1 / fire_speed;


                    photonView.RPC("Shoot", RpcTarget.All);
                }

                if (photonView.IsMine && ammunition_capacity == 0 && !isReload && remaining_bullets > 0)
                {
                    photonView.RPC("WeaponReload", RpcTarget.All);
                }

                if (photonView.IsMine && !isReload && Input.GetKeyDown(KeyCode.R) && remaining_bullets > 0)
                {
                    photonView.RPC("WeaponReload", RpcTarget.All);
                }

                if (isRecoil)
                {
                    RecoilEffect();
                }

                if (isRecover)
                {
                    RecoverEffect();
                }
            }

        }

        [PunRPC]
        void Shoot()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            isRecoil = true;
            isRecover = false;

            Ray raycast = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            RaycastHit hit;
            int gameMode = (int)PhotonNetwork.CurrentRoom.CustomProperties["mode"];

            if (Physics.Raycast(raycast.origin, raycast.direction, out hit, 200f))
            {
                PhotonNetwork.Instantiate(hitEffect.name, hit.point, Quaternion.identity);

                if (hit.transform.gameObject.GetComponent<PlayerMovementManager>())
                {

                    //if (weapon_hit_damage >= hit.transform.gameObject.GetComponent<PlayerMovementManager>().hp)
                    //{
                    //    RoomManager.instance.kills++;
                    //    RoomManager.instance.SetValues();
                    //    PhotonNetwork.LocalPlayer.AddScore(100);
                    //}
                    PlayerMovementManager hitPlayerMovementManager = hit.transform.gameObject.GetComponent<PlayerMovementManager>();
                    if (hitPlayerMovementManager != null)
                    {
                        PhotonView hitPhotonView = hitPlayerMovementManager.GetComponent<PhotonView>();

                        if (hitPhotonView != null)
                        {
                            string hitPlayerNickName = hitPhotonView.Owner.NickName;
                            //print(hitPlayerNickName + " is Player 2");
                            List<string> homeTeamPlayers = PlayerMovementManager.instance.homeTeamPlayers;

                            // Check team membership
                            string nameOfPlayer = PlayerMovementManager.instance.GetComponent<PhotonView>().Owner.NickName;
                            //print(nameOfPlayer);
                            bool isShooterHomeTeam = homeTeamPlayers.Contains(nameOfPlayer);
                            bool isTargetHomeTeam = homeTeamPlayers.Contains(hitPlayerNickName);

                            // Determine if damage should be applied (opposite teams)
                            bool shouldApplyDamage = gameMode == 0 || gameMode == 2 || (isShooterHomeTeam != isTargetHomeTeam);


                            if (hit.transform.gameObject.GetComponent<PhotonView>() && shouldApplyDamage)
                            {
                                //print("Damage 2");
                                hit.transform.gameObject.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, weapon_hit_damage, PhotonNetwork.LocalPlayer.ActorNumber);
                            }
                        }
                    }
                }
            }
            ammunition_capacity--;
            remaining_bullets--;
            audioSource.Play();
            PlayerMovementManager.instance.UpdateWeaponStats((current_magzine).ToString() + " / " + remaining_bullets.ToString(), ammunition_capacity + "/" + mag_ammo);
        }

        [PunRPC]
        void WeaponReload()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            float reloadDelay = 2.5f;
            isReload = true;
            if (current_magzine > 0)
            {
                Invoke(nameof(DelayedReload), reloadDelay);
            }
        }

        void DelayedReload()
        {
            if (ammunition_capacity > 0)
            {
                float remainder = remaining_bullets % mag_ammo;
                if (remainder > 0)
                {
                    current_magzine = (remaining_bullets / mag_ammo) + 1;
                }
                else
                {
                    current_magzine = (remaining_bullets / mag_ammo);
                }
                ammunition_capacity = mag_ammo;
            }
            else
            {
                current_magzine -= 1;
                if (current_magzine == 1)
                {
                    ammunition_capacity = remaining_bullets;
                }
                else
                {
                    ammunition_capacity = mag_ammo;
                }
            }

            //magzineText.text = (current_magzine).ToString() + " / " + remaining_bullets.ToString();
            //ammunitionText.text = ammunition_capacity + "/" + mag_ammo;
            PlayerMovementManager.instance.UpdateWeaponStats((current_magzine).ToString() + " / " + remaining_bullets.ToString(), ammunition_capacity + "/" + mag_ammo);
            isReload = false;
        }

        void RecoilEffect()
        {
            Vector3 positionAfterRecoil = new Vector3(currentPosition.x, currentPosition.y, currentPosition.z - backMovement);

            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, positionAfterRecoil, ref recoilSpeed, recoilLength);

            if (transform.localPosition == positionAfterRecoil)
            {
                isRecoil = false;
                isRecover = true;
            }
        }

        void RecoverEffect()
        {
            Vector3 positionAfterRecovery = currentPosition;
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, positionAfterRecovery, ref recoilSpeed, recoverLength);

            if (transform.localPosition == positionAfterRecovery)
            {
                isRecover = false;
                isRecoil = false;
            }
        }

        public void WeaponRestart()
        {
            remaining_bullets = og_remaining_bullets;
            mag_ammo = og_mag_ammo;
            isReload = false;
            current_magzine = remaining_bullets / mag_ammo;
            ammunition_capacity = remaining_bullets % mag_ammo;
            if (ammunition_capacity == 0)
            {
                ammunition_capacity = mag_ammo;
            }
            if (first_use)
            {
                //magzineText.text = (current_magzine).ToString() + " / " + remaining_bullets.ToString();
                //ammunitionText.text = ammunition_capacity + "/" + mag_ammo;
                PlayerMovementManager.instance.UpdateWeaponStats((current_magzine).ToString() + " / " + remaining_bullets.ToString(), ammunition_capacity + "/" + mag_ammo);
            }
            currentPosition = transform.localPosition;

            recoilLength = 0;
            recoverLength = 1 / fire_speed * recoveryStrength;
        }

        public void Destroy()
        {
            PhotonNetwork.Destroy(gameObject);
        }

        //[PunRPC]
        //public void TakeDamage(int p_damage, int p_actor)
        //{
        //    PlayerMovementManager.instance.TakeDamage(p_damage, p_actor);
        //}
    }

}