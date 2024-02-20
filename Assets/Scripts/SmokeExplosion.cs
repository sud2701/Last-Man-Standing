using Photon.Pun.UtilityScripts;
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

namespace SER.LastManStanding
{
    public class SmokeExplosion : MonoBehaviourPun
    {
        public float explosionForce;
        public float explosionRadius;
        public GameObject particleSystemPrefab;
        GameObject particleSystemInstance;

        private void Start()
        {
            if (photonView != null)
            {
                if (!photonView.IsMine && PhotonNetwork.IsConnected)
                {
                    return;
                }
                Invoke(nameof(Explode), 4f);
            }
        }


        private void Explode()
        {
            if (particleSystemPrefab != null)
            {
                // Instantiate the particle system at the position with default rotation
                particleSystemInstance = PhotonNetwork.Instantiate(particleSystemPrefab.name, transform.position, Quaternion.identity);

                // Play the particle system
                ParticleSystem ps = particleSystemInstance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }

                // Optional: Destroy the particle system after it has finished playing
                Invoke(nameof(DestroySmoke), ps.main.duration);
            }
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }



        }

        public void DestroySmoke()
        {
            PhotonNetwork.Destroy(particleSystemInstance);
        }
    }

}