using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using UnityEngine.UI;
#endif

public struct CapsuleData
{
    public string capsuleID;
    public GameObject animalPrefab;
    public Sprite capsuleIcon; // UI에 표시할 아이콘
}

public class PlayerInteraction : MonoBehaviour
{
    // ... 이하 스크립트 내용
}

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 30.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;
        [Tooltip("Crouch speed of the character in m/s")]
        public float CrouchingSpeed = 1.5f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Swimming")]
        [SerializeField] private LayerMask waterMask;
        [Tooltip("Swim speed of the character in m/s")]
        public float swimSpeed = 0.5f;
        [Tooltip("Gravity in water (should be less negative than normal gravity)")]
        public float swimGravity = -5.0f;  // -15보다 약함
        private bool _isSwimming = false;
        [Header("Camera")]
        public bool useTopDownCamera = true; 
        [Header("Stun")]
        public float slideStunDuration = 1.5f;
        private bool isStunned = false;
        private Vector3 slideVelocity = Vector3.zero;
        private float slideFriction = 0.95f;
        [Header("Aiming")]
        [Tooltip("The virtual camera used for aiming. Assign your Aiming VCam GameObject here.")]
        public GameObject AimVirtualCamera;
        [Tooltip("How fast the character rotates to face the camera direction while aiming.")]
        public float AimRotationSpeed = 20f;
        private bool _isAiming;
        
        private List<CapsuleData> collectedCapsules = new List<CapsuleData>();
        private Camera mainCamera;
    

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float nextRippleTime = 0f;
        private Vector3 _slideVelocity = Vector3.zero;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // slippery
        private float _slipFriction = 0.93f; // (0~1, 낮을수록 오래 미끄러짐)
        private float _slipThreshold = 0.1f; // 이 이하로 느려지면 정지
        private float _slipControlRate = 1.2f; // 방향전환 속도
        private float _slipSpeed = 4f;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDIsSwimming;
        private int _animIDIsRolling;
        private int _animIDIsAiming;
        private int _animIDIsCrouching;


#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private Vector3 _slipVelocity = Vector3.zero;
        

        private const float _threshold = 0.01f;
        private bool _isCrouching = false;
        private bool _isOnPuddle = false;
        private bool _canReleaseAnimal = false;
        private bool _hasAnimator;
        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }
       

        private void Awake(){
            // get a reference to our main camera
            mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start(){
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _animIDIsRolling = Animator.StringToHash("isRolling");
            _animIDIsAiming = Animator.StringToHash("isAiming");
        }

        private void Update(){
            _hasAnimator = TryGetComponent(out _animator);

            _isAiming = _input.aim; // 현재 조준 상태인지 업데이트
            AimVirtualCamera.SetActive(_isAiming);

            if (isStunned)
            {
                // 미끄러지는 동안 계속 이동
                if (slideVelocity.magnitude > 0.1f)
                {
                    slideVelocity *= slideFriction;
                    _controller.Move(slideVelocity * Time.deltaTime);
                }
                return; // 조작 불가
            }

            HandleCrouchInput();
            HandleAiming(); 
            JumpAndGravity();
            GroundedCheck();
            Move();

            // 수영 상태 Animator 업데이트
            if (_hasAnimator){
                _animator.SetBool(_animIDIsSwimming, _isSwimming);
            }

            if (_canReleaseAnimal && Input.GetKeyDown(KeyCode.Q)) {
                ActivateCapsule(transform.position);
            }
        }

        private void LateUpdate()
        {
            // 카메라 회전은 항상 처리되어야 마우스 입력에 반응합니다.
            CameraRotation();

            // 조준 상태일 때, 캐릭터가 카메라 방향을 보도록 회전시킵니다.
            // 이 로직을 LateUpdate로 옮겨 피드백 루프를 방지합니다.
            if (_isAiming)
            {
                float targetYaw = _mainCamera.transform.rotation.eulerAngles.y;
                Quaternion targetRotation = Quaternion.Euler(0.0f, targetYaw, 0.0f);
                
                // 부드럽게 캐릭터를 회전시킵니다.
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, AimRotationSpeed * Time.deltaTime);
            }
        }

        private void HandleAiming()
        {
            // StarterAssetsInputs 스크립트에 'aim' boolean 변수가 추가되었다고 가정합니다.
            _isAiming = _input.aim;

            // 조준 상태에 따라 가상 카메라 활성화/비활성화
            AimVirtualCamera.SetActive(_isAiming);

            // 애니메이터 파라미터 업데이트
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDIsAiming, _isAiming);
            }
        }

        private void AssignAnimationIDs(){
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDIsSwimming = Animator.StringToHash("isSwimming");
            _animIDIsCrouching = Animator.StringToHash("IsCrouching");
        }

        private void GroundedCheck(){
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation(){
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move(){
            float targetSpeed;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 1. 조준 상태 로직
            if (_isAiming){
                targetSpeed = _isCrouching ? CrouchingSpeed : (_input.sprint ? SprintSpeed : MoveSpeed);

                if (_input.move == Vector2.zero) targetSpeed = 0.0f;

                float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
                float speedOffset = 0.1f;

                // 가속 및 감속
                if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
                {
                    _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                    _speed = Mathf.Round(_speed * 1000f) / 1000f;
                }
                else
                {
                    _speed = targetSpeed;
                }
                _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
                if (_animationBlend < 0.01f) _animationBlend = 0f;
                


                // 이동 방향 계산 (캐릭터의 현재 방향 기준)
                Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y);
                Vector3 targetDirection = transform.TransformDirection(inputDirection); // 입력 값을 월드 좌표로 변환

                // 캐릭터 이동
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

                // 애니메이터 업데이트
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, _animationBlend);
                }
            }
            // 2. 웅덩이 상태 로직 (이하 동일)
            else if (_isOnPuddle)
            {
                Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y);
                
                if (_input.move != Vector2.zero)
                {
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);

                    Vector3 worldDir = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
                    Vector3 targetSlipVelocity = worldDir.normalized * _slipSpeed;
                    
                    _slipVelocity = Vector3.Lerp(_slipVelocity, targetSlipVelocity, Time.deltaTime * _slipControlRate);
                }
                else
                {   
                    _slipVelocity *= _slipFriction;
                    if (_slipVelocity.magnitude < _slipThreshold)
                    {
                        _slipVelocity = Vector3.zero;
                    }
                }
                Vector3 vertical = new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime;
                Vector3 moveAmount = _slipVelocity * Time.deltaTime;
                
                _controller.Move(moveAmount + vertical);
                
                _animationBlend = Mathf.Lerp(_animationBlend, _slipVelocity.magnitude, Time.deltaTime * SpeedChangeRate);
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, _animationBlend);
                    _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
                }
            }
            // 3. 수영 상태 - 펭귄 특화 로직
            else if (_isSwimming)
            {
                // A. 속도 설정: 펭귄이니까 걷기보다 훨씬 빠르게 (예: 1.5배 ~ 2배)
                float penguinSwimSpeed = swimSpeed; // Inspector에서 SprintSpeed보다 높게 설정하세요.
                
                // 입력이 없어도 바로 0이 되지 않도록 처리 (관성)
                if (_input.move == Vector2.zero) targetSpeed = 0.0f;
                else targetSpeed = penguinSwimSpeed;

                // B. 가감속 로직 (물속 저항 구현)
                // 지상보다 SpeedChangeRate를 낮게 잡아서(Time.deltaTime * 2f 등) 미끄러지듯 가속/감속되게 함
                float swimAcceleration = 2.0f; // 지상보다 낮게 설정하여 관성 부여
                
                _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * swimAcceleration);
                if (_animationBlend < 0.01f) _animationBlend = 0f;
                
                // 실제 이동 속도에 반영
                _speed = _animationBlend; 

                // C. 회전 로직
                Vector3 inputDir = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                if (_input.move != Vector2.zero)
                {
                    _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    // 물속에서는 회전이 지상보다 조금 더 느리고 부드럽게 (RotationSmoothTime을 늘림)
                    float swimRotationSmoothTime = RotationSmoothTime * 1.5f; 
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, swimRotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }

                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

                // D. 부력 처리 (중요)
                // 수영 중에는 중력(_verticalVelocity)을 0으로 만들거나, 
                // 혹은 스페이스바(Jump)를 누르면 상승, C(Crouch)를 누르면 하강하는 로직으로 대체해야 함.
                // 여기서는 일단 '수면 유지'를 위해 중력을 무시하는 예시입니다.
                if (_verticalVelocity < 0) _verticalVelocity = 0f; 

                // 최종 이동: 입력이 없어도 _speed(관성)가 남아있으면 계속 앞으로 전진함
                // 펭귄은 수영할 때 전신을 쓰므로, 단순히 move.x, y가 아니라 '바라보는 방향'으로 계속 나아가게 함
                if(_input.move != Vector2.zero || _speed > 0.1f)
                {
                    // 입력이 있을 땐 입력 방향, 없을 땐 현재 캐릭터가 보는 방향으로 관성 이동
                    Vector3 moveDir = (_input.move != Vector2.zero) ? targetDirection.normalized : transform.forward;
                    _controller.Move(moveDir * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                }

                // 애니메이터
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, _animationBlend);
                    _animator.SetFloat(_animIDMotionSpeed, 1f); // 수영은 모션 속도 일정하게
                }
            }
            else {
                targetSpeed = _isCrouching ? CrouchingSpeed : (_input.sprint ? SprintSpeed : MoveSpeed);
                if (_input.move == Vector2.zero) targetSpeed = 0.0f;

                float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
                float speedOffset = 0.1f;

                if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
                {
                    _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                    _speed = Mathf.Round(_speed * 1000f) / 1000f;
                }
                else
                {
                    _speed = targetSpeed;
                }

                _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
                if (_animationBlend < 0.01f) _animationBlend = 0f;

                Vector3 inputDir = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                if (_input.move != Vector2.zero)
                {
                    _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }

                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, _animationBlend);
                    _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
                }
            }
        }

        private void JumpAndGravity()
        {
            if (_isSwimming)
            {
                // 수영 중 약한 중력 (또는 0)
                float swimGravity = -8.0f;  

                // 수영 중 위로 올라가기
                if (_input.jump)
                {
                    _verticalVelocity = 2f; // 작은 상승력
                }
                else
                {
                    _verticalVelocity = 0f; // 수평 유지
                }
                
                if (_verticalVelocity < _terminalVelocity)
                {
                    _verticalVelocity += swimGravity * Time.deltaTime;
                }
                return; // 일반 중력 로직 건너뛰기
            }
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        private void HandleCrouchInput()
        {
            // C키를 눌렀을 때
            if (Input.GetKeyDown(KeyCode.C))
            {
                // 앉기 상태를 토글(반전)
                _isCrouching = !_isCrouching;
                
                // 애니메이터에 상태 전달
                _animator.SetBool(_animIDIsCrouching, _isCrouching);
            }
        }

        void ActivateCapsule(Vector3 activationPoint)
        {
            // 1. 가진 캡슐이 없으면 실패
            if (collectedCapsules.Count == 0)
            {
                Debug.Log("해방할 캡슐이 없습니다!");
                return;
            }

            // 2. 가장 먼저 먹은 캡슐 꺼내기 (FIFO)
            CapsuleData capsuleToRelease = collectedCapsules[0];

            // 3. 동물 소환
            if (capsuleToRelease.animalPrefab != null)
            {
                // activationPoint는 보통 플레이어 앞이나 지정된 위치
                Instantiate(capsuleToRelease.animalPrefab, activationPoint, Quaternion.identity);
            }

            // 4. [핵심 변경] "수집"이 아니라 "방생" 함수를 호출!
            // (이미 먹을 때 불은 켜졌고, 여기서는 방생 카운트를 올려서 탈출 조건을 체크함)
            GameManager.Instance.OnAnimalReleased();

            // 5. 리스트에서 제거 (사용했으니 삭제)
            collectedCapsules.RemoveAt(0);
        }
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent){
            float noiseRange = 3.0f; // 기본 걷기 범위
            
            if (_isCrouching) return; 
            
            if (_isSwimming) {
                SoundEmitter.MakeSound(transform.position, noiseRange, true);
                return;
            }
            
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                float currentSpeed = _controller.velocity.magnitude; // 현재 실제 이동 속도
                
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                    
                    if (currentSpeed > MoveSpeed + 1.0f) 
                    {
                        noiseRange = 6.0f; // 뛰기 범위
                    }
                }

                if (Time.time >= nextRippleTime)
                {
                    // 3. 감지 로직 실행
                    SoundEmitter.MakeSound(transform.position, noiseRange, false);
                    
                    // 4. 다음 파동 시간 예약
                    nextRippleTime = Time.time + 0.2f; 
                }
            }
        }
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if ((waterMask & (1 << other.gameObject.layer)) != 0)
            {
                _isSwimming = true;
                _animator.SetBool("isSwimming", _isSwimming);
                _canReleaseAnimal = true;
            }

            else if (other.CompareTag("WaterPuddle") && !isStunned)
            {
                _isOnPuddle = true;
                // Roll 애니메이션 시작
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDIsRolling, true);
                }
                
                // 현재 속도로 미끄러지기 시작
                Vector3 currentVel = _controller.velocity;
                currentVel.y = 0;
                if (currentVel.magnitude > 0.5f)
                {
                    _slideVelocity = currentVel;
                }
                Debug.Log("Slippery in");
            }

            else if (other.TryGetComponent<CapsuleController>(out CapsuleController capsule))
            {
                // 캡슐 데이터 생성 및 리스트에 저장 (주머니에 넣기)
                CapsuleData newData = new CapsuleData
                {
                    capsuleID = capsule.capsuleID,
                    animalPrefab = capsule.animalPrefab, // 나중에 소환할 프리팹 정보 저장
                    capsuleIcon = capsule.capsuleIcon
                };
                collectedCapsules.Add(newData);

                // 먹자마자 게임 매니저에 알림 (비상구 불 켜기 등)
                GameManager.Instance.OnCapsuleCollected(capsule.capsuleID);

                // 캡슐 오브젝트 파괴
                Destroy(capsule.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((waterMask & (1 << other.gameObject.layer)) != 0)
            {
                _isSwimming = false;
                _canReleaseAnimal = false;
            }

            else if (other.CompareTag("WaterPuddle"))
            {
                _isOnPuddle = false;
                _slideVelocity = Vector3.zero;
                
                // Roll 애니메이션 종료
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDIsRolling, false);
                }
                
                Debug.Log("Slippery out");
                
            }
        }
    }
}