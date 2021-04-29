using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;


namespace ThirdPersonCameraWithLockOn
{
	struct CameraPosition{
		private Vector3 position;
		private Transform xForm;

		public Vector3 Position { get {return position;} set { position = value;}}
		public Transform XForm {get {return xForm;} set {xForm = value;}}


		public void Init(string camName, Vector3 pos, Transform transform, Transform parent){
			position = pos;
			xForm = transform;
			xForm.name = camName;
			xForm.parent = parent;
			xForm.localPosition = Vector3.zero;
			xForm.localPosition = position;

		}
	}

	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("ThirdPersonCameraWithLockOn")]
	public class ThirdPersonCamera : MonoBehaviour {

		private enum ButtonPress
		{
			Up,
			Down,
			Held
		}

		[System.Serializable]
		public enum CamStates
		{
			ThirdPersonCam, //regular third person camera
			ResetCam, //resets the cam to the back of the character
			LockOn, // lock on mode
			Off // set this mode when you want to manipulate the camera externally
		}
		
		[Header("General")]
		

		[SerializeField]
		[Tooltip("what object does the camera follow in the scene, if none the camera will find an onbject tagged 'Player'")]
		private Transform follow;

		public Transform Follow
		{
			get { return follow; }
			set { follow = value; }
		}

		[SerializeField]
		private bool inverseXAxis = true;

		public bool InverseXAxis
		{
			get { return inverseXAxis; }
			set { inverseXAxis = value; }
		}
		[SerializeField]
		private bool inverseYAxis = false;

		public bool InverseYAxis
		{
			get { return inverseYAxis; }
			set { inverseYAxis = value; }
		}

		private GameObject lockOnTarget = null;
		public GameObject LockOnTarget
		{
			get { return lockOnTarget; }
			set { lockOnTarget = value; }
		}


		private Vector3 lookAt;
		//[SerializeField]
		private CamStates camstate = CamStates.ThirdPersonCam;
		public CamStates Camstate
		{
			get { return camstate; }
			set { camstate = value; }
		}

		private Vector3 lookDir;


		private Vector3 targetPosition;


		private Vector3 velocityCamSmooth = Vector3.zero;
		
		private const float TARGETING_THRESHOLD = 0.1f;


		//private Vector3 curLookDir;
		//private Vector3 velocityLookDir = Vector3.zero;
		[Space(10)]


		#region Input
		[Header("Input")]


		[SerializeField]
		[Tooltip("Input name for horizontal mouse movement")]
		private string mouseInputX = "Mouse X";
		public string MouseInputX
		{
			get { return mouseInputX; }
			private set { mouseInputX = value; }
		}

		[SerializeField]
		[Tooltip("Input name for vertical mouse movement")]
		private string mouseInputY = "Mouse Y";
		public string MouseInputY
		{
			get { return mouseInputY; }
			private set { mouseInputY = value; }
		}

		[SerializeField]
		[Tooltip("Input name for horizontal right joystick movement")]
		public string rightStickX = "RightStickX";
		public string RightStickX
		{
			get { return rightStickX; }
			set { rightStickX = value; }
		}

		[SerializeField]
		[Tooltip("Input name for vertical right joystick movement")]
		public string rightStickY = "RightStickY";
		public string RightStickY
		{
			get { return rightStickY; }
			set { rightStickY = value; }
		}

		[Space(10)]

		[SerializeField]
		[Tooltip("Enable the Lock On function")]
		private bool enableLockOn = true;
		public bool EnableLockOn
		{
			get { return enableLockOn; }
			set { enableLockOn = value; }
		}
		[SerializeField]
		[Tooltip("Enable the Camera Reset function")]
		private bool enableCamReset = true;
		public bool EnableCamReset
		{
			get { return enableCamReset; }
			set { enableCamReset = value; }
		}
		[SerializeField]
		[Tooltip("Enable the Change Traget function")]
		private bool enableChangeTarget = true;
		public bool EnableChangeTarget
		{
			get { return enableChangeTarget; }
			set { enableChangeTarget = value; }
		}


		[Space(10)]


		[SerializeField]
		[Tooltip("Switch between using KeyCodes input method and Unitys input manager for buttons")]
		private bool useInputManagerInsteadOfKeyCode = false;
		public bool UseInputManagerInsteadOfKeyCode
		{
			get { return useInputManagerInsteadOfKeyCode; }
			set { useInputManagerInsteadOfKeyCode = value; }
		}

		[Space(10)]

		[SerializeField]
		[Tooltip("Key codes for the caemra reset function")]
		private KeyCode[] cameraResetKeyCodes = new KeyCode[]{KeyCode.T, KeyCode.Joystick1Button9};
		public KeyCode[] CameraResetKeyCodes
		{
			get { return cameraResetKeyCodes; }
			private set { cameraResetKeyCodes = value; }
		}

		[SerializeField]
		[Tooltip("Key codes for the lock on function")]
		private KeyCode[] lockOnKeyCodes = new KeyCode[]{KeyCode.LeftShift, KeyCode.Joystick1Button4};
		public KeyCode[] LockOnKeyCodes
		{
			get { return lockOnKeyCodes; }
			private set { lockOnKeyCodes = value; }
		}

		[SerializeField]
		[Tooltip("Key codes for the change targets function")]
		private KeyCode[] changeTargetKeyCodes = new KeyCode[]{KeyCode.LeftControl, KeyCode.Joystick1Button8};
		public KeyCode[] ChangeTargetKeyCodes
		{
			get { return changeTargetKeyCodes; }
			private set { changeTargetKeyCodes = value; }
		}
		
		[SerializeField]
		[Tooltip("Key codes to cycle back through targets")]
		private KeyCode[] previousTargetKeyCodes = new KeyCode[]{KeyCode.LeftAlt};
		public KeyCode[] PreviousTargetKeyCodes
		{
			get { return previousTargetKeyCodes; }
			private set { previousTargetKeyCodes = value; }
		}

		[Space(10)]

		[SerializeField]
		[DrawIf("useInputManagerInsteadOfKeyCode", true)]
		[Tooltip("Input name for the reset camera funcion")]
		public string cameraResetButton = "CamReset";
		public string CameraResetButton
		{
			get { return cameraResetButton; }
			set { cameraResetButton = value; }
		}

		[SerializeField]
		[DrawIf("useInputManagerInsteadOfKeyCode", true)]
		[Tooltip("Input name for the lock on function")]
		public string lockOnButton = "LockOn";
		public string LockOnButton
		{
			get { return lockOnButton; }
			set { lockOnButton = value; }
		}

		[SerializeField]
		[DrawIf("useInputManagerInsteadOfKeyCode", true)]
		[Tooltip("Input name for changing lock on targets")]
		public string changeTargetsButton = "ChangeTargets";
		public string ChangeTargetsButton
		{
			get { return changeTargetsButton; }
			set { changeTargetsButton = value; }
		}

		[SerializeField]
		[DrawIf("useInputManagerInsteadOfKeyCode", true)]
		[Tooltip("Input name for changing lock on targets")]
		public string previousTargetButton = "ChangeTargets";
		public string PreviousTargetButton
		{
			get { return previousTargetButton; }
			set { previousTargetButton = value; }
		}

		[Space(10)]

		[SerializeField]
		[Tooltip("Switch control between mouse and controller when input is detected")]
		private bool dynamicControlTypeDetection = false;
		public bool DynamicControlTypeDetection
		{
			get { return dynamicControlTypeDetection; }
			set { dynamicControlTypeDetection = value; }
		}


		[SerializeField]
		[DrawIf("dynamicControlTypeDetection", false)]
		[Tooltip("Use controller to move the camera, Note: not compatible with usingMouse option")]
		private bool usingController = false;
		public bool UsingController
		{
			get { return usingController; }
			set { usingController = value; }
		}


		[SerializeField]
		[DrawIf("dynamicControlTypeDetection", false)]
		[Tooltip("Use mouse to move the camera, Note: not compatible with usingController option")]
		private bool usingMouse = true;
		public bool UsingMouse
		{
			get { return usingMouse; }
			set { usingMouse = value; }
		}
		[Space(30)]


		#endregion


		#region ThirdPersonCameraProperties
		[Header("Third Person Camera Properties")]


		[SerializeField]
		[Tooltip("The default angle for the camera in ThirdPersonCam mode, this is also used to calculate lockon camera angle")]
		private float defaultYAngle = 20f;
		public float DefaultYAngle
		{
			get { return defaultYAngle; }
			set {
					defaultYAngle = Mathf.Clamp(value, -89f, 89f);
				}
		}

		[SerializeField]
		[Tooltip("Distance between the camera and the target in ThirdPersonCam mode, this is also used to calculate lockon camera distance")]
		private float distance = 6f;
		public float Distance
		{
			get { return distance; }
			set {
					distance = Mathf.Max(value, 0.01f);
				}
		}

		[SerializeField]
		[Tooltip("Camera X axis turn speed")]
		private float xSpeed = 1.0f;
		public float XSpeed
		{
			get { return xSpeed; }
			set {
					xSpeed = Mathf.Max(value, 0f);
				}
		}

		[SerializeField]
		[Tooltip("Camera Y axis turn speed")]
		private float ySpeed = 1.0f;
		public float YSpeed
		{
			get { return ySpeed; }
			set {
					ySpeed = Mathf.Max(value, 0f);
				}
		}

		[SerializeField]
		[Tooltip("Don't change cameras y position when colliding (controllers only), makes the camera maintain its original height")]
		private bool lockCameraYDuringCollision = true;
		public bool LockCameraYDuringCollision
		{
			get { return lockCameraYDuringCollision; }
			set { lockCameraYDuringCollision = value; }
		}

		[SerializeField]
		[Tooltip("Will the camera lerp to its default angle of viewing When the player lets go of the camera analog? (Doesn't work with mouse controls")]
		private bool lerpCameraToDefault = true;
		public bool LerpCameraToDefault
		{
			get { return lerpCameraToDefault; }
			set { lerpCameraToDefault = value; }
		}

		[SerializeField]
		[Tooltip("How fast does the camera lerp to default angle")]
		private float yLerpSpeed = 20f;
		public float YLerpSpeed
		{
			get { return yLerpSpeed; }
			set {
					yLerpSpeed = Mathf.Max(value, 0f);
				}
		}


		[SerializeField]
		[Tooltip("Camera Y axis angle limit min, cameras angle cannot be lower then this in ThirdPersonCam mode")]
		private float yMinLimit = -10f;
		public float YMinLimit
		{
			get { return yMinLimit; }
			set {
					yMinLimit = Mathf.Max(value, -89f);
				}
		}

		[SerializeField]
		[Tooltip("Camera Y axis angle limit min, cameras angle cannot be higher then this in ThirdPersonCam mode")]
		private float yMaxLimit = 50f;
		public float YMaxLimit
		{
			get { return yMaxLimit; }
			set {
					yMaxLimit = Mathf.Min(value, 89f);
				}
		}

		[SerializeField]
		[Tooltip("Turn on use of soft limits, soft limits lerp the camera back below them when no camera input is detected, controller only")]
		private bool useSoftLimits = false;
		public bool UseSoftLimits
		{
			get { return useSoftLimits; }
			set {
					useSoftLimits = value;
				}
		}


		[SerializeField]
		[DrawIf("useSoftLimits", true)]
		[Tooltip("Camera Y axis angle min soft limit, camera can move below this value but will lerp back when controller camera input is released, needs Use Soft Limits enabled")]
		private float yMinSoftLimit = 20f;
		public float YMinSoftLimit
		{
			get { return yMinSoftLimit; }
			set {
					yMinSoftLimit = Mathf.Max(value, -89f);
				}
		}

		[SerializeField]
		[DrawIf("useSoftLimits", true)]
		[Tooltip("Camera Y axis angle max soft limit, camera can move above this value but will lerp back when controller camera input is released, needs Use Soft Limits enabled")]
		private float yMaxSoftLimit = 20f;
		public float YMaxSoftLimit
		{
			get { return yMaxSoftLimit; }
			set {
					yMaxSoftLimit = Mathf.Min(value, 89f);
				}
		}


		
		[SerializeField]
		[Tooltip("Offset the camera look at target when in free cam mode, the camera will look at relative to the player origin, with this u can set the camera look at target a point relative to player characetr origin")]
		public Vector3 lookoffset = new Vector3(0,2.5f,0);
		public Vector3 Lookoffset
		{
			get { return lookoffset; }
			private set { lookoffset = value; }
		}

		
		[SerializeField]
		[Tooltip("Smooth time for the camera movement")]
		private float camSmoothDampTime = 0.06f;
		public float CamSmoothDampTime
		{
			get { return camSmoothDampTime; }
			private set { camSmoothDampTime = Mathf.Max(0, camSmoothDampTime); }
		}

		[SerializeField]
		[Tooltip("Smooth time for the camera movement when avoiding clipping (should be lower then cam smooth damp time)")]
		private float camClippingSmoothDampTime = 0.04f;
		public float CamClippingSmoothDampTime
		{
			get { return camClippingSmoothDampTime; }
			private set { camClippingSmoothDampTime = Mathf.Max(0, camClippingSmoothDampTime); }
		}

		[SerializeField]
		[Tooltip("Margins of the Boxcast for camera collision (0 = camera near clip plane dimensions")]
		private float collisionMargin = 0.05f;
		public float CollisionMargin 
		{
			get { return collisionMargin; }
			set { collisionMargin = Mathf.Max(0, value); }
		}

		[SerializeField]
		[Tooltip("Layers that are obstacles to the camera")]
		private LayerMask camObstacle;
		public LayerMask CamObstacle
		{
			get { return camObstacle; }
			set { camObstacle = value; }
		}

		#endregion

		#region LockOnProperties
		[Header("Lock On Properties")]

		[SerializeField]
		[Tooltip("Tag by which Lock-onable gameobjects are identified")]
		public string lockOnTargetsTag = "LockOnTarget";
		public string LockOnTargetsTag
		{
			get { return lockOnTargetsTag; }
			set { lockOnTargetsTag = value; }
		}

		[SerializeField]
		[Tooltip("Whether the lock on button is a toggle or hold to engage")]
		private bool lockOnToggle = true;
		public bool LockOnToggle
		{
			get { return lockOnToggle; }
			set { lockOnToggle = value; }
		}
        
        // set when the lock on toggle is active (except during cooldown)
        private bool lockOnToggleEngaged = false;
        public bool LockOnToggleEngaged
        {
            get { return lockOnToggleEngaged; }
            set { lockOnToggleEngaged = value; }
        }

        // set when the lock on toggle is pressed this frame
        private bool lockOnTogglePressed = false;
        public bool LockOnTogglePressed
        {
            get { return lockOnTogglePressed; }
            set { lockOnTogglePressed = value; }
        }

        [SerializeField]
        [Tooltip("Whether te camera can be manually controled in lock on mode or not, when off the camera will stick to the characters back")]
        private bool lockOnManualControl = true;
        public bool LockOnManualControl
        {
            get { return lockOnManualControl; }
            set { lockOnManualControl = value; }
        }

        [SerializeField]
        [Tooltip("When off the camera will only look at the target but not move from its third person position")]
        private bool experimentalTurnOffAutomaticDistanceCalculation = false;
        public bool ExperimentalTurnOffAutomaticDistanceCalculation
        {
            get { return experimentalTurnOffAutomaticDistanceCalculation; }
            set { experimentalTurnOffAutomaticDistanceCalculation = value; }
        }

        [SerializeField]
        [Tooltip("Will the lock on disingage if angles get too steep")]
        private bool experimentalLockOnDisingageOnSteepAngle = false;
        public bool LockOnDisingageOnSteepAngle
        {
            get { return experimentalLockOnDisingageOnSteepAngle; }
            set { experimentalLockOnDisingageOnSteepAngle = value; }
        }

        [SerializeField]
		[DrawIf("experimentalLockOnDisingageOnSteepAngle", true)]
        [Tooltip("Min angle when reached to disingage lockon")]
        private float experimentalLockOnDisingageMinAngle = -10f;
        public float ExperimentalLockOnDisingageMinAngle
        {
            get { return experimentalLockOnDisingageMinAngle; }
            set
            {
                experimentalLockOnDisingageMinAngle = Mathf.Max(value, -89f);
            }
        }

        [SerializeField]
		[DrawIf("experimentalLockOnDisingageOnSteepAngle", true)]
        [Tooltip("Min angle when reached to disingage lockon")]
        private float experimentalLockOnDisingageMaxAngle = 50f;
        public float ExperimentalLockOnDisingageMaxAngle
        {
            get { return experimentalLockOnDisingageMaxAngle; }
            set
            {
                experimentalLockOnDisingageMaxAngle = Mathf.Min(value, 89f);
            }
        }

        [SerializeField]
        [Range(0.1f, 1.0f)]
        [Tooltip("The interpolated look at target between the follow character and the target (0 meaning look at the character, and 1 meaning look at the target)")]
        private float lockOnFollowToTargetRatio = 0.5f;
        public float LockOnFollowToTargetRatio
        {
            get { return lockOnFollowToTargetRatio; }
            set
            {
                lockOnFollowToTargetRatio = Mathf.Clamp(value, 0.0f, 1.0f);
            }
        }



        [SerializeField]
		[Tooltip("How far from the camera can the lock on target be? 0 means infinite")]
		private float lockOnDistanceLimit = 0f;
		public float LockOnDistanceLimit
		{
			get { return lockOnDistanceLimit; }
			set {
					lockOnDistanceLimit = Mathf.Max(value, 0f);
				}
		}


		[SerializeField]
		[Tooltip("Disingage Lock On when the locked on target is outside Lock On Distance Limit")]
		private bool breakLockOnWhenOutOfRange = false;
		public bool BreakLockOnWhenOutOfRange
		{
			get { return breakLockOnWhenOutOfRange; }
			set { breakLockOnWhenOutOfRange = value; }
		}

		[SerializeField]
		[Tooltip("Disallow engaging lock on if the target ios behind a cam obstacle, note: this only disallows lock on engage, a locked on target wont be disingaged if it goes behind a wall")]
		private bool disallowLockingOnToTargetBehindWall = true;
		public bool DisallowLockingOnToTargetBehindWall
		{
			get { return disallowLockingOnToTargetBehindWall; }
			set { disallowLockingOnToTargetBehindWall = value; }
		}

		private GameObject[] lockOnTargets;
        public GameObject[] LockOnTargets
        {
            get { return lockOnTargets; }
            set { lockOnTargets = value; }
        }
		private int lockOnCurrent = 0;
        public int LockOnCurrent
        {
            get { return lockOnCurrent; }
            set { lockOnCurrent = value; }
        }

        [SerializeField]
		[Tooltip("Optional, use a graphic over the locked on gameobject when lockon is active")]
		private RectTransform lockOnReticle;
		public RectTransform LockOnReticle
		{
			get { return lockOnReticle; }
			set { lockOnReticle = value; }
		}

		[SerializeField]
		[Tooltip("Display Lock On Reticle when holding Lock On in off mode")]
		private bool lockOnReticleWorksWithOffMode = false;
		public bool LockOnReticleWorksWithOffMode
		{
			get { return lockOnReticleWorksWithOffMode; }
			set { lockOnReticleWorksWithOffMode = value; }
		}


		[SerializeField]
		[Tooltip("How fast does the lock on reticle grow to full size")]
		private float lockonAnimSpeed = 30f;
		public float LockonAnimSpeed
		{
			get { return lockonAnimSpeed; }
			set {
					lockonAnimSpeed = Mathf.Max(value, 0f);
				}
		}


		private Vector3 lockOnLookAt; //between player and enemy
		private Vector3 smoothLookAt;
		//private Vector3 smoothCameraTracking;

		[SerializeField]
		[Tooltip("How fast does the camera look thowards a new target when turning on lockon")]
		private float lookSpeed = 5f;
		public float LookSpeed
		{
			get { return lookSpeed; }
			set {
					lookSpeed = Mathf.Max(value, 0f);
				}
		}
	
		[SerializeField]
		[Tooltip("Distance below which the camera can rotate 360 in lock on mode, above this distance camera can only rotate a limited angle around the character")]
		private float lockOnCameraFullRotationMaxDistance = 16;
		public float LockOnCameraFullRotationMaxDistance
		{
			get { return lockOnCameraFullRotationMaxDistance; }
			set {
					lockOnCameraFullRotationMaxDistance = Mathf.Max(value, 0f);
				}
		}

        // calculates angle transition between far cam and close cam mode
        float angleLimitCurrent = 180f;
        float angleLimitStart = 180f;


        [SerializeField]
        [Tooltip("Transition speed between full rotation and limited rotation algorithms")]
        private float farCamTransitionSpeed = 1000f;
        public float FarCamTransitionSpeed
        {
            get { return farCamTransitionSpeed; }
            set
            {
                farCamTransitionSpeed = Mathf.Max(value, 0f);
            }
        }



        bool LNcooldown = false;

		[SerializeField]
		[Tooltip("When disingaging lockon, the camera doesnt move into the next mode for this amount of time (helps dealing with rapid lockon spamming), off for lock on toggle")]
		private float lockOnCoolDownTime = 0.5f;
		public float LockOnCoolDownTime
		{
			get { return lockOnCoolDownTime; }
			set {
					lockOnCoolDownTime = Mathf.Max(value, 0f);
				}
		}


		[SerializeField]
		[Tooltip("In lockon, keeps the character above the screen bottom by this floor distance")]
		private float lockOnScreenBottomMargin = 0.6f;
		public float LockOnScreenBottomMargin
		{
			get { return lockOnScreenBottomMargin; }
			set {
					lockOnScreenBottomMargin = Mathf.Max(value, 0f);
				}
		}

		[SerializeField]
		[Tooltip("In lock on, while far cam is active, the player can only rotate the camera a specific angle range, this value determines how far inbetween the middle and the border can the player character go")]
		private float lockOnRotationRangePercent = 40f;
		public float LockOnRotationRangePercent
		{
			get { return lockOnRotationRangePercent; }
			set {
					lockOnRotationRangePercent = Mathf.Max(value, 0f);
				}
		}


		[SerializeField]
		[Tooltip("Stop the camera going up with the player when the player is jumping in Lock On mode, it is necerrsary to call UpdateJumpingStatus(bool isJumping) and update the jumping statusfor this to work, see SimpleCharacetrController for example")]
		private bool stopCameraFollowingYWhenPlayerIsJumping = true;
		public bool StopCameraFollowingYWhenPlayerIsJumping
		{
			get { return stopCameraFollowingYWhenPlayerIsJumping; }
			set { stopCameraFollowingYWhenPlayerIsJumping = value; }
		}


		private bool playerJumping = false;


		#endregion

		#region Misc
		[Header("Camera Fade objects")]


		[SerializeField]
		[Tooltip("Turn On/Off the Camera fade algorith that fades objects that are inbetween the player and the camera")]
		private bool cameraFadeObjects = true;
		public bool CameraFadeObjects
		{
			get { return cameraFadeObjects; }
			set { cameraFadeObjects = value; }
		}

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Layer of objects that fade when they are infront of the camera")]
		private LayerMask cameraFadeLayer;
		public LayerMask CameraFadeLayer
		{
			get { return cameraFadeLayer; }
			set { cameraFadeLayer = value; }
		}


		// [SerializeField]
		// [Tooltip("Tag of objects that fade when the camera is infront of them")]
		// private string cameraFadeTag = "CameraFade";
		// public string CameraFadeTag
		// {
		// 	get { return cameraFadeTag; }
		// 	set { cameraFadeTag = value; }
		// }

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Radius of the spherecast that determines if an object is infront of the camera and needs to be faded")]
		private float fadeObjectsSpherecastRadius = 0.6f;
		public float FadeObjectsSpherecastRadius
		{
			get { return fadeObjectsSpherecastRadius; }
			set {
					fadeObjectsSpherecastRadius = Mathf.Max(value, 0f);
				}
		}

		

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Cam Fade object will always be fully transparent if infront of the camera")]
		private bool alwaysFullyTransparent = false;
		public bool AlwaysFullyTransparent
		{
			get { return alwaysFullyTransparent; }
			set { alwaysFullyTransparent = value; }
		}

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Distance between the camera and a camFade object in which the cameFade object becomes fully transparent, futher then that distance the objects transparecncy is calculated using distance")]
		private float fullTransparencyDistance = 2f;
		public float FullTransparencyDistance
		{
			get { return fullTransparencyDistance; }
			set {
					fullTransparencyDistance = Mathf.Max(value, 0f);
				}
		}

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Fade in speed , 0 means instant")]
		private float fadeInSpeed = 10f;
		public float FadeInSpeed
		{
			get { return fadeInSpeed; }
			set {
					if( value < 0f)
					{
						fadeInSpeed = 0f;
					}
					else
					{
						fadeInSpeed = value;
					}
				}
		}

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Fade out speed, 0 means instant")]
		private float fadeOutSpeed = 10f;
		public float FadeOutSpeed
		{
			get { return fadeOutSpeed; }
			set {
					if( value < 0f)
					{
						fadeOutSpeed = 0f;
					}
					else
					{
						fadeOutSpeed = value;
					}
				}
		}

		

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Fade player to transparent when the camera gets within Player Fade Distance")]
		private bool fadePlayerWhenClose = true;
		public bool FadePlayerWhenClose
		{
			get { return fadePlayerWhenClose; }
			set { fadePlayerWhenClose = value; }
		}

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Distance at which the follow object fades to transparent")]
		private float playerFadeDistance = 1f;
		public float PlayerFadeDistance
		{
			get { return playerFadeDistance; }
			set {
					if( playerFadeDistance < 0f)
					{
						playerFadeDistance = 0f;
					}
					else
					{
						playerFadeDistance = value;
					}
				}
		}

		[SerializeField]
		[DrawIf("cameraFadeObjects", true)]
		[Tooltip("Shader to use when fading out")]
		private Shader fadeShader;
		public Shader FadeShader
		{
			get { return fadeShader; }
			set { fadeShader = value; }
		}


		#endregion
		#region Debug

		[Header("DEBUG")]
		//[HideInInspector]
		[SerializeField]
		[Tooltip("Turn on debug (must be true for the other debugs to work")]
		private bool debugOn = true;
		public bool DebugOn
		{
			get { return debugOn; }
			set { debugOn = value; }
		}



		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Draw third person debug lines")]
		private bool debugThirdPersonMode = false;
		public bool DebugThirdPersonMode
		{
			get { return debugThirdPersonMode; }
			set { debugThirdPersonMode = value; }
		}


		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Log fade values")]
		private bool debugCamFade = false;
		public bool DebugCamFade
		{
			get { return debugCamFade; }
			set { debugCamFade = value; }
		}

		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Draw colisions")]
		private bool debugCollision = false;
		public bool DebugCollision
		{
			get { return debugCollision; }
			set { debugCollision = value; }
		}


		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Draw lock on vectors")]
		private bool debugLockOn = false;
		public bool DebugLockOn
		{
			get { return debugLockOn; }
			set { debugLockOn = value; }
		}

		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Draw vertical movement comparison vectors at world origin")]
		private bool debugCameraVerticalMovement = false;
		public bool DebugCameraVerticalMovement
		{
			get { return debugCameraVerticalMovement; }
			set { debugCameraVerticalMovement = value; }
		}

		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Log camera distance behaviour")]
		private bool debugLockOnCameraDistanceBehaviour = false;
		public bool DebugLockOnCameraDistanceBehaviour
		{
			get { return debugLockOnCameraDistanceBehaviour; }
			set { debugLockOnCameraDistanceBehaviour = value; }
		}

		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Log lock on calculations")]
		private bool debugCameraLockOnCalculations = false;
		public bool DebugCameraLockOnCalculations
		{
			get { return debugCameraLockOnCalculations; }
			set { debugCameraLockOnCalculations = value; }
		}

		[SerializeField]
		[DrawIf("debugOn", true)]
		[Tooltip("Log camera vertical calculation mode")]
		private bool debugCameraLookingUpDown = false;
		public bool DebugCameraLookingUpDown
		{
			get { return debugCameraLookingUpDown; }
			set { debugCameraLookingUpDown = value; }
		}


		#endregion


		float x = 180.0f;
		float y = 0.0f;

		float yLockOn = 0.0f;
		float xLockOn = 0.0f;

		float lockOnDistance;

		private float hFOV;




		//private float widthDistance;
		//private float calibratedDistance;



		private Camera cam;
		private Vector3 characterOffset;


		private RayHitComparer rayHitComparer;

		//private Quaternion lastRot;
		//private float yprev;
		//private float xprev;

		private float xDelta;
		private float yDelta;

		//private bool isCurrentPositionInCollision = false;

		[ExecuteInEditMode]
		void OnValidate()
		{

			yMinLimit = Mathf.Max(yMinLimit, -89f);
			yMaxLimit = Mathf.Min(yMaxLimit, 89f);
			yMinSoftLimit = Mathf.Max(yMinSoftLimit, -89f);
			yMaxSoftLimit = Mathf.Min(yMaxSoftLimit, 89f);
			defaultYAngle = Mathf.Clamp(defaultYAngle, -89f, 89f);
			distance = Mathf.Max(distance, 0.01f);
			yLerpSpeed = Mathf.Max(0, yLerpSpeed);
			camSmoothDampTime = Mathf.Max(0, camSmoothDampTime);
			camClippingSmoothDampTime = Mathf.Max(0, camClippingSmoothDampTime);
			collisionMargin = Mathf.Max(0, collisionMargin);

			lockOnDistanceLimit = Mathf.Max(0, lockOnDistanceLimit);
			lookSpeed = Mathf.Max(0, lookSpeed);
			lockOnCoolDownTime = Mathf.Max(0, lockOnCoolDownTime);
			lockOnScreenBottomMargin = Mathf.Max(0, lockOnScreenBottomMargin);
			lockOnRotationRangePercent = Mathf.Max(0, lockOnRotationRangePercent);
            farCamTransitionSpeed = Mathf.Max(farCamTransitionSpeed, 0f);

            fadeObjectsSpherecastRadius = Mathf.Max(0, fadeObjectsSpherecastRadius);
			fadeInSpeed = Mathf.Max(fadeInSpeed, 0);
			fadeOutSpeed = Mathf.Max(fadeOutSpeed, 0);

		}

		private bool GetCameraInput(string buttonName, KeyCode[] keycodes, ButtonPress press)
		{
			if(useInputManagerInsteadOfKeyCode)
			{
				try{ 
					switch(press)
					{
						case ButtonPress.Held: 
							return Input.GetButton(buttonName);
						case ButtonPress.Down:
							return Input.GetButtonDown(buttonName);
						case ButtonPress.Up:
							return Input.GetButtonUp(buttonName);
						default: return false;
					}
				}
				catch (UnityEngine.UnityException exp)
				{
					Debug.LogError(exp);
					return false;
				}
				
			}
			else
			{
				bool pressed = false;
				for(int i = 0; i < keycodes.Length; i++)
				{
					switch(press)
					{
						case ButtonPress.Held: 
							pressed = pressed || Input.GetKey(keycodes[i]);
							break;
						case ButtonPress.Down:
							pressed = pressed || Input.GetKeyDown(keycodes[i]);
							break;
						case ButtonPress.Up:
							pressed = pressed || Input.GetKeyUp(keycodes[i]);
							break;
						default: return false;
					}
					
				}
				return pressed;
			}
		}


		private float GetCameraAxis(string axisName)
		{
			try
			{
				return Input.GetAxis(axisName);
			}
			catch(UnityEngine.UnityException exp)
			{
				Debug.Log(exp);
				return 0.0f;
			}
		}


		void Start () {

			if(follow == null)
			{
				Debug.LogWarning("Third Person Camera With Lock On follow not assigned, trying to follow a player tag by default.", follow);
				// folow the player!!
				follow = GameObject.FindWithTag ("Player").transform;
				if (follow == null)
				{
					Debug.LogError("Third Person Camera With Lock On not following any object, assign an object to follow variable", follow);
					//follow = gameObject.transform;
				}
			}

			

			lookDir = follow.forward;
			//curLookDir = follow.forward;

			cam = gameObject.GetComponent("Camera") as Camera;


			float cameraHeightAt1 = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad *.5f);
			hFOV = Mathf.Atan(cameraHeightAt1 * cam.aspect) * 2 * Mathf.Rad2Deg; //needs recalc on screen change


			if(lockOnReticle)
			{
				lockOnReticle.sizeDelta = Vector2.zero;
			}

			smoothLookAt = follow.position + lookoffset;
			lookAt = smoothLookAt;


			rayHitComparer = new RayHitComparer();


			x = Vector3.Angle(follow.forward, Vector3.forward);
			if(follow.forward.x < 0){ x = 360 - x;}
			y = defaultYAngle;

			Quaternion rotation = Quaternion.Euler(y, x, 0);

			Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);

			Vector3 position = rotation * negDistance + lookAt; //rotate negdistance by rot around 000 and then translate in look target direction

			targetPosition = position;

			BoxCastWallCollision(characterOffset, ref targetPosition, false);

			transform.position = targetPosition;

			transform.LookAt (lookAt);

		}


		void Update () {

			// prepare lock on targets

			

			if (enableLockOn && GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Down)
                && (!lockOnToggle || !lockOnToggleEngaged)) {
				lockOnCurrent = -1;
				lockOnTargets = SortLockOns();
				if(lockOnTargets != null && lockOnTargets.Length > 0){
					//lockOnTarget = lockOnTargets[0];
					lockOnCurrent = lockOnTargets.Length - 1;
					ChangeTarget();
					
				}
				
			}

			

			if (enableLockOn && (GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Held) || lockOnToggleEngaged)) {
				//enable targetswitch
				if(enableChangeTarget && GetCameraInput(changeTargetsButton, changeTargetKeyCodes, ButtonPress.Down) && lockOnTargets != null && lockOnTargets.Length > 0){
					ChangeTarget();
				}

				if(enableChangeTarget && GetCameraInput(previousTargetButton, previousTargetKeyCodes, ButtonPress.Down) && lockOnTargets != null && lockOnTargets.Length > 0){
					PreviousTarget();
				}

			}

			// check if lockon target is out of range
				if (breakLockOnWhenOutOfRange && lockOnDistanceLimit > 0)
				{
					if (lockOnTarget != null && Vector3.Distance(lockOnTarget.transform.position, transform.position) > lockOnDistanceLimit)
					{
						lockOnTarget = null;
					}
				}

		}

		void LateUpdate(){
			
			// hof and aspect are calculated every frame so that the screen can be dragged mid gameplay
			float cameraHeightAt1 = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad *.5f);
			hFOV = Mathf.Atan(cameraHeightAt1 * cam.aspect) * 2 * Mathf.Rad2Deg;

            //float camhorizontal = Mathf.Tan(hFOV * Mathf.Deg2Rad *0.5f) + 0.01f;

            
			if (camstate == CamStates.Off) {
				lookAt = follow.position + lookoffset;
				
				if(lockOnReticle)
				{
					if(lockOnReticleWorksWithOffMode)
					{
						// if the player is holding the lock on button
						if(enableLockOn && GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Held))
						{
							Vector2 lockOnPos = Camera.main.WorldToViewportPoint(lockOnTarget.transform.position);
							lockOnReticle.anchorMin = lockOnPos;
							lockOnReticle.anchorMax = lockOnPos;

							lockOnReticle.sizeDelta = Vector2.Lerp(lockOnReticle.sizeDelta, new Vector2(50f, 50f), lockonAnimSpeed * Time.deltaTime);
						}
						// else remove the lock on reticle
						else
						{
							lockOnReticle.sizeDelta = Vector2.zero;
						}

					}
					else
					{
						lockOnReticle.sizeDelta = Vector2.zero;
					}
				}
				//attach external script to handle code
				return;
			}



			characterOffset = follow.position + new Vector3 (0f, lookoffset.y, 0f);
			lookAt = follow.position + lookoffset;

			// The camera will remain in lockon mode for lock for lockOnCoolDownTime time
            // exit lock on
            // if lock on is not a toggle exit when the button is released
            // if lock on is a toggle exit when the button is pressed
			if (enableLockOn && camstate == CamStates.LockOn && 
                ((!lockOnToggle && GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Up))
                || (lockOnToggle && GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Down) && lockOnToggleEngaged)))
            {
                Debug.Log("toggle depressed");

                if (lockOnToggle)
                {
                    lockOnTogglePressed = true;
                    lockOnToggleEngaged = false;
                }
                else {
				    StartCoroutine("LockOnCooldown");
                }

            }

           // if lock on is a toggle keep the camstate in lock on and check if we exited the state
            if (camstate == CamStates.LockOn && lockOnToggle && enableLockOn)
            {
                // exit the state
                if (lockOnToggleEngaged == false)
                {
                    camstate = CamStates.ThirdPersonCam;
                    GetXYFromXYLockOn();
                }
            }
            // if lock on isnt toggle camstate is third person unless determined otherwise later
            else if (enableLockOn)
            {
                camstate = CamStates.ThirdPersonCam;
            }

            if (camstate != CamStates.ThirdPersonCam)
                Debug.Log(camstate);

            // lock on cooldown should still behave like lock on mode
            if (LNcooldown)
				camstate = CamStates.LockOn;


            if (enableLockOn || (!enableLockOn && camstate == CamStates.LockOn))
            {
                if ((lockOnToggle && GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Down) && lockOnTarget != null && !lockOnTogglePressed) 
                    || (!lockOnToggle && GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Held) && lockOnTarget != null))
                {
                    camstate = CamStates.LockOn;

                    if (lockOnToggle)
                    {
                        lockOnToggleEngaged = true;
                        lockOnTogglePressed = true;
                    }
                }
                
            }

            if (enableCamReset && !(camstate == CamStates.LockOn) && GetCameraInput(cameraResetButton, cameraResetKeyCodes, ButtonPress.Down))
            {
                camstate = CamStates.ResetCam;
            }



			// switch state to Lock On
			if (/* Input.GetButtonDown (lockOnButton) */enableLockOn && GetCameraInput(lockOnButton, lockOnKeyCodes, ButtonPress.Down) 
                && ((lockOnToggleEngaged) || !lockOnToggle)) {

                InitiateLockOn_Internal();

			}

            lockOnTogglePressed = false;


            /**
				Camera modes are handled here
			 */
            switch (camstate) {
			/**
				ThirdPersonCam is the regular third person camera mode
			 */
			case CamStates.ThirdPersonCam:

				if(lockOnReticle != null)
				{
					lockOnReticle.sizeDelta = Vector2.zero;
				}

				if(!usingController && !usingMouse)
				{
					//camstate = CamStates.ResetCam;
					x = Vector3.Angle(follow.forward, Vector3.forward);
					if(follow.forward.x < 0){ x = 360 - x;}
					y = defaultYAngle;
					//return;
				}

				if (dynamicControlTypeDetection)
				{
					if(usingController)
					{
						if(GetCameraAxis(rightStickX) == 0 && GetCameraAxis(rightStickY) == 0 && (GetCameraAxis(mouseInputX) > 0 || GetCameraAxis(mouseInputY) > 0))
						{
							toggleInput();
						}
					}
					if (usingMouse)
					{
						if((GetCameraAxis(rightStickX) > 0 || GetCameraAxis(rightStickY) > 0) && GetCameraAxis(mouseInputX) == 0 && GetCameraAxis(mouseInputY) == 0)
						{
							toggleInput();
						}
					}
				}
				/**
					Mouse input
				 */
				if(usingMouse)
				{
					//analog movement
					xDelta = GetCameraAxis(mouseInputX) * xSpeed * distance;
					yDelta = GetCameraAxis(mouseInputY) * ySpeed * distance;

					x += inverseXAxis ? xDelta : -xDelta;
					y += InverseYAxis ? yDelta : -yDelta;
					// x += GetCameraAxis(horizontalAxis) * xSpeed * distance *  0.02f;
					// y -= GetCameraAxis(verticalAxis) * ySpeed * distance * 0.02f;
				}



				/**
					Controller input
				 */
				if(usingController)
				{
					// if no camera controller input is detected
					if(GetCameraAxis(rightStickY) == 0)
					{
						if(useSoftLimits)
						{
							if( y > yMaxLimit && y < yMaxLimit + yLerpSpeed)
							{
								y = yMaxSoftLimit;
							}
							else if (y < yMinSoftLimit && y > yMinSoftLimit - yLerpSpeed)
							{
								y = yMinSoftLimit;
							}
							else {
								if(y > yMaxSoftLimit)
								{
									y = y-yLerpSpeed;
								}
								else if (y < yMinSoftLimit)
								{
									y = y + yLerpSpeed;
								}
							}
						}
						else if (lerpCameraToDefault)
						{
							if (y < yLerpSpeed+ defaultYAngle && y > defaultYAngle - yLerpSpeed)
								y = defaultYAngle;
							else
								y = (y > defaultYAngle ? y-yLerpSpeed : y+yLerpSpeed);

						}
					}
					else
					{
						yDelta = GetCameraAxis(rightStickY) * ySpeed * distance;
						y = (inverseYAxis) ? y +yDelta : y-yDelta;

					}

					xDelta = GetCameraAxis(rightStickX) * xSpeed * distance;
					x = (inverseXAxis) ? x +xDelta : x-xDelta;

				}
				y = ClampAngle(y, yMinLimit, yMaxLimit);


				//make sure the camera can keep up with the movement speed

				// ole algorithm that uses only smoothdamp

				lookDir = characterOffset - this.transform.position;
				lookDir.y = 0;
				lookDir.Normalize ();

				
				MoveCamera(x, y, distance, lookAt, lockCameraYDuringCollision);

				smoothLookAt = SmoothLookingAt(smoothLookAt, lookAt, lookSpeed);

				transform.LookAt (smoothLookAt);

				break;
			/**
				Reset cam resets the camera to its default angle and position
			 */
			case CamStates.ResetCam:

				x = Vector3.Angle(follow.forward, Vector3.forward);
				if(follow.forward.x < 0){ x = 360 - x;}
				y = defaultYAngle;

				MoveCamera(x, y, distance, lookAt, lockCameraYDuringCollision);

				transform.LookAt (lookAt);



				break;
			/**
				Lock on locks on to a target
			 */
			case CamStates.LockOn:

				//if there are enemies
				if (lockOnTarget != null){

                        //setup lookat
                    //lockOnLookAt = (characterOffset + lockOnTarget.transform.position) / 2.0f;
					lockOnLookAt = Vector3.Lerp(characterOffset, lockOnTarget.transform.position, lockOnFollowToTargetRatio);

					//float camCharTargetAngle = 180 - defaultYAngle;

					float targetDistance = new Vector3(lockOnTarget.transform.position.x - lookAt.x, 0 ,lockOnTarget.transform.position.z - lookAt.z).magnitude;

					if (targetDistance > lockOnCameraFullRotationMaxDistance){
						//farCam
						if (debugOn && debugLockOnCameraDistanceBehaviour)
						{
							Debug.Log("FarCamActive");
						}

						Vector3 floorAngle = lockOnLookAt;
						floorAngle.y = 0;
						Vector3 floorChar = lookAt;
						floorChar.y = 0;
						floorAngle -= floorChar;

                        // [0,180]
						float centerAngle = Vector3.Angle(Vector3.forward, floorAngle);

                        // [0,360]
						if(floorAngle.x < 0){
							centerAngle = 360 - centerAngle;
						}

                        // if center angle jumps 0->360 or 360->0
                        if (Mathf.Abs(xLockOn - centerAngle) > 180)
                        {
                            // eaqch round trip means that the CA and x get further away by a factor of 180
                            //Debug.Log("(x-ca) / 180 " + (Mathf.Abs(xLockOn - centerAngle) / 180));

                            // how many half round trips has the xlockon done
                            int rotationFactor = Mathf.FloorToInt(Mathf.Abs(xLockOn - centerAngle) / 180.0f);
                            // how many round trips (0verflow over 360) has the xlockon done
                            int overflowFactor = rotationFactor - Mathf.FloorToInt(rotationFactor / 2);
                            //Debug.Log("overflowFactor " + overflowFactor);

                            // if the center angle and xlockon are no longer in the same angle specter [0,360]
                            // adjust the center angle to overflow to xlockon range

                            if (xLockOn - centerAngle > 180)
                            {
                                float newAngle = overflowFactor * 360 - (xLockOn - centerAngle);
                                centerAngle = xLockOn + newAngle;
                                //Debug.Log("x-ca > 180");
                            }
                            else if (xLockOn - centerAngle < -180)
                            {
                                float newAngle = overflowFactor * 360 + (xLockOn - centerAngle);
                                centerAngle = xLockOn - newAngle;
                                //Debug.Log("x-ca < -180");
                            }
                        }
                            //Debug.Log(centerAngle);

						if (lockOnManualControl)
                        {
                            if (usingController)
                            {
                                //xlockon lerp to centerAngle
                                if (GetCameraAxis(rightStickX) == 0)
                                {
                                    //y released needs to return to default

                                    //if (y < yLerpSpeed+ defaultYAngle || Math.Abs(y) > defaultYAngle - yLerpSpeed)
                                    if (xLockOn < yLerpSpeed + centerAngle && xLockOn > centerAngle - yLerpSpeed)
                                    {
                                        xLockOn = centerAngle;
                                    }
                                    else
                                    {
                                        xLockOn = (xLockOn > centerAngle ? xLockOn - yLerpSpeed : xLockOn + yLerpSpeed);
                                    }
                                }
                                else
                                {
                                    xDelta += GetCameraAxis(rightStickX) * xSpeed / 2 * distance;
                                    xLockOn += GetCameraAxis(rightStickX) * xSpeed / 2 * distance;
                                }
                            }

                            if (usingMouse)
                            {
                                //float xLockOnDelta = GetCameraAxis(mouseInputX) * xSpeed/2 * distance;
                                float xDelta = GetCameraAxis(mouseInputX) * xSpeed / 2 * distance;
                                xLockOn = inverseXAxis ? xLockOn + xDelta : xLockOn - xDelta;
                            }
                        }
                        else
                            {
                                AutomaticLockOnUpdate();
                            }
						

						//calculate allowed angle range

						
						// calculating how the camera should behave in order to keep the player character a set number away from the cameras borders depends on the hFOV and aspect ratio
						// if(debugOn && debugCameraLockOnCalculations)
						// {
						// 	//Debug.Log("horizontalFOV " + hFOV);
						// 	//Debug.Log("camera aspect ratio " + cam.aspect );
						// }

						if(true &&lockOnManualControl)
						{
							float horizontal;
							float angleLimit;


							// projection of follow to cameraToLookAtVector
							Vector3 cameraToLockOnLookAt = lockOnLookAt - transform.position;
							Vector3 cameraToFollow = characterOffset - transform.position;

							//float alpha = Mathf.Cos(Vector3.Angle(cameraToFollow, cameraToLockOnLookAt) * Mathf.Deg2Rad);
							//float followScalarProjection = cameraToLockOnLookAt.magnitude > cameraToFollow.magnitude ? cameraToFollow.magnitude * alpha : cameraToLockOnLookAt.magnitude * alpha;

							Vector3 projectedCamToLockOn = new Vector3(cameraToLockOnLookAt.x, 0, cameraToLockOnLookAt.z);
							Vector3 projectedFollowToLookAt = characterOffset - lockOnLookAt;
							projectedFollowToLookAt.y = 0;
							// distance from the camera to the follow(player) when the camera is right behind their back in lock on (constant!)
							float projectedFollowDistance = projectedCamToLockOn.magnitude - projectedFollowToLookAt.magnitude;
							
							//horizontal length of the camera frustum at the distance of the follow(player)
							horizontal = Mathf.Tan(hFOV * Mathf.Deg2Rad *.5f) * cameraToFollow.magnitude * lockOnRotationRangePercent * 0.01f;
							//horizontal = Mathf.Tan(hFOV * Mathf.Deg2Rad *.5f) * followScalarProjection * lockOnRotationRangePercent * 0.01f;
							//angleLimit = Mathf.Atan(horizontal / (lockOnDistance-followScalarProjection)) * Mathf.Rad2Deg; //old lockondistance

							//the angle the camera can rotate in order for the follow to be within the lockOnRotationRangePercent screen percentage
							angleLimit = Mathf.Atan(horizontal / (lockOnDistance- projectedFollowDistance)) * Mathf.Rad2Deg; //old lockondistance

							angleLimit = Math.Abs(angleLimit);

							// smooth interpolation to angle limit 
							if (angleLimitCurrent > Mathf.Abs(angleLimit))
							{
								angleLimitCurrent -= farCamTransitionSpeed * Time.deltaTime;
							}

							if (angleLimitCurrent < Mathf.Abs(angleLimit))
							{
								angleLimitCurrent = angleLimit;
							}



							xLockOn = ClampAngle(xLockOn, centerAngle - angleLimitCurrent, centerAngle + angleLimitCurrent);

							if(debugOn && debugCameraLockOnCalculations)
							{
								//Debug.Log("lockOnRotationRangePercent " + lockOnRotationRangePercent * 0.01f);
								Debug.Log("horizontal " + horizontal);
								//Debug.Log("followScalarProjection " + followScalarProjection);
								Debug.Log("angle limit " + angleLimit);
								Debug.Log("angle limit current" + angleLimitCurrent);
								//Debug.DrawLine(transform.position, follow.position + lookoffset, Color.blue);
								//Debug.DrawLine(transform.position, lockOnLookAt, Color.red);

								//Debug.Log("xLockon " + xLockOn);
							}
							//y lockon
						}
						


					}else{
						//closeCam
						if (debugOn && debugLockOnCameraDistanceBehaviour)
						{
							Debug.Log("closeCamActive");
						}

                        if(lockOnManualControl)
                        {
                            if (usingController)
                            {
                                xDelta = GetCameraAxis(rightStickX) * xSpeed / 2 * distance;
                                xLockOn += InverseXAxis ? xDelta : -xDelta;
                            }
                            else if (usingMouse)
                            {
                                xDelta = GetCameraAxis(mouseInputX) * xSpeed / 2 * distance;
                                xLockOn += InverseXAxis ? xDelta : -xDelta;
                            }
                            angleLimitCurrent = angleLimitStart;
                        }
                        else
                            {
                                AutomaticLockOnUpdate();
                            }
						

                    }

					//if(debugOn && debugCameraLockOnCalculations)
					//{
					//		Debug.Log("xLockon " + xLockOn);
					//}


					// angle between floor and char to target
					if(debugOn && debugLockOn)
					{
						Debug.DrawLine(transform.position, lockOnLookAt, Color.red);
						Debug.DrawLine(characterOffset, lockOnLookAt, Color.blue);

						Debug.DrawLine(lockOnLookAt, lockOnTarget.transform.position, Color.green);
						Debug.DrawLine(characterOffset, transform.position, Color.yellow);
					}


                        //float charTotarget = Vector3.Distance(characterOffset, lockOnLookAt);

                    if (experimentalTurnOffAutomaticDistanceCalculation)
                    {
                        //lockOnDistance = distance + (characterOffset - lockOnLookAt).magnitude;

                        MoveCamera(xLockOn, yLockOn, distance, lookAt, false);

                        if(experimentalLockOnDisingageOnSteepAngle && CheckIfSteepAngle(lockOnLookAt))
                        {
                            lockOnTarget = null;
                            return;

                        }

                        smoothLookAt = SmoothLookingAt(smoothLookAt, lockOnLookAt, lookSpeed);

                        transform.LookAt(smoothLookAt);
                    }
                    else
                    {


                        Vector3 charToLookVec = lockOnLookAt - characterOffset;


                        Vector3 charToTargetAngle = charToLookVec;
                        charToTargetAngle.y = 0;

                        Vector3 crossCTT = Vector3.Cross(charToTargetAngle, charToLookVec);
                        Vector3 posAngl = Quaternion.Euler(0, +90, 0) * charToTargetAngle;

                        //float charLockOnAngle = Vector3.Angle(charToTargetAngle, charToLookVec);

                        // these rays are at world origin, for debugging angles and cross products
                        if (debugOn && debugLockOn)
                        {
                            //Vector3 origin = Vector3.zero;
                            Debug.Log(Vector3.Angle(charToTargetAngle, lockOnLookAt - characterOffset));
                            Debug.DrawRay(Vector3.zero, lockOnLookAt - characterOffset, Color.cyan);
                            Debug.DrawRay(Vector3.zero, charToTargetAngle, Color.red);
                            Debug.DrawRay(Vector3.zero, crossCTT, Color.green);
                            Debug.DrawRay(Vector3.zero, posAngl, Color.magenta);
                        }

                        // using virtual Free cam position

                        Vector3 charToTargetProject = lockOnLookAt - characterOffset;
                        charToTargetProject.y = 0;

                        Vector3 camFreePos = characterOffset + (Quaternion.AngleAxis(defaultYAngle, Quaternion.AngleAxis(90, follow.up) * charToTargetProject) * (-charToTargetProject.normalized * distance));
                        Vector3 lookAtToThirdPersonCam = camFreePos - lockOnLookAt;


                        if (debugOn && debugCameraVerticalMovement)
                        {
                            Debug.Log("playerJumping " + playerJumping);
                            Debug.Log("stopCameraFollowingYWhenPlayerIsJumping " + stopCameraFollowingYWhenPlayerIsJumping);
                        }


                        if (!stopCameraFollowingYWhenPlayerIsJumping || playerJumping == false)
                        {
                            Vector3 crossFreexFloor = Vector3.Cross(lookAtToThirdPersonCam, charToTargetProject);
                            Vector3 negativeAngle = Quaternion.Euler(0, -90, 0) * charToTargetProject.normalized; //lookingUp

                            if (debugOn && debugCameraVerticalMovement)
                            {
                                Debug.Log("lookAtToThirdPersonCam, -charToTargetProject angle " + Vector3.Angle(lookAtToThirdPersonCam, -charToTargetProject)); //yLockOn positive
                                Debug.DrawLine(Vector3.zero, lookAtToThirdPersonCam, Color.red);
                                Debug.DrawLine(Vector3.zero, -charToTargetProject, Color.blue);
                                Debug.DrawLine(Vector3.zero, crossFreexFloor, Color.magenta);
                                Debug.DrawLine(Vector3.zero, negativeAngle, Color.green);
                            }




                            yLockOn = Vector3.Angle(lookAtToThirdPersonCam, -charToTargetProject);
                            if (Mathf.Sign(crossFreexFloor.x) == Mathf.Sign(negativeAngle.x))
                            {
                                yLockOn = 0 - yLockOn;
                            }


                        }

                        Vector3 ThirdPersonCamToChar = characterOffset - camFreePos;

                        Vector3 targetToChar = lockOnLookAt - characterOffset;

                        Vector3 crossFCTC = Vector3.Cross(ThirdPersonCamToChar, targetToChar);
                        Vector3 upTriangle = Quaternion.Euler(0, -90, 0) * charToTargetProject.normalized;

                        lockOnDistance = FOVLockOnDistance(Mathf.Sign(crossFCTC.x) == Mathf.Sign(upTriangle.x));


                        MoveCamera(xLockOn, yLockOn, lockOnDistance, lockOnLookAt, false);

                        smoothLookAt = SmoothLookingAt(smoothLookAt, lockOnLookAt, lookSpeed);

                        transform.LookAt(smoothLookAt);
                        }


                    //lock on graphic
                    if (lockOnReticle != null)
                    {
                        Vector2 lockOnPos = Camera.main.WorldToViewportPoint(lockOnTarget.transform.position);
                        lockOnReticle.anchorMin = lockOnPos;
                        lockOnReticle.anchorMax = lockOnPos;

                        lockOnReticle.sizeDelta = Vector2.Lerp(lockOnReticle.sizeDelta, new Vector2(50f, 50f), lockonAnimSpeed * Time.deltaTime);

                        // turn off the reticle when in cooldown
                        if (LNcooldown)
                        {
                            lockOnReticle.sizeDelta = Vector2.zero;
                        }
                    }


                    } else{
					//no emenies free cam
					goto case CamStates.ThirdPersonCam;
				}
				break;


			}

			camFadeObjects (this.transform.position);
			camFadeFollow();

		}

        bool CheckIfSteepAngle(Vector3 target)
        {
            //check if the angles are too steep
            Vector3 lookAtDir = (transform.position - target).normalized;
            Vector3 floorDir = lookAtDir;
            floorDir.y = 0;
            float angleOfView = Vector3.Angle(lookAtDir, floorDir);

            //angle of view is always positive
            // the y of the lookAtDir tells us if our angle is positive or negative
            if(lookAtDir.y < 0)
            {
                angleOfView *= -1f;
            }

            if (angleOfView > experimentalLockOnDisingageMaxAngle || angleOfView < experimentalLockOnDisingageMinAngle)
            {
                return true;
            }

            return false;
        }

        private void AutomaticLockOnUpdate()
        {
            // the camera is positioned at the characters back
            Vector3 floorDirection = follow.position - lockOnLookAt;
            floorDirection.y = 0;
            xLockOn = Vector3.Angle(-Vector3.forward, floorDirection);
            if (floorDirection.x > 0)
            {
                xLockOn = 360 - xLockOn;
            }

            //y angle matches the angle between the characteroffset and target
            //Vector3 charToLookAt = (characterOffset - lockOnLookAt).normalized;
            //yLockOn = Vector3.Angle(floorDirection, charToLookAt);
            //if(charToLookAt.y < 0)
            //{
            //    yLockOn *= -1f;
            //}

            yLockOn = defaultYAngle;
        }

		

		// calculates camera distance from lookAt in Lock on mode
		private float FOVLockOnDistance (bool upsideTri){
			//using angles and vectors



			Vector3 charToTargetProject = lockOnLookAt - characterOffset;
			charToTargetProject.y = 0;
			// the position the free cam would be in, (projection is rotated so that we get the right vector of the char, and then rotated defaultYangle upwards and multiplied by distance.
			Vector3 camFreePos = characterOffset + (Quaternion.AngleAxis(defaultYAngle,Quaternion.AngleAxis(90, follow.up) * charToTargetProject) * (- charToTargetProject.normalized * distance));
			// the base lock on distance, from free cam to target
			Vector3 camToLookAt = lockOnLookAt - camFreePos;

			Vector3 floorToTarget = (lockOnLookAt - follow.transform.position);
			float camfloorToTargetAngle;
			Vector3 floorExtend;



			if (upsideTri) {

				if(debugOn && debugCameraLookingUpDown)
				{
					Debug.Log("Camera Looking Up");
				}


				Vector3 floorToTargetfloor = floorToTarget;
				floorToTargetfloor.y = 0;
				float floorToTargetAngle = Vector3.Angle (floorToTarget, floorToTargetfloor);


				camfloorToTargetAngle = 180 - floorToTargetAngle;


				floorExtend = follow.transform.position + (-charToTargetProject.normalized * lockOnScreenBottomMargin);

			} else {

				if(debugOn && debugCameraLookingUpDown)
				{
					Debug.Log("Camera Looking Down");
				}


				floorExtend = characterOffset + (Vector3.up * lockOnScreenBottomMargin);

				camfloorToTargetAngle = Vector3.Angle(Vector3.up, floorToTarget);

			}

			Vector3 ThirdPersonCamToChar = characterOffset - camFreePos;

			Vector3 ThirdPersonCamToFloorOffset = floorExtend - camFreePos;

			float ThirdPersonCamLookAtAngle = Vector3.Angle (ThirdPersonCamToChar, camToLookAt);

			float ThirdPersonCamToFloorOffsetAngle = Vector3.Angle (ThirdPersonCamToFloorOffset, ThirdPersonCamToChar);

			float beta = 180 - ThirdPersonCamLookAtAngle - ThirdPersonCamToFloorOffsetAngle;

			if(debugOn && debugCameraLookingUpDown)
			{
				Debug.Log ("ThirdPersonCamLookAtAngle " + ThirdPersonCamLookAtAngle);
				Debug.Log ("ThirdPersonCamToFloorOffsetAngle " + ThirdPersonCamToFloorOffsetAngle);
				Debug.DrawLine(characterOffset, camFreePos, Color.green);
				Debug.DrawLine(characterOffset, follow.position, Color.blue);
			}

			//sinus

			float x = Mathf.Sin (beta * Mathf.Deg2Rad) * ThirdPersonCamToFloorOffset.magnitude / Mathf.Sin (cam.fieldOfView/2 * Mathf.Deg2Rad);

			//cos

			float floorSide = x + lockOnScreenBottomMargin;
			if (!upsideTri) {
				floorSide += Vector3.Magnitude(characterOffset-follow.transform.position);
			}


			float fovDist = Mathf.Sqrt(floorSide * floorSide + floorToTarget.magnitude*floorToTarget.magnitude - 2 * floorToTarget.magnitude * floorSide * Mathf.Cos(camfloorToTargetAngle* Mathf.Deg2Rad) );

			if (camToLookAt.magnitude > fovDist)
				return camToLookAt.magnitude;


			return fovDist;


		}

		private Vector3 SmoothLookingAt(Vector3 smoothLookAt, Vector3 lockOnLookAt, float lookSpeed){
			if (!CompareVectors(smoothLookAt, lockOnLookAt, 1f))
						smoothLookAt = Vector3.Lerp(smoothLookAt, lockOnLookAt, lookSpeed * Time.deltaTime);
			else smoothLookAt = lockOnLookAt;
			return smoothLookAt;
		}

		private void SmoothPosition(Vector3 fromPos, Vector3 toPos, float speed){

			this.transform.position = Vector3.SmoothDamp (fromPos, toPos, ref velocityCamSmooth, speed /* *  Time.deltaTime */);

		}

		private void MoveCamera(float x, float y, float distance, Vector3 lookAt, bool collisionYlock = false){

			//xprev = x;
			//yprev = y;
			//Debug.Log("smooth position");


			Quaternion rotation = Quaternion.Euler(y, x, 0);

			Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);

			Vector3 position = rotation * negDistance + lookAt; //rotate negdistance by rot around 000 and then translate in look target direction

			targetPosition = position;

			bool hitSomething = BoxCastWallCollision(characterOffset, ref targetPosition, collisionYlock);

			if(collisionYlock && hitSomething)
			{
				BoxCastWallCollision(characterOffset, ref targetPosition, false);
			}

			//SmoothPosition(this.transform.position, targetPosition, hitSomething ? camClippingSmoothDampTime : camSmoothDampTime);
			float smoothSpeed = hitSomething ? camClippingSmoothDampTime : camSmoothDampTime;

			Vector3 newPosition = Vector3.SmoothDamp (this.transform.position, targetPosition, ref velocityCamSmooth, smoothSpeed );

			if(debugOn && debugThirdPersonMode)
			{
				//Debug.DrawLine(lookAt, currentRot * negDistance + lookAt, Color.magenta);
				Debug.DrawLine(newPosition, lookAt, Color.green);
				//Debug.DrawLine(targetPosition, lookAt, Color.grey);
			}

			// negDistance = new Vector3(0.0f, 0.0f, -(newPosition-lookAt).magnitude);
			// Quaternion collisionRot = Quaternion.LookRotation(lookAt - newPosition);

			// yprev = collisionRot.eulerAngles.x;

			// if(yprev > 180.0f)
			// 	yprev = yprev - 360;
			// float xnew = collisionRot.eulerAngles.y;
			// xprev = CalculateAngleByPhaseOfX(xprev, xnew);

			this.transform.position = newPosition;

		}

		private int CalculatePhaseOfAngle(float angle, float shift = 180.0f)
		{
			float xPeriod = (angle+shift)/360;

			// determine whether the current x is on the same period
			xPeriod = xPeriod < 0 ? xPeriod -1 : xPeriod; 
			xPeriod = (int)(xPeriod);
			return (int)xPeriod;
		}


		// increases the xCurrent angle by phase of angle X (in the nearest range of)
		private float CalculateAngleByPhaseOfX(float x, float xCurrent, float angleOffset = 0.0f)
		{

			int xPeriod = CalculatePhaseOfAngle(x);
			float xNew = xCurrent + 360 * xPeriod;
			xNew += angleOffset;

			//x and xcur differecne should be <180
			if(x - xNew > 180)
			{
				xNew += 360;
			}
			else if(x - xNew < -180)
			{
				xNew -= 360;
			}

			//are the x and new in the same phase?
			return xNew;

		}


		// phase shifts x so that its inbetween xprev and xnext
		private float CalculateAngleInBetweenAngles(float xprev, float xnext, float x)
		{
			
			// if(xprev > xnext)
			// {
			// 	float tmp = xnext;
			// 	xnext = xprev;
			// 	xprev = tmp;
			// }


			float phaseXprev = CalculatePhaseOfAngle(xprev, 0.0f);
			float phaseXnext = CalculatePhaseOfAngle(xnext, 0.0f);

			

			float xPhasePrev = x + 360 * phaseXprev;
			float xPhaseNext = x + 360 * phaseXnext;

			// if(true && xprev != xnext)
			// {
			// 	Debug.Log("x prev " + xprev + " phase " + phaseXprev);
			// 	Debug.Log("x next " + xnext + " phase " + phaseXnext);
			// 	Debug.Log("x  " + x + " phaseprev " + xPhasePrev + " phasenext " + xPhaseNext );
			// }

			//Debug.Log((xprev <= xPhaseNext && xPhaseNext <= xnext));
			//Debug.Log((xnext <= xPhaseNext && xPhaseNext <= xprev));
			//Debug.Log(xnext == xprev);
			//Debug.Log(FastApproximately(xnext, xPhaseNext, 0.1f));
			// if(!FastApproximately(xprev, xPhasePrev, 100f))
			// {
			// 	Debug.Log("x prev " + xprev + " phase " + phaseXprev);
			// 	Debug.Log("x next " + xnext + " phase " + phaseXnext);
			// 	Debug.Log("x  " + x + " phaseprev " + xPhasePrev + " phasenext " + xPhaseNext );
			// }
			

			if(phaseXnext == phaseXprev)
				if( (xprev <= xPhaseNext && xPhaseNext <= xnext) || (xnext <= xPhaseNext && xPhaseNext <= xprev))
					return xPhaseNext;
			
			// handle situations where phases are different of xprev and xnext

			if(FastApproximately(xprev, xPhasePrev, 1f) || FastApproximately(xnext, xPhasePrev, 1f))
				return xPhasePrev;
			
			if(FastApproximately(xprev, xPhaseNext, 1f) || FastApproximately(xnext, xPhaseNext, 1f))
				return xPhaseNext;
			

			if((xprev <= xPhasePrev && xPhasePrev <= xnext) || (xnext <= xPhasePrev && xPhasePrev <= xprev))
			{
				//Debug.Log("firstPhase");
				return xPhasePrev;
			}

			if((xprev <= xPhaseNext && xPhaseNext <= xnext) || (xnext <= xPhaseNext && xPhaseNext <= xprev))
			{
				//Debug.Log("secondPhase");
				return xPhaseNext;
			}

			// cases when the angle is outside the xprev xnext range
			if((xprev <= xnext && xnext <= xPhasePrev) || (xnext <= xprev && xPhasePrev <= xnext))
			{
				//Debug.Log("outsideprev");
				return xPhasePrev;
			}

			if((xprev <= xnext && xPhaseNext <= xprev) || (xnext <= xprev && xprev <= xPhaseNext))
			{
				//Debug.Log("outsidenext");
				return xPhaseNext;
			}


			//Debug.Log((xprev <= xPhasePrev && xPhasePrev <= xnext) || (xnext <= xPhasePrev && xPhasePrev <= xprev));
			//Debug.Log((xprev <= xPhaseNext && xPhaseNext <= xnext) || (xnext <= xPhaseNext && xPhaseNext <= xprev));

			//Debug.LogWarning("x phase cannot be determined: " + " xprev " + xprev + " x " + x + " xnext " + xnext);
			//Debug.LogWarning("x  " + x + " phaseprev " + xPhasePrev + " phasenext " + xPhaseNext );


			if(FastSubstractAbs(xprev, xPhasePrev) < FastSubstractAbs(xnext, xPhaseNext))
				return xPhasePrev;
			else
				return xPhaseNext;

			// if(xPhaseNext >= xnext)
			// 	return xPhaseNext;

			// if(xPhasePrev <= xprev )
			// 	return xPhasePrev;

			//Debug.LogWarning("critical third case");



			//return xPhasePrev;
		}

		private void camFadeObjects(Vector3 toTarget){

			if(cameraFadeObjects == false)
			{
				return;
			}

			RaycastHit[] hits;
			Vector3 start = toTarget + transform.forward * (cam.nearClipPlane - fadeObjectsSpherecastRadius);
			Vector3 direction = characterOffset - toTarget;

			hits = Physics.SphereCastAll (start, fadeObjectsSpherecastRadius, direction, direction.magnitude + fadeObjectsSpherecastRadius, cameraFadeLayer);

			for (int i = 0; i < hits.Length; i++){

				Renderer rend = hits[i].transform.GetComponent<Renderer>();
				if(rend){

					float alpha = 0f;

					if (!alwaysFullyTransparent && hits[i].distance > fullTransparencyDistance)
						alpha = Mathf.Clamp01((hits[i].distance - fullTransparencyDistance)/ (direction.magnitude - fullTransparencyDistance));




					// get or create the script FadeObject on the object.
					FadeObject fo = rend.GetComponent<FadeObject>();
					if(fo == null)
						fo = rend.gameObject.AddComponent<FadeObject>();

					fo.FadeOutSpeed = fadeOutSpeed;
					fo.FadeInSpeed = fadeInSpeed;
					fo.FadeShader = fadeShader;
					//Set the transparencym script handles fading
					fo.SetTransparency(alpha);

					if (debugOn && debugCamFade)
					{
						Debug.Log("hits[i].distance " + hits[i].distance);
						Debug.Log("alpha " + alpha);
						fo.DebugOn = true;
					}

				}
			}
		}

		private void camFadeFollow()
		{
			if(fadePlayerWhenClose && (this.transform.position - characterOffset).magnitude < playerFadeDistance)
			{
				Renderer[] rends = follow.gameObject.GetComponentsInChildren<Renderer>();
				for (int i = 0; i < rends.Length; i++)
				{
					Renderer rend = rends[i];
					if(rend)
					{
						float alpha = 0f;
						FadeObject fo = rend.GetComponent<FadeObject>();
						if(fo == null)
							fo = rend.gameObject.AddComponent<FadeObject>();

						fo.FadeOutSpeed = fadeOutSpeed;
						fo.FadeInSpeed = fadeInSpeed;
						fo.FadeShader = fadeShader;

						fo.SetTransparency(alpha);
					}
				}
				
			}
		}

		private bool BoxCastWallCollision(Vector3 followPosition, ref Vector3 cameraPosition, bool lockY = false)
		{
			bool hitSomething = false;

			if(usingMouse)
			{
				lockY = false;
			}

			Vector3 dir = cameraPosition - followPosition;
			dir += dir*0.1f;
			Quaternion dirQuat = Quaternion.LookRotation(dir, Vector3.up);


			float camHorizontal = Mathf.Tan(hFOV * Mathf.Deg2Rad *0.5f) * (cam.nearClipPlane); //* 0.5 * 2
			float camVertical = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad *0.5f) * (cam.nearClipPlane);

			Vector3 boxExtends = new Vector3(camHorizontal + collisionMargin , camVertical + collisionMargin, 0.02f);
			Vector3 boxOrigin = followPosition + dir.normalized*0.1f;

			//Vector3 camNearPlaneCenter = cameraPosition + dir*cam.nearClipPlane;
			// the origin of the cast is infront of the camera casting towards the camera
			float radius = camHorizontal > camVertical ? camHorizontal : camVertical;

			RaycastHit[] hits;

			hits = Physics.BoxCastAll(boxOrigin, boxExtends, dir.normalized, dirQuat, dir.magnitude, camObstacle);
			Array.Sort(hits, rayHitComparer);

			float nearest = Mathf.Infinity;
			int iHit = 0;

			for (int i = 0; i < hits.Length; i++)
			{
				// only deal with the collision if it was closer than the previous one, not a trigger, and not attached to a rigidbody tagged with the dontClipTag
				if (hits[i].distance < nearest && (!hits[i].collider.isTrigger) &&
					(hits[i].collider == null || hits[i].collider != follow.GetComponent<Collider>()) && !hits[i].point.Equals(Vector3.zero))
				{
					// change the nearest collision to latest
					nearest = hits[i].distance;
					iHit = i;
					hitSomething = true;
				}

				if(debugOn && debugCollision)
				{
					// vizualize all the hits
					Debug.DrawLine(followPosition, hits[i].point, Color.magenta);
				}
			}

			if(hitSomething)
			{
				Vector3 hitVector = hits[iHit].point - followPosition;
				Vector3 hitProjection = Vector3.Project(hitVector, dir.normalized);
				Vector3 hitPointProjection = followPosition + hitProjection;

				if(debugOn && debugCollision)
				{
					Debug.DrawLine(followPosition, hitPointProjection, Color.blue);
				}

				Vector3 direction = hits[iHit].normal * radius;

				// finally adjust new camera position infront of the hit
				//cameraPosition = followPosition + dir.normalized * hitProjection.magnitude;
				hitSomething = true;

				//check the normal of the surface we hit
				//Debug.Log(hits[iHit].normal.y);

				// y cannot be kept if the collision surface is a ceiling
				if(lockY == true && hits[iHit].normal.y < -0.01f)
					lockY = false;
				

				// keeps the Y from moving up, used for lockon to keep the look at in view
				if(!lockY)
				{
					//cameraPosition = new Vector3(hitPointProjection.x - direction.x, hitPointProjection.y, hitPointProjection.z - direction.z);
					cameraPosition = hitPointProjection + direction;
				}
				else
				{
					if(direction.y <= 0.01f)
						cameraPosition = new Vector3(hitPointProjection.x + direction.x, cameraPosition.y, hitPointProjection.z + direction.z);
					else
						cameraPosition = hitPointProjection + direction;
				}
			}
			
			
			if(debugOn && debugCollision)
			{
				Color drawcol = hitSomething ? Color.red : Color.green;
				ExtDebug.DrawBoxCastBox(boxOrigin, boxExtends, dirQuat, dir.normalized, dir.magnitude, drawcol);
			}


			return hitSomething;
		}

		public class RayHitComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				return ((RaycastHit) x).distance.CompareTo(((RaycastHit) y).distance);
			}
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			angle= Mathf.Clamp(angle, min, max);

			if (angle < -360F)
				angle += 360F;
			if (angle > 360F)
				angle -= 360F;

			return angle;
		}


		public void NewTarget (GameObject target){
			lockOnTarget = target;
		}

		public void NoTarget (){
			lockOnTarget = null;
		}


		IEnumerator LockOnCooldown(){
			float t = 0;
			while (t < lockOnCoolDownTime){
				LNcooldown = true;
				t += Time.deltaTime;
				yield return 0;
			}


            GetXYFromXYLockOn();


            LNcooldown = false;
		}

        void GetXYFromXYLockOn()
        {
            y = defaultYAngle;

            //get the x relative to xLockon
            Vector3 floorCharPos = transform.position - lookAt;
            floorCharPos.y = 0;

            x = Vector3.Angle(floorCharPos, -Vector3.forward);

            if (floorCharPos.x > 0)
            {
                x = 360 - x;
            }
        }

		bool CompareVectors(Vector3 a,Vector3 b, float angleError){
			//if they aren't the same length, don't bother checking the rest.
			if(!Mathf.Approximately(a.magnitude, b.magnitude))
				return false;
			float cosAngleError = Mathf.Cos(angleError * Mathf.Deg2Rad);
			//A value between -1 and 1 corresponding to the angle.
			float cosAngle = Vector3.Dot(a.normalized, b.normalized);
			//The dot product of normalized Vectors is equal to the cosine of the angle between them.
			//So the closer they are, the closer the value will be to 1.  Opposite Vectors will be -1
			//and orthogonal Vectors will be 0.

			if(cosAngle >= cosAngleError) {
				//If angle is greater, that means that the angle between the two vectors is less than the error allowed.
				return false;
			}
			else
				return true;
		}

		// public void SetInCamBounds(bool set){
		// 	inCamBounds = set;
		// }


		public Vector3 getLookAt(){
			return smoothLookAt;
		}

		public void toggleInput()
		{
			if (usingController)
			{
				usingController = false;
				usingMouse = true;
			}
			else
			{
				usingController = true;
				usingMouse = false;
			}
			return;
		}
		//Call this function to tell the camera is the player jumping or not, necessary for the stopCameraFollowingYWhenPlayerIsJumping functionallity to work
		public void UpdateJumpingStatus(bool isJumping)
		{
			playerJumping = isJumping;
		}

        // Initiate lock on via script, this disables player lock on control
        public void InitiateLockOn(GameObject lockOnObject)
        {
            lockOnCurrent = -1;
            lockOnTargets = SortLockOns();
            enableLockOn = false;

            lockOnTarget = lockOnObject;
            camstate = CamStates.LockOn;

            InitiateLockOn_Internal();
        }

        private void InitiateLockOn_Internal()
        {
            if (lockOnTarget != null)
            {
                // used to be checking for length of lockontargets
                StopCoroutine("LockOnCooldown");
                LNcooldown = false;
                lockOnLookAt = Vector3.Lerp(characterOffset, lockOnTarget.transform.position, lockOnFollowToTargetRatio);

                //yLockon - angle distance and floor distance
                //xLockon - angle floordistance and floor forward
                if (lockOnManualControl)
                {
                    Vector3 floorDistance = transform.position - lockOnLookAt;
                    floorDistance.y = 0; //floor distance vector


                    xLockOn = Vector3.Angle(-Vector3.forward, floorDistance);
                    if (floorDistance.x > 0)
                    {
                        xLockOn = 360 - xLockOn;
                    }

                    //rotate by phase of x
                    xLockOn = CalculateAngleByPhaseOfX(x, xLockOn);
                }
                else
                {
                    AutomaticLockOnUpdate();
                }


                angleLimitCurrent = angleLimitStart;
            }
        }

        //exit lock on via script, enable player lock on command
        public void ExitLockOn(bool enablePlayerLockOnCommand = true, bool useLockOnCooldown = false)
        {
            camstate = CamStates.ThirdPersonCam;
            lockOnCurrent = -1;

            if (lockOnToggle)
            {
                lockOnTogglePressed = true;
                lockOnToggleEngaged = false;
            }
            else if(useLockOnCooldown)
            {
                StartCoroutine("LockOnCooldown");
            }
            enableLockOn = enablePlayerLockOnCommand;
        }


		//sorts lockOns by distance from follow
		private GameObject[] SortLockOns(){

			lockOnTargets = GameObject.FindGameObjectsWithTag (lockOnTargetsTag);

			if(lockOnTargets == null)
			{
				Debug.LogError("Lock On targets NULL");
				return null;
			}

			if(lockOnTargets.Length == 0)
				return lockOnTargets;


			// if distance limit is on remove the targets that are out of range
			// if (lockOnDistanceLimit > 0)
			// {
			// 	List<GameObject> lockOnsTemp = new List<GameObject>(lockOnTargets);
			// 	for (int i = lockOnsTemp.Count-1; i >= 0; i-- )
			// 	{
			// 		if (Vector3.Distance(lockOnsTemp[i].transform.position, follow.position ) > lockOnDistanceLimit)
			// 		{
			// 			lockOnsTemp.RemoveAt(i);
			// 		}
			// 	}
			// 	lockOnTargets = lockOnsTemp.ToArray();
			// }


			lockOnTargets = lockOnTargets.OrderBy(t => Vector3.Distance(t.transform.position, follow.position + lookoffset )).ToArray();


			return lockOnTargets;
		}

		private bool PreviousTarget()
		{
			return ChangeTarget(false);
		}

		private bool ChangeTarget(bool next = true)
		{
			bool targetLegal = false;
			int i = 0;
			while(!targetLegal)
			{	
				if(next)
					lockOnCurrent = (lockOnCurrent+1) % (lockOnTargets.Length);
				else
					lockOnCurrent = (lockOnCurrent-1) % (lockOnTargets.Length);

				if(lockOnCurrent < 0)
					lockOnCurrent += lockOnTargets.Length;


				lockOnTarget = lockOnTargets[lockOnCurrent];
				i++;

				// check all the targets including starting
				if(i == lockOnTargets.Length + 1)
				{
					break;
				}

				targetLegal = CheckTargetLegal();

				//Debug.Log(lockOnCurrent +  " " + targetLegal);
			}

			if(!targetLegal)
			{
				lockOnTarget = null;
				lockOnCurrent = -1;
			}

			return targetLegal;
		}

		private bool CheckTargetLegal()
		{
			if (lockOnDistanceLimit > 0)
			{
				if (Vector3.Distance(lockOnTarget.transform.position, follow.position ) > lockOnDistanceLimit)
				{
					return false;
				}
			}

            if(experimentalLockOnDisingageOnSteepAngle && CheckIfSteepAngle(lockOnTarget.transform.position))
            {

                // check if target is steep
                return false;
            }

			if(disallowLockingOnToTargetBehindWall)
			{
				Vector3 dir = lockOnTarget.transform.position - transform.position; 
				Ray lockOnRay = new Ray();
				lockOnRay.direction = dir.normalized;
				lockOnRay.origin = transform.position;
				RaycastHit[] hits;
				hits = Physics.RaycastAll(lockOnRay, dir.magnitude, camObstacle);

				//get the collider of the target
				Collider collider = lockOnTarget.GetComponent<Collider>(); 
				if(collider == null)
				{
					collider = lockOnTarget.transform.root.GetComponentInChildren<Collider>();
				}

				bool hitSomething = false;
				for (int i = 0; i < hits.Length; i++)
				{
					// only deal with the collision if it was closer than the previous one, not a trigger, and not attached to a rigidbody tagged with the dontClipTag
					if ((!hits[i].collider.isTrigger) &&
						(hits[i].collider == null || hits[i].collider != follow.GetComponent<Collider>()) && hits[i].collider != collider)
					{
						//Debug.Log("hit " + hits[i].collider + " collider " + collider);
						hitSomething = true;
						break;
					}

				}
				
				if(debugOn && debugCollision)
				{
					// vizualize all the hits
					//Debug.DrawLine(transform.position, lockOnTarget.transform.position, (hitSomething) ? Color.red : Color.green, 2f);
					if(hitSomething)
					{
						Debug.DrawLine(transform.position, lockOnTarget.transform.position, Color.red, 2f);
						Debug.Log("hit " + collider);
					}
				}

				if(hitSomething ==  true)
				{
					return false;
				}
			}
			return true;
		}

		 public static class ExtDebug
		{
			//Draws just the box at where it is currently hitting.
			public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color)
			{
				origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
				DrawBox(origin, halfExtents, orientation, color);
			}

			//Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
			public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color)
			{
				direction.Normalize();
				Box bottomBox = new Box(origin, halfExtents, orientation);
				Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

				Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft,    color);
				Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
				Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
				Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight,    color);
				Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft,    color);
				Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
				Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
				Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight,    color);

				DrawBox(bottomBox, color);
				DrawBox(topBox, color);
			}

			public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color)
			{
				DrawBox(new Box(origin, halfExtents, orientation), color);
			}
			public static void DrawBox(Box box, Color color)
			{
				Debug.DrawLine(box.frontTopLeft,     box.frontTopRight,    color);
				Debug.DrawLine(box.frontTopRight,     box.frontBottomRight, color);
				Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color);
				Debug.DrawLine(box.frontBottomLeft,     box.frontTopLeft, color);

				Debug.DrawLine(box.backTopLeft,         box.backTopRight, color);
				Debug.DrawLine(box.backTopRight,     box.backBottomRight, color);
				Debug.DrawLine(box.backBottomRight,     box.backBottomLeft, color);
				Debug.DrawLine(box.backBottomLeft,     box.backTopLeft, color);

				Debug.DrawLine(box.frontTopLeft,     box.backTopLeft, color);
				Debug.DrawLine(box.frontTopRight,     box.backTopRight, color);
				Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color);
				Debug.DrawLine(box.frontBottomLeft,     box.backBottomLeft, color);
			}

			public struct Box
			{
				public Vector3 localFrontTopLeft     {get; private set;}
				public Vector3 localFrontTopRight    {get; private set;}
				public Vector3 localFrontBottomLeft  {get; private set;}
				public Vector3 localFrontBottomRight {get; private set;}
				public Vector3 localBackTopLeft      {get {return -localFrontBottomRight;}}
				public Vector3 localBackTopRight     {get {return -localFrontBottomLeft;}}
				public Vector3 localBackBottomLeft   {get {return -localFrontTopRight;}}
				public Vector3 localBackBottomRight  {get {return -localFrontTopLeft;}}

				public Vector3 frontTopLeft     {get {return localFrontTopLeft + origin;}}
				public Vector3 frontTopRight    {get {return localFrontTopRight + origin;}}
				public Vector3 frontBottomLeft  {get {return localFrontBottomLeft + origin;}}
				public Vector3 frontBottomRight {get {return localFrontBottomRight + origin;}}
				public Vector3 backTopLeft      {get {return localBackTopLeft + origin;}}
				public Vector3 backTopRight     {get {return localBackTopRight + origin;}}
				public Vector3 backBottomLeft   {get {return localBackBottomLeft + origin;}}
				public Vector3 backBottomRight  {get {return localBackBottomRight + origin;}}

				public Vector3 origin {get; private set;}

				public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
				{
					Rotate(orientation);
				}
				public Box(Vector3 origin, Vector3 halfExtents)
				{
					this.localFrontTopLeft     = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
					this.localFrontTopRight    = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
					this.localFrontBottomLeft  = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
					this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

					this.origin = origin;
				}


				public void Rotate(Quaternion orientation)
				{
					localFrontTopLeft     = RotatePointAroundPivot(localFrontTopLeft    , Vector3.zero, orientation);
					localFrontTopRight    = RotatePointAroundPivot(localFrontTopRight   , Vector3.zero, orientation);
					localFrontBottomLeft  = RotatePointAroundPivot(localFrontBottomLeft , Vector3.zero, orientation);
					localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
				}
			}

			//This should work for all cast types
			static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
			{
				return origin + (direction.normalized * hitInfoDistance);
			}

			static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
			{
				Vector3 direction = point - pivot;
				return pivot + rotation * direction;
			}
		}

		public static bool FastApproximately(float a, float b, float threshold)
		{
			return ((a < b)?(b - a):(a - b)) <= threshold;
		}

		public static float FastSubstractAbs(float a, float b)
		{
			return ((a < b)?(b - a):(a - b));
		}

	}
}