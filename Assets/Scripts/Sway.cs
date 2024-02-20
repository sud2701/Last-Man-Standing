using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SER.LastManStanding
{
    public class Sway : MonoBehaviour
    {
        public float swayStrength = 0.09f;

        public float smoothness_factor = 3f;

        private Vector3 weapon_origin;

        // Start is called before the first frame update
        void Start()
        {
            weapon_origin = transform.localPosition;
        }

        // Update is called once per frame
        void Update()
        {
            Vector2 weapon_input = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            weapon_input.x = Mathf.Clamp(weapon_input.x, -swayStrength, swayStrength);
            weapon_input.y = Mathf.Clamp(weapon_input.y, -swayStrength, swayStrength);

            Vector3 weapon_target_position = new Vector3(-weapon_input.x, -weapon_input.y, 0);
            transform.localPosition = Vector3.Lerp(transform.localPosition, weapon_target_position + weapon_origin, Time.deltaTime * smoothness_factor);
        }
    }

}