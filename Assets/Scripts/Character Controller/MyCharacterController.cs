using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController
{
    public enum CharacterState
    {
        Default,
        Charging,
        NoClip,
        Swimming,
        Climbing,
    }

    public enum ClimbingState
    {
        Anchoring,
        Climbing,
        DeAnchoring
    }

    public enum OrientationMethod
    {
        TowardsCamera,
        TowardsMovement,
    }

    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public bool JumpHeld;
        public bool CrouchDown;
        public bool CrouchUp;
        public bool ChargingDown;
        public bool CrouchHeld;
        public bool NoClipDown;
        public bool ClimbLadder;
    }

    public struct AICharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public enum BonusOrientationMethod
    {
        None,
        TowardsGravity,
        TowardsGroundSlopeAndGravity,
    }

    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")]
        public float MaxStableMoveSpeed = 10f;
        public float MinStableMoveSpeed = 4f;
        public float AccelerationSpeed = 0f;
        public float AccelerationRate = 1f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;
        // public float MaxSlopeRunEnergy = 4f;  // DELETE?
        // public float SlopeRunEnergy = 4f;
        // public float SlideSpeed = 5f;
        // public float SlideAngle = -0.2f;
        public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 15f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public bool AllowDoubleJump = false;
        public bool AllowWallJump = false;
        public float JumpUpSpeed = 10f;
        public float JumpScalableForwardSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0f;
        public float JumpPostGroundingGraceTime = 0f;
        public float WallJumpClamp = 1.5f;

        [Header("Ladder Climbing")]
        public float ClimbingSpeed = 4f;
        public float AnchoringDuration = 0.25f;
        public LayerMask InteractionLayer;

        [Header("Swimming")]
        public Transform SwimmingReferencePoint;
        public LayerMask WaterLayer;
        public float SwimmingSpeed = 4f;
        public float SwimmingMovementSharpness = 3;
        public float SwimmingOrientationSharpness = 2f;
        public float WaterJumpMultiplier = 1.5f;

        [Header("Charging")]
        public float ChargeSpeed = 25f;
        public float MaxChargeTime = 1f;
        public float StoppedTime = 0.1f;

        [Header("NoClip")]
        public float NoClipMoveSpeed = 10f;
        public float NoClipSharpness = 15;

        [Header("Misc")]
        public List<Collider> IgnoredColliders = new List<Collider>();
        public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
        public float BonusOrientationSharpness = 10f;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        // public bool OrientTowardsGravity = true;
        public Transform MeshRoot;
        public Transform CameraFollowPoint;
        public float CrouchedCapsuleHeight = 1f;
        public LayerMask BoosterLayer;
        public LayerMask SpringLayer;
        public float SpringBounceAmount = 25f;

        public CharacterState CurrentCharacterState { get; private set; }

        private Collider[] _probedColliders = new Collider[8];
        private RaycastHit[] _probedHits = new RaycastHit[8];
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private bool _jumpInputIsHeld = false;
        private bool _crouchInputIsHeld = false;
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private bool _doubleJumpConsumed = false;
        private bool _canWallJump = false;
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;
        private Collider _waterZone;
        private bool _isTouchingSpring = false;
        private bool _isTouchingBooster = false;

        private Vector3 _currentChargeVelocity;
        private bool _isStopped;
        private bool _mustStopVelocity = false;
        private float _timeSinceStartedCharge = 0;
        private float _timeSinceStopped = 0;

        private Vector3 _wallJumpNormal;
        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        // Ladder vars
        private float _ladderUpDownInput;
        private MyLadder _activeLadder { get; set; }
        private ClimbingState _internalClimbingState;
        private ClimbingState _climbingState
        {
            get
            {
                return _internalClimbingState;
            }
            set
            {
                _internalClimbingState = value;
                _anchoringTimer = 0f;
                _anchoringStartPosition = Motor.TransientPosition;
                _anchoringStartRotation = Motor.TransientRotation;
            }
        }
        private Vector3 _ladderTargetPosition;
        private Quaternion _ladderTargetRotation;
        private float _onLadderSegmentState = 0;
        private float _anchoringTimer = 0f;
        private Vector3 _anchoringStartPosition = Vector3.zero;
        private Quaternion _anchoringStartRotation = Quaternion.identity;
        private Quaternion _rotationBeforeClimbing = Quaternion.identity;

        private void Awake()
        {
            // Handle initial state
            TransitionToState(CharacterState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState);
            CurrentCharacterState = newState;
            OnStateEnter(newState, tmpInitialState);
        }

        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        Motor.SetGroundSolvingActivation(true);
                        break;
                    }
                case CharacterState.Charging:
                    {
                        _currentChargeVelocity = Motor.CharacterForward * ChargeSpeed;
                        _isStopped = false;
                        _timeSinceStartedCharge = 0f;
                        _timeSinceStopped = 0f;
                        break;
                    }
                case CharacterState.NoClip:
                    {
                        Motor.SetCapsuleCollisionsActivation(false);
                        Motor.SetMovementCollisionsSolvingActivation(false);
                        Motor.SetGroundSolvingActivation(false);
                        break;
                    }
                case CharacterState.Swimming:
                    {
                        Motor.SetGroundSolvingActivation(false);
                        AccelerationSpeed = MinStableMoveSpeed;
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        _rotationBeforeClimbing = Motor.TransientRotation;

                        Motor.SetMovementCollisionsSolvingActivation(false);
                        Motor.SetGroundSolvingActivation(false);
                        _climbingState = ClimbingState.Anchoring;

                        // Store the target position and rotation to snap to
                        _ladderTargetPosition = _activeLadder.ClosestPointOnLadderSegment(Motor.TransientPosition, out _onLadderSegmentState);
                        _ladderTargetRotation = _activeLadder.transform.rotation;
                        break;
                    }
            }
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
                case CharacterState.NoClip:
                    {
                        Motor.SetCapsuleCollisionsActivation(true);
                        Motor.SetMovementCollisionsSolvingActivation(true);
                        Motor.SetGroundSolvingActivation(true);
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        Motor.SetMovementCollisionsSolvingActivation(true);
                        Motor.SetGroundSolvingActivation(true);
                        break;
                    }
            }
        }

        /// <summary>
        /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            _jumpInputIsHeld = inputs.JumpHeld;
            _crouchInputIsHeld = inputs.CrouchHeld;

            // Handle state transition from input
            if (inputs.ChargingDown)
            {
                TransitionToState(CharacterState.Charging);
            }

            // Handle state transition from input
            if (inputs.NoClipDown)
            {
                if (CurrentCharacterState == CharacterState.Default)
                {
                    TransitionToState(CharacterState.NoClip);
                }
                else if (CurrentCharacterState == CharacterState.NoClip)
                {
                    TransitionToState(CharacterState.Default);
                }
            }

            // Handle ladder transitions
            _ladderUpDownInput = inputs.MoveAxisForward;
            if (inputs.ClimbLadder)
            {
                if (Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders, InteractionLayer, QueryTriggerInteraction.Collide) > 0)
                {
                    if (_probedColliders[0] != null)
                    {
                        // Handle ladders
                        MyLadder ladder = _probedColliders[0].gameObject.GetComponent<MyLadder>();
                        if (ladder)
                        {
                            // Transition to ladder climbing state
                            if (CurrentCharacterState == CharacterState.Default)
                            {
                                _activeLadder = ladder;
                                TransitionToState(CharacterState.Climbing);
                            }
                            // Transition back to default movement state
                            else if (CurrentCharacterState == CharacterState.Climbing)
                            {
                                _climbingState = ClimbingState.DeAnchoring;
                                _ladderTargetPosition = Motor.TransientPosition;
                                _ladderTargetRotation = _rotationBeforeClimbing;
                            }
                        }
                    }
                }
            }

            // Clamp input
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Move and look inputs
                        //_moveInputVector = Quaternion.Euler(0, cameraPlanarRotation.eulerAngles.y, 0) * moveInputVector;
                        _moveInputVector = cameraPlanarRotation * moveInputVector;
                        _lookInputVector = cameraPlanarDirection;

                        switch (OrientationMethod)
                        {
                            case OrientationMethod.TowardsCamera:
                                _lookInputVector = cameraPlanarDirection;
                                break;
                            case OrientationMethod.TowardsMovement:
                                _lookInputVector = _moveInputVector.normalized;
                                break;
                        }

                        // Jumping input
                        if (inputs.JumpDown)
                        {
                            _timeSinceJumpRequested = 0f;
                            _jumpRequested = true;
                        }

                        // Crouching input
                        if (inputs.CrouchDown)
                        {
                            _shouldBeCrouching = true;

                            if (!_isCrouching)
                            {
                                _isCrouching = true;
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                                MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                            }
                        }
                        else if (inputs.CrouchUp)
                        {
                            _shouldBeCrouching = false;
                        }
                        break;
                    }
                case CharacterState.NoClip:
                    {
                        _moveInputVector = inputs.CameraRotation * moveInputVector;
                        _lookInputVector = cameraPlanarDirection;
                        break;
                    }
                case CharacterState.Swimming:
                    {
                        _jumpRequested = inputs.JumpHeld;

                        _moveInputVector = inputs.CameraRotation * moveInputVector;
                        _lookInputVector = cameraPlanarDirection;
                        break;
                    }
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref AICharacterInputs inputs)
        {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        private Quaternion _tmpTransientRot;

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            // Handle detecting Springs
            // Bug: top collision gives different results than side collisions
            if (_isTouchingSpring == false && Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders, SpringLayer, QueryTriggerInteraction.Collide) > 0)
            {
                Motor.ForceUnground(0.2f);
                AddVelocity(Vector3.up * SpringBounceAmount);
                //TransitionToState(CharacterState.Charging);
                _isTouchingSpring = true;
            }
            else
            {
                _isTouchingSpring = false;
            }

            // Handle detecting Booster
            if (_isTouchingBooster == false && Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders, BoosterLayer, QueryTriggerInteraction.Collide) > 0)
            {
                Motor.ForceUnground(0.2f);
                TransitionToState(CharacterState.Charging);
                _isTouchingBooster = true;
            }
            else
            {
                _isTouchingBooster = false;
            }

            // Handle detecting water surfaces
            {
                // Do a character overlap check to detect water surfaces
                if (Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders, WaterLayer, QueryTriggerInteraction.Collide) > 0)
                {
                    // If a water surface was detected
                    if (_probedColliders[0] != null)
                    {
                        // If the swimming reference point is inside the box, make sure we are in swimming state
                        if (Physics.ClosestPoint(SwimmingReferencePoint.position, _probedColliders[0], _probedColliders[0].transform.position, _probedColliders[0].transform.rotation) == SwimmingReferencePoint.position)
                        {
                            if (CurrentCharacterState == CharacterState.Default)
                            {
                                TransitionToState(CharacterState.Swimming);
                                _waterZone = _probedColliders[0];
                            }
                        }
                        // otherwise; default state
                        else
                        {
                            if (CurrentCharacterState == CharacterState.Swimming)
                            {
                                TransitionToState(CharacterState.Default);
                            }
                        }
                    }
                }
            }

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
                case CharacterState.Charging:
                    {
                        // Update times
                        _timeSinceStartedCharge += deltaTime;
                        if (_isStopped)
                        {
                            _timeSinceStopped += deltaTime;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                case CharacterState.NoClip:
                case CharacterState.Swimming:
                    {
                        if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                        {
                            // Smoothly interpolate from current to target look direction
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                            // Set the current rotation (which will be used by the KinematicCharacterMotor)
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }

                        Vector3 currentUp = (currentRotation * Vector3.up);
                        if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
                        {
                            // Rotate from current up to invert gravity
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
                        {
                            if (Motor.GroundingStatus.IsStableOnGround)
                            {
                                Vector3 initialCharacterBottomHemiCenter = Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                                Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                                // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                                Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * Motor.Capsule.radius));
                            }
                            else
                            {
                                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                            }
                        }
                        else
                        {
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        switch (_climbingState)
                        {
                            case ClimbingState.Climbing:
                                currentRotation = _activeLadder.transform.rotation;
                                break;
                            case ClimbingState.Anchoring:
                            case ClimbingState.DeAnchoring:
                                currentRotation = Quaternion.Slerp(_anchoringStartRotation, _ladderTargetRotation, (_anchoringTimer / AnchoringDuration));
                                break;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Ground movement
                        if (Motor.GroundingStatus.IsStableOnGround)
                        {
                            float currentVelocityMagnitude = currentVelocity.magnitude;

                            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
                            if (currentVelocityMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
                            {
                                // Take the normal from where we're coming from
                                Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
                                if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
                                {
                                    effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
                                }
                                else
                                {
                                    effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
                                }
                            }

                            // Reorient velocity on slope
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                            // Calculate target velocity
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                            // Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;  // Old Version

                            // TEST
                            // Acceleration / Momentum handling
                            if (AccelerationSpeed < MaxStableMoveSpeed && _moveInputVector.sqrMagnitude > 0f)
                            {
                                AccelerationSpeed += AccelerationRate * Time.deltaTime;
                            }
                            if (_moveInputVector.sqrMagnitude == 0f)
                            {
                                AccelerationSpeed -= AccelerationRate * Time.deltaTime * 3f;
                            }
                            if (AccelerationSpeed > MaxStableMoveSpeed)
                            {
                                AccelerationSpeed = MaxStableMoveSpeed;
                            }
                            if (AccelerationSpeed < MinStableMoveSpeed)
                            {
                                AccelerationSpeed = MinStableMoveSpeed;
                            }

                            Vector3 targetMovementVelocity = reorientedInput * AccelerationSpeed;
                            

                            // Smooth movement Velocity
                            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
                        }
                        // Air movement
                        else
                        {
                            // Add move input
                            if (_moveInputVector.sqrMagnitude > 0f)
                            {
                                Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

                                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                                // Limit air velocity from inputs
                                if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                                {
                                    // clamp addedVel to make total vel not exceed max vel on inputs plane
                                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                                }
                                else
                                {
                                    // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                                    {
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                                    }
                                }

                                // Prevent air-climbing sloped walls
                                if (Motor.GroundingStatus.FoundAnyGround)
                                {
                                    if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                                    {
                                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                                    }
                                }

                                // Handle Acceleration in air
                                if (AccelerationSpeed < MaxStableMoveSpeed && _moveInputVector.sqrMagnitude > 0f)
                                {
                                    AccelerationSpeed += AccelerationRate * Time.deltaTime;
                                }
                                if (AccelerationSpeed > MaxStableMoveSpeed)
                                {
                                    AccelerationSpeed = MaxStableMoveSpeed;
                                }

                                // Apply added velocity
                                currentVelocity += addedVelocity;
                            }

                            // Gravity
                            currentVelocity += Gravity * deltaTime;

                            // Drag
                            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
                        }

                        // Handle jumping
                        _jumpedThisFrame = false;
                        _timeSinceJumpRequested += deltaTime;
                        if (_jumpRequested)
                        {
                            // Handle double jump
                            if (AllowDoubleJump)
                            {
                                if (_jumpConsumed && !_doubleJumpConsumed && (AllowJumpingWhenSliding ? !Motor.GroundingStatus.FoundAnyGround : !Motor.GroundingStatus.IsStableOnGround))
                                {
                                    Motor.ForceUnground(0.1f);

                                    // Add to the return velocity and reset jump state
                                    currentVelocity += (Motor.CharacterUp * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                    _jumpRequested = false;
                                    _doubleJumpConsumed = true;
                                    _jumpedThisFrame = true;
                                }
                            }

                            // See if we actually are allowed to jump
                            if (_canWallJump ||
                                (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
                            {
                                // Calculate jump direction before ungrounding
                                Vector3 jumpDirection = Motor.CharacterUp;
                                if (_canWallJump)
                                {
                                    // Wall jump
                                    jumpDirection = Vector3.ClampMagnitude(_wallJumpNormal + Vector3.up, WallJumpClamp);
                                }
                                else if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                {
                                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                                }

                                // Makes the character skip ground probing/snapping on its next update. 
                                // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                                Motor.ForceUnground();

                                // Add to the return velocity and reset jump state
                                currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                                _jumpRequested = false;
                                _jumpConsumed = true;
                                _jumpedThisFrame = true;
                            }
                        }

                        // Reset wall jump
                        _canWallJump = false;

                        // Take into account additive velocity
                        if (_internalVelocityAdd.sqrMagnitude > 0f)
                        {
                            currentVelocity += _internalVelocityAdd;
                            _internalVelocityAdd = Vector3.zero;
                        }
                        break;
                    }
                case CharacterState.NoClip:
                    {
                        float verticalInput = 0f + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);

                        // Smoothly interpolate to target velocity
                        Vector3 targetMovementVelocity = (_moveInputVector + (Motor.CharacterUp * verticalInput)).normalized * NoClipMoveSpeed;
                        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-NoClipSharpness * deltaTime));
                        break;
                    }
                case CharacterState.Swimming:
                    {
                        float verticalInput = 0f + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);

                        // Smoothly interpolate to target swimming velocity
                        Vector3 targetMovementVelocity = (_moveInputVector + (Motor.CharacterUp * verticalInput)).normalized * SwimmingSpeed;
                        Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-SwimmingMovementSharpness * deltaTime));

                        // See if our swimming reference point would be out of water after the movement from our velocity has been applied
                        {
                            Vector3 resultingSwimmingReferancePosition = Motor.TransientPosition + (smoothedVelocity * deltaTime) + (SwimmingReferencePoint.position - Motor.TransientPosition);
                            Vector3 closestPointWaterSurface = Physics.ClosestPoint(resultingSwimmingReferancePosition, _waterZone, _waterZone.transform.position, _waterZone.transform.rotation);

                            // if our position would be outside the water surface on next update, project the velocity on the surface normal so that it would not take us out of the water
                            if (closestPointWaterSurface != resultingSwimmingReferancePosition)
                            {
                                Vector3 waterSurfaceNormal = (resultingSwimmingReferancePosition - closestPointWaterSurface).normalized;
                                smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, waterSurfaceNormal);

                                // Jump out of water
                                if (_jumpRequested)
                                {
                                    smoothedVelocity += (Motor.CharacterUp * JumpUpSpeed * WaterJumpMultiplier) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                }
                            }
                        }

                        currentVelocity = smoothedVelocity;
                        break;
                    }
                case CharacterState.Charging:
                    {
                        // If we have stopped and need to cancel velocity, do it here
                        if (_mustStopVelocity)
                        {
                            currentVelocity = Vector3.zero;
                            _mustStopVelocity = false;
                        }

                        if (_isStopped)
                        {
                            // When stopped, do no velocity handling except gravity
                            currentVelocity += Gravity * deltaTime;
                        }
                        else
                        {
                            // When charging, velocity is always constant
                            float previousY = currentVelocity.y;
                            currentVelocity = _currentChargeVelocity;
                            currentVelocity.y = previousY;
                            currentVelocity += Gravity/2 * deltaTime;
                        }
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        currentVelocity = Vector3.zero;

                        switch (_climbingState)
                        {
                            case ClimbingState.Climbing:
                                currentVelocity = (_ladderUpDownInput * _activeLadder.transform.up).normalized * ClimbingSpeed;
                                break;
                            case ClimbingState.Anchoring:
                            case ClimbingState.DeAnchoring:
                                Vector3 tmpPosition = Vector3.Lerp(_anchoringStartPosition, _ladderTargetPosition, (_anchoringTimer / AnchoringDuration));
                                currentVelocity = Motor.GetVelocityForMovePosition(Motor.TransientPosition, tmpPosition, deltaTime);
                                break;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Handle jump-related values
                        {
                            // Handle jumping pre-ground grace period
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                // If we're on a ground surface, reset jumping values
                                if (!_jumpedThisFrame)
                                {
                                    _doubleJumpConsumed = false;
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // Keep track of time since we were last able to jump (for grace period)
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // Handle uncrouching
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // Do an overlap check with the character's standing height to see if there are any obstructions
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // If obstructions, just stick to crouching dimensions
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                            }
                            else
                            {
                                // If no obstructions, uncrouch
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
                case CharacterState.Charging:
                    {
                        // Detect being stopped by elapsed time
                        if (!_isStopped && _timeSinceStartedCharge > MaxChargeTime)
                        {
                            _mustStopVelocity = true;
                            _isStopped = true;
                        }

                        // Detect end of stopping phase and transition back to default movement state
                        if (_timeSinceStopped > StoppedTime)
                        {
                            TransitionToState(CharacterState.Default);
                        }
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        switch (_climbingState)
                        {
                            case ClimbingState.Climbing:
                                // Detect getting off ladder during climbing
                                _activeLadder.ClosestPointOnLadderSegment(Motor.TransientPosition, out _onLadderSegmentState);
                                if (Mathf.Abs(_onLadderSegmentState) > 0.05f)
                                {
                                    _climbingState = ClimbingState.DeAnchoring;

                                    // If we're higher than the ladder top point
                                    if (_onLadderSegmentState > 0)
                                    {
                                        _ladderTargetPosition = _activeLadder.TopReleasePoint.position;
                                        _ladderTargetRotation = _activeLadder.TopReleasePoint.rotation;
                                    }
                                    // If we're lower than the ladder bottom point
                                    else if (_onLadderSegmentState < 0)
                                    {
                                        _ladderTargetPosition = _activeLadder.BottomReleasePoint.position;
                                        _ladderTargetRotation = _activeLadder.BottomReleasePoint.rotation;
                                    }
                                }
                                break;
                            case ClimbingState.Anchoring:
                            case ClimbingState.DeAnchoring:
                                // Detect transitioning out from anchoring states
                                if (_anchoringTimer >= AnchoringDuration)
                                {
                                    if (_climbingState == ClimbingState.Anchoring)
                                    {
                                        _climbingState = ClimbingState.Climbing;
                                    }
                                    else if (_climbingState == ClimbingState.DeAnchoring)
                                    {
                                        TransitionToState(CharacterState.Default);
                                    }
                                }

                                // Keep track of time since we started anchoring
                                _anchoringTimer += deltaTime;
                                break;
                        }
                        break;
                    }
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }

            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }

            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // We can wall jump only if we are not stable on ground and are moving against an obstruction
                        if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
                        {
                            _canWallJump = true;
                            _wallJumpNormal = hitNormal;
                        }
                        break;
                    }
                case CharacterState.Charging:
                    {
                        // Detect being stopped by obstructions
                        if (!_isStopped && !hitStabilityReport.IsStable && Vector3.Dot(-hitNormal, _currentChargeVelocity.normalized) > 0.5f)
                        {
                            _mustStopVelocity = true;
                            _isStopped = true;
                        }
                        break;
                    }
            }
        }

        public void AddVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        _internalVelocityAdd += velocity;
                        break;
                    }
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        protected void OnLanded()
        {
        }

        protected void OnLeaveStableGround()
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}