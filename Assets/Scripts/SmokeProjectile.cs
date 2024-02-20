using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace SER.LastManStanding
{
    public class SmokeProjectile : MonoBehaviourPun
    {
        [SerializeField] GameObject smokeBall;
        public bool underControl;
        private Vector3 startingPoint;
        private float speed = 20.0f;
        private float maxDistance = 50.0f;
        private float distanceTravelled = 0f;
        private Camera mainCamera;
        private float downwardForce = -2.0f;
        private float downwardForceIncrement = -3.8f;
        void Start()
        {
            startingPoint = transform.position;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (photonView.IsMine)
            {
                CreateSmoke(collision.contacts[0].point);
                PhotonNetwork.Destroy(gameObject);
            }
            // photonView.RPC("CreateSmoke", RpcTarget.All, collision.contacts[0].point);
        }


        void Update()
        {
            if (photonView != null)
            {
                if (!photonView.IsMine && PhotonNetwork.IsConnected)
                {
                    return;
                }


                if (underControl)
                {
                    transform.rotation = mainCamera.transform.rotation;
                }

                Vector3 movementVector = (transform.forward * speed * Time.deltaTime);
                if (!underControl)
                {
                    downwardForce += downwardForceIncrement * Time.deltaTime;
                    movementVector += (transform.up * downwardForce * Time.deltaTime);
                }
                Vector3 newPosition = transform.position + movementVector;

                distanceTravelled = Vector3.Distance(startingPoint, newPosition);
                if (distanceTravelled > maxDistance)
                {
                    if (photonView.IsMine)
                    {
                        CreateSmoke(transform.position);
                        PhotonNetwork.Destroy(gameObject);
                    }
                    // photonView.RPC("CreateSmoke", RpcTarget.All, transform.position);
                }
                else
                {
                    transform.position += movementVector;
                }
            }
        }

        public void SetInitialValues(bool _underControl, Camera _mainCamera)
        {
            underControl = _underControl;
            mainCamera = _mainCamera;

        }

        // [PunRPC]
        public void SetUnderControl(bool _underControl)
        {
            underControl = _underControl;
        }

        // [PunRPC]
        void CreateSmoke(Vector3 position)
        {
            PhotonNetwork.Instantiate(smokeBall.name, position, transform.rotation);
        }
    }
}