using System.Collections;
using UnityEngine;
using Photon.Pun;

namespace SER.LastManStanding
{
    public class ScopeOpener : MonoBehaviourPun
    {
        public Animator animator;

        private bool scopeOpened = false;

        public GameObject ScopeView;

        public GameObject ScopeCamera;

        public Camera MainView;

        public float regularScope;

        public float scopeFOV = 15f;

        private void Update()
        {
            if (photonView != null)
            {
                if (PhotonNetwork.IsConnected && photonView.IsMine) // Only handle input for the local player
                {
                    if (Input.GetKeyDown(KeyCode.Z))
                    {
                        scopeOpened = !scopeOpened;
                        animator.SetBool("Scoped", scopeOpened);

                        if (scopeOpened && photonView != null)
                        {
                            photonView.RPC("OnScopeOpenedRPC", RpcTarget.AllBuffered); // Synchronize scope opening
                        }
                        else
                        {
                            if (photonView != null)
                            {
                                photonView.RPC("OnScopeClosedRPC", RpcTarget.AllBuffered); // Synchronize scope closing
                            }
                        }
                    }
                }
            }
        }

        public bool IsScopeOpened()
        {
            return scopeOpened;
        }

        [PunRPC]
        void OnScopeOpenedRPC()
        {
            if (photonView.IsMine)
            {
                Invoke(nameof(OnScopeOpened), 0.15f);
            }
        }

        [PunRPC]
        void OnScopeClosedRPC()
        {
            if (photonView.IsMine)
            {
                OnScopeClosed();
            }
        }

        void OnScopeOpened()
        {
            ScopeView.SetActive(true);
            ScopeCamera.SetActive(false);
            regularScope = MainView.fieldOfView;
            MainView.fieldOfView = scopeFOV;
        }

        void OnScopeClosed()
        {
            ScopeView.SetActive(false);
            ScopeCamera.SetActive(true);
            MainView.fieldOfView = regularScope;
        }
    }
}