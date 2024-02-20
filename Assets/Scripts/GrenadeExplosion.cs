using Photon.Pun.UtilityScripts;
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

namespace SER.LastManStanding
{
    public class GrenadeExplosion : MonoBehaviourPun
    {
        public float explosionForce;
        public float explosionRadius;

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
            HashSet<GameObject> uniqueColliders = new HashSet<GameObject>();
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider hit in colliders)
            {
                // Check if the collider is already in the set to ensure uniqueness
                if (!uniqueColliders.Contains(hit.gameObject))
                {
                    uniqueColliders.Add(hit.gameObject);

                    if (hit.gameObject.GetComponent<PlayerMovementManager>())
                    {

                        hit.gameObject.GetComponent<PhotonView>().RPC("DecreaseHealth", RpcTarget.All, 75);
                    }

                    Rigidbody rb = hit.GetComponent<Rigidbody>();

                    if (rb != null)
                    {
                        rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                    }
                }
            }
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }



        }
    }

}