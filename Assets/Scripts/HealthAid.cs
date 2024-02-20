using UnityEngine;
using System.Collections;

namespace SER.LastManStanding
{
    public class HealthAid : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<PlayerMovementManager>().IncreaseHealth(20);
                Destroy(gameObject);
            }
        }
    }
}