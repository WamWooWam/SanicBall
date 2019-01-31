﻿using Sanicball;
using SanicballCore;
using UnityEngine;

namespace Sanicball.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public class OmniCamera : MonoBehaviour, IBallCamera
    {
        public Rigidbody Target { get; set; }
        public Camera AttachedCamera
        {
            get
            {
                if (!attachedCamera)
                {
                    attachedCamera = GetComponent<Camera>();
                }
                return attachedCamera;
            }
        }
        public ControlType CtrlType { get; set; }

        [SerializeField]
        private float orbitHeight = 0.5f;
        [SerializeField]
        private float orbitDistance = 4.0f;

        private Camera attachedCamera;
        private Quaternion currentDirection = Quaternion.Euler(0, 0, 0);
        private Quaternion currentDirectionWithOffset = Quaternion.Euler(0, 0, 0);
        private Vector3 up = Vector3.up;

        public float fovOffset = 0;

        public void SetDirection(Quaternion dir)
        {
            currentDirection = dir;
        }

        public void Remove()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            //Input
            var targetDirectionOffset = Quaternion.identity;
            Vector2 camVector = GameInput.CameraVector(CtrlType);
            Vector3 orientedCamVector = new Vector3(camVector.x, 0, camVector.y);
            if (orientedCamVector != Vector3.zero)
            {
                Quaternion camQuaternion = Quaternion.Slerp(Quaternion.identity, Quaternion.LookRotation(orientedCamVector), orientedCamVector.magnitude);
                targetDirectionOffset = camQuaternion;
            }

            if (Target != null)
            {
                //Rotate the camera towards the velocity of the rigidbody

                //Set the up vector, and make it lerp towards the target's up vector if the target has a Ball
                Vector3 targetUp = Vector3.up;
                Ball bc = Target.GetComponent<Ball>();
                if (bc)
                {
                    targetUp = bc.Up;
                }
                up = Vector3.Lerp(up, targetUp, Time.deltaTime * 100);

                //Based on how fast the target is moving, create a rotation bending towards its velocity.
                Quaternion towardsVelocity = (Target.velocity != Vector3.zero) ? Quaternion.LookRotation(Target.velocity, up) : Quaternion.identity;
                const float maxTrans = 20f;
                Quaternion finalTargetDir = Quaternion.Slerp(currentDirection, towardsVelocity, Mathf.Max(0, Mathf.Min(-10 + Target.velocity.magnitude, maxTrans) / maxTrans));

                //Lerp towards the final rotation
                currentDirection = Quaternion.Slerp(currentDirection, finalTargetDir, Time.deltaTime * 4);

                //Look for a BallControlInput and set its look direction
                BallControlInput bci = Target.GetComponent<BallControlInput>();
                if (bci != null)
                {
                    bci.LookDirection = currentDirection;
                }

                //Set camera FOV to get higher with more velocity
                AttachedCamera.fieldOfView = Mathf.Lerp(AttachedCamera.fieldOfView, Mathf.Min(60f + (Target.velocity.magnitude), 100f) + fovOffset, Time.deltaTime * 20);

                currentDirectionWithOffset = Quaternion.Slerp(currentDirectionWithOffset, currentDirection * targetDirectionOffset, Time.deltaTime * 6);
                transform.position = Target.transform.position + Vector3.up * orbitHeight + currentDirectionWithOffset * (Vector3.back * orbitDistance);
                transform.rotation = currentDirectionWithOffset;
            }
        }
    }
}