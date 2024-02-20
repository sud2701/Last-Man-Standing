using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace SER.LastManStanding
{
    public class WeaponSwitch : MonoBehaviour
    {
        public PhotonView playerSetupView;
        private int currentWeapon = 0;
        public ScopeOpener scopeOpener;
        private int highestWeaponActive;
        private bool[] isWeaponActive;
        // Start is called before the first frame update
        void Start()
        {
            isWeaponActive = new bool[3];
            isWeaponActive[0] = true;
            isWeaponActive[1] = false;
            isWeaponActive[2] = false;
            highestWeaponActive = 0;
            SelectWeapon();
        }

        // Update is called once per frame
        void Update()
        {
            if (playerSetupView != null && !scopeOpener.IsScopeOpened())
            {
                int previousWeapon = currentWeapon;

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    currentWeapon = 0;
                }

                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    if (isWeaponActive[1])
                    {
                        currentWeapon = 1;
                    }
                    
                }

                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    if (isWeaponActive[2])
                    {
                        currentWeapon = 2;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    currentWeapon = 3;
                }

                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    currentWeapon = 4;
                }

                if (Input.GetAxis("Mouse ScrollWheel") > 0)
                {
                    if (currentWeapon >= transform.childCount - 1)
                    {
                        currentWeapon = 0;
                    }
                    else
                    {
                        currentWeapon += 1;
                        while(currentWeapon <= 2)
                        {
                            if (!isWeaponActive[currentWeapon])
                            {
                                currentWeapon += 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                if (Input.GetAxis("Mouse ScrollWheel") < 0)
                {
                    if (currentWeapon <= 0)
                    {
                        currentWeapon = highestWeaponActive;
                    }
                    else
                    {
                        currentWeapon -= 1;
                        while (true)
                        {
                            if (!isWeaponActive[currentWeapon])
                            {
                                currentWeapon -= 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                if (previousWeapon != currentWeapon)
                {
                    SelectWeapon();
                }
            }


        }

        void SelectWeapon()
        {

            if (currentWeapon >= transform.childCount)
            {
                currentWeapon = highestWeaponActive;
                if (playerSetupView != null)
                {
                    playerSetupView.RPC("SetTPPView", RpcTarget.All, currentWeapon);
                }
            }
            int index = 0;

            foreach (Transform weapon in transform)
            {
                if (index == currentWeapon)
                {
                    weapon.gameObject.SetActive(true);
                }
                else
                {
                    weapon.gameObject.SetActive(false);
                }

                index++;
            }
        }

        public void ChangeWeapon(int changeIndex)
        {
            currentWeapon = changeIndex;
            SelectWeapon();
        }

        public void UpdateActiveWeapons(int index, bool value)
        {
            if (isWeaponActive[index])
            {
                int i = 0;
                foreach(Transform weapon in transform)
                {
                    if(i == index)
                    {
                        weapon.gameObject.GetComponent<Weapon>().WeaponRestart();
                        ChangeWeapon(index);
                        return;
                    }
                    else
                    {
                        i++;
                        continue;
                    }
                }
            }
            isWeaponActive[index] = true;
            if(index > highestWeaponActive)
            {
                highestWeaponActive = index;
            }
        }
    }

}