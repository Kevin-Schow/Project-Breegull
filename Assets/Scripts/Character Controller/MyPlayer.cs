using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using ThirdPersonCameraWithLockOn;

namespace KinematicCharacterController
{
    public class MyPlayer : MonoBehaviour
    {
        public MyCharacterController Character;
        public ThirdPersonCamera CharacterCamera;

        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";
        private const string MouseScrollInput = "Mouse ScrollWheel";
        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            // CharacterCamera.SetFollowTransform(Character.CameraFollowPoint); // OLD CAMERA

            // Ignore the character's collider(s) for camera obstruction checks
            // CharacterCamera.IgnoredColliders.Clear(); // OLD CAMERA
            // CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>()); // OLD CAMERA
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            // Handle rotating the camera along with physics movers
            // if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            // {
            //     CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
            //     CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            // }

            HandleCameraInput();
        }

        private void HandleCameraInput()
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            // float scrollInput = -Input.GetAxis(MouseScrollInput);
// #if UNITY_WEBGL
//         scrollInput = 0f;
// #endif
            // Apply inputs to the camera
            // CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector); // OLD CAMERA

            // Handle toggling zoom level // OLD CAMERA
            // if (Input.GetMouseButtonDown(1))
            // {
            //     CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;
            // }
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);
            characterInputs.CameraRotation = CharacterCamera.transform.rotation; // OLD CAMERA
            characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);
            characterInputs.JumpHeld = Input.GetKey(KeyCode.Space);
            // characterInputs.JumpDown = Input.GetButtonDown("XboxA");
            // characterInputs.JumpHeld = Input.GetButton("XboxA");
            characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);
            characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);
            characterInputs.CrouchHeld = Input.GetKey(KeyCode.C);
            characterInputs.ChargingDown = Input.GetKeyDown(KeyCode.X);
            characterInputs.NoClipDown = Input.GetKeyUp(KeyCode.N);
            characterInputs.ClimbLadder = Input.GetKeyUp(KeyCode.F);

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);

            //Strafe Left
            // if (Input.GetKeyDown(KeyCode.Q))
            // {
                
            // }

            // Apply impulse
            if (Input.GetKeyDown(KeyCode.V))
            {
                Character.Motor.ForceUnground(0.2f);
                Character.AddVelocity(Vector3.up * 20f);
            }
        }
    }
}