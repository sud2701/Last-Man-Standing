using UnityEngine;
using Photon.Pun;

namespace SER.LastManStanding
{
    public class SpawnManager : MonoBehaviourPunCallbacks
    {
        public GameObject[] prefabsToSpawn; // This should have exactly 2 weapon prefabs
        public Transform plane;
        public LayerMask spawnLayerMask;
        public GameObject spawnObject;
        public Vector3[] spawnLocations;
        public Vector3[] isLocationAssigned;
        public int lenSpawnLocations;
        public int noSpawnObjs;

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                spawnLocations = new Vector3[lenSpawnLocations];
                int ind = 0;
                foreach (Transform childTransform in spawnObject.transform)
                {
                    spawnLocations[ind] = childTransform.position;
                    ind++;
                }
                InitiateInitialSpawn();
            }
        }

        private void InitiateInitialSpawn()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ShuffleArray(spawnLocations);
                SpawnObjects(noSpawnObjs);
            }
        }

        public void ShuffleArray(Vector3[] array)
        {
            System.Random random = new System.Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);

                // Swap elements
                Vector3 temp = array[i];
                array[i] = array[randomIndex];
                array[randomIndex] = temp;
            }
        }

        void SpawnObjects(int numberOfObjects)
        {
            for (int i = 0; i < numberOfObjects; i++)
            {
                Vector3 randomPosition = spawnLocations[i];

                GameObject prefabToSpawn = GetRandomPrefab();

                GameObject newObject = PhotonNetwork.Instantiate(prefabToSpawn.name, randomPosition, Quaternion.identity);
                newObject.SetActive(true);
            }
            
        }

        GameObject GetRandomPrefab()
        {
            // 50% chance for each weapon
            int randomIndex = Random.Range(0, 2); // Random index 0 or 1
            return prefabsToSpawn[randomIndex];
        }

        Vector3 GetRandomPosition()
        {
            //float minX = bounds.min.x;
            //float maxX = bounds.max.x;
            //float minZ = bounds.min.z;
            //float maxZ = bounds.max.z;

            //float x = Random.Range(minX, maxX);
            //float z = Random.Range(minZ, maxZ);

            //Vector3 randomPosition = new Vector3(x, plane.position.y + 4f, z);


            //Collider[] colliders = Physics.OverlapSphere(randomPosition, distanceBetweenObjects, spawnLayerMask);


            //while (colliders.Length > 0)
            //{
            //    x = Random.Range(minX + 5, maxX - 5);
            //    z = Random.Range(minZ + 5, maxZ - 5);
            //    randomPosition = new Vector3(x, plane.position.y + 4f, z);

            //    colliders = Physics.OverlapSphere(randomPosition, distanceBetweenObjects, spawnLayerMask);
            //}

            //return randomPosition;


            return new Vector3();
        }
    }

}