using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine;

namespace SER.LastManStanding
{
    public class Pause : MonoBehaviour
    {
        public static bool paused = false;
        public static Pause instance;
        private bool disconnecting = false;

        private void Start()
        {
            if (instance == null)
                instance = this;
        }

        public void TogglePause()
        {
            if (disconnecting) return;

            paused = !paused;

            transform.GetChild(0).gameObject.SetActive(paused);
            ToggleCursorLock();
        }

        public void Quit()
        {
            disconnecting = true;
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene(0);
        }


        public static void ToggleCursorLock()
        {
            if (paused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
        }

    }
}