using UnityEngine;
using Photon.Pun;

namespace SER.LastManStanding
{
    public class SelfDestruct : MonoBehaviourPun
    {
        public float destroyTime = 90f;
        public float rotationSpeed = 30f;
        public SpawnManager spawnManager;
        void Start()
        {
            spawnManager = GameObject.FindWithTag("Spawner").GetComponent<SpawnManager>();

            //if (photonView.IsMine)
            //{
            //    Invoke(nameof(DestroyAfterTime), destroyTime);
            //}
        }

        void Update()
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }

        void DestroyAfterTime()
        {

            if (photonView.IsMine)
            {
                //spawnManager.DecrementTotalSpawnedObjects();
                //PhotonNetwork.Destroy(gameObject);
            }

        }

        void OnCollisionEnter(Collision collision)
        {

            if (collision.gameObject.CompareTag("Player"))
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

}