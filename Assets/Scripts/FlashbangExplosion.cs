using Photon.Pun;
using UnityEngine;

namespace SER.LastManStanding
{
    public class FlashbangExplosion : MonoBehaviourPun
    {
        public float explosionRadius;
        public float flashDuration; // Duration of the blinding effect
        public float blindingAngle = 100f; // Field of view angle in which players get blinded

        private void Start()
        {
            if (photonView != null)
            {
                if (!photonView.IsMine && PhotonNetwork.IsConnected)
                {
                    return;
                }
                Invoke(nameof(Explode), 4f); // Adjust time as needed
            }
        }

        private void Explode()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider hit in colliders)
            {
                PlayerMovementManager player = hit.gameObject.GetComponent<PlayerMovementManager>();

                if (player != null)
                {
                    if (IsPlayerFacingFlashbang(hit.transform))
                    {
                        // Apply the flash effect
                        hit.gameObject.GetComponent<PhotonView>().RPC("ApplyFlashEffect", RpcTarget.All, flashDuration);
                    }
                }
            }

            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        private bool IsPlayerFacingFlashbang(Transform playerTransform)
        {
            Vector3 dirToFlashbang = (transform.position - playerTransform.position).normalized;
            float angle = Vector3.Angle(playerTransform.forward, dirToFlashbang);

            return angle < blindingAngle / 2;
        }
    }
}
