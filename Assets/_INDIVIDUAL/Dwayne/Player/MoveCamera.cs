using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class MoveCamera : MonoBehaviour
    {
        public Transform cameraPosition;
        
        private void LateUpdate()
        {
            transform.position = cameraPosition.position;
        }
    }
}