using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
namespace SER.LastManStanding
{
    public class WeaponPickup : MonoBehaviourPun
    {
        public WeaponSwitch weaponSwitch;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("AssaultRifle"))
            {
                weaponSwitch.UpdateActiveWeapons(1, true);
                weaponSwitch.ChangeWeapon(1);
            }
            else if (collision.gameObject.CompareTag("SniperRifle"))
            {
                weaponSwitch.UpdateActiveWeapons(2, true);
                weaponSwitch.ChangeWeapon(2);
            }
            else if (collision.gameObject.CompareTag("Pistol"))
            {
                weaponSwitch.UpdateActiveWeapons(0, true);
                weaponSwitch.ChangeWeapon(0);
            }
            //else if (collision.gameObject.CompareTag("HealthKit"))
            //{
            //    if (!photonView.IsMine)
            //    {
            //        return;
            //    }
            //    photonView.RPC("IncreaseHealth", RpcTarget.All, 50);
            //}
        }
    }

}