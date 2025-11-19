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
        [Header("Capsule")]
        public int inventorySize = 3;
        public List<Image> inventorySlots; // UI 슬롯 이미지들을 담을 리스트
        public Color selectedSlotColor = Color.yellow; // 선택된 슬롯 테두리 색
        public Color defaultSlotColor = Color.white;   // 기본 슬롯 테두리 색
        
        // [변경점 1] 인벤토리 타입 변경
        private List<CapsuleData> inventory = new List<CapsuleData>();
        private int currentInventoryIndex = -1;
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


#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private Vector3 _slipVelocity = Vector3.zero;
        

        private const float _threshold = 0.01f;

        private bool _isOnPuddle = false;
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
            UpdateInventoryUI();

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

            Inventory();
            HandleAiming(); 
            JumpAndGravity();
            GroundedCheck();
            Move();

            // 수영 상태 Animator 업데이트
            if (_hasAnimator){
                _animator.SetBool(_animIDIsSwimming, _isSwimming);
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

        private void Move()
        {
            float targetSpeed;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 1. 조준 상태 로직
            if (_isAiming)
            {
                targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
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
            // 3. 일반 및 수영 상태 로직 (이하 동일)
            else
            {
                targetSpeed = _isSwimming ? swimSpeed : (_input.sprint ? SprintSpeed : MoveSpeed);
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

        private void Inventory()
        {
            if (Input.GetKeyDown(KeyCode.Q) && currentInventoryIndex != -1)
            {
                Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                if (Physics.Raycast(ray, out RaycastHit hit, 50f))
                {
                    if (hit.collider.CompareTag("WaterSource")) ActivateCapsule(hit.point);
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f && inventory.Count > 0)
            {
                currentInventoryIndex += (scroll > 0f) ? 1 : -1;
                if (currentInventoryIndex >= inventory.Count) currentInventoryIndex = 0;
                if (currentInventoryIndex < 0) currentInventoryIndex = inventory.Count - 1;
                
                UpdateInventoryUI(); // 아이템 선택 변경 시 UI 업데이트
            }
        }
    
        void ActivateCapsule(Vector3 activationPoint)
        {
            CapsuleData capsuleToActivate = inventory[currentInventoryIndex];
            
            if (capsuleToActivate.animalPrefab != null)
            {
                Instantiate(capsuleToActivate.animalPrefab, activationPoint, Quaternion.identity);
            }
            
            GameManager.Instance.OnCapsuleCollected(capsuleToActivate.capsuleID);
            inventory.RemoveAt(currentInventoryIndex);
            
            currentInventoryIndex = (inventory.Count > 0) ? 0 : -1;
            UpdateInventoryUI();
        }
        
        void UpdateInventoryUI()
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                // 인벤토리에 아이템이 있는 경우
                if (i < inventory.Count)
                {
                    inventorySlots[i].sprite = inventory[i].capsuleIcon;
                    inventorySlots[i].color = new Color(1, 1, 1, 1); // 보이게
                }
                else // 빈 슬롯인 경우
                {
                    inventorySlots[i].sprite = null;
                    inventorySlots[i].color = new Color(1, 1, 1, 0.5f); // 반투명하게
                }
                
                // 현재 선택된 슬롯 테두리 강조
                Image slotBorder = inventorySlots[i].transform.parent.GetComponent<Image>();
                if (slotBorder != null)
                {
                    slotBorder.color = (i == currentInventoryIndex) ? selectedSlotColor : defaultSlotColor;
                }
            }
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

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);

                    // AI가 들을 수 있도록 소리 이벤트 발생
                    SoundEmitter.MakeSound(transform.position, 10f); // 10f는 청각 범위
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
                if (inventory.Count < inventorySize)
                {
                    // [변경점 2] 오브젝트가 아닌 '데이터'를 생성하여 인벤토리에 추가
                    CapsuleData newData = new CapsuleData
                    {
                        capsuleID = capsule.capsuleID,
                        animalPrefab = capsule.animalPrefab,
                        capsuleIcon = capsule.capsuleIcon
                    };
                    inventory.Add(newData);

                    currentInventoryIndex = inventory.Count - 1;
                    Destroy(capsule.gameObject);
                    UpdateInventoryUI(); // 아이템 획득 시 UI 업데이트
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((waterMask & (1 << other.gameObject.layer)) != 0)
            {
                _isSwimming = false;
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