using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SER.LastManStanding
{
    public class TPPTextRotation : MonoBehaviour
    {

        // Update is called once per frame
        void Update()
        {

            if (Camera.main != null && Camera.main.isActiveAndEnabled)
            {
                transform.LookAt(Camera.main.transform);
            }

        }
    }
}