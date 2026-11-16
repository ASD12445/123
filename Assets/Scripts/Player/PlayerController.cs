using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Speeds")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;

        [Header("Jump / Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -18f;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 1.2f;
        [SerializeField] private float pitchLimit = 85f;

        [Header("Refs")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Camera playerCamera;

        public Camera PlayerCamera => playerCamera;

        private CharacterController _controller;
        private float _pitch;
        private Vector3 _verticalVelocity;

        private InputActionMap _map;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        public bool IsSprinting { get; private set; }
        public Vector3 Velocity => _controller != null ? _controller.velocity : Vector3.zero;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraHolder == null)
            {
                var found = transform.Find("CameraHolder");
                if (found != null)
                    cameraHolder = found;
            }

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            BuildInput();
        }

        private void OnEnable()
        {
            _map?.Enable();
        }

        private void OnDisable()
        {
            _map?.Disable();
        }

        private void OnDestroy()
        {
            if (_map == null)
                return;

            _map.Disable();
            _moveAction.Dispose();
            _lookAction.Dispose();
            _jumpAction.Dispose();
            _sprintAction.Dispose();
            _map.Dispose();
        }

        private void Update()
        {
            bool inputAllowed = GameManager.Instance == null || GameManager.Instance.CursorLocked;

            HandleLook(inputAllowed);
            HandleMovement(inputAllowed);
        }

        private void HandleLook(bool allowed)
        {
            if (!allowed || cameraHolder == null)
                return;

            Vector2 look = _lookAction.ReadValue<Vector2>();
            float mouseX = look.x * mouseSensitivity;
            float mouseY = look.y * mouseSensitivity;

            _pitch = Mathf.Clamp(_pitch - mouseY, -pitchLimit, pitchLimit);
            cameraHolder.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMovement(bool allowed)
        {
            Vector2 input = allowed ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            IsSprinting = allowed && _sprintAction.IsPressed() && input.y > 0.1f;

            float speed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 worldMove = (transform.right * input.x + transform.forward * input.y) * speed;

            if (_controller.isGrounded)
            {
                if (_verticalVelocity.y < 0f)
                    _verticalVelocity.y = -2f;

                if (allowed && _jumpAction.WasPressedThisFrame())
                    _verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 motion = worldMove + Vector3.up * _verticalVelocity.y;
            _controller.Move(motion * Time.deltaTime);
        }

        private void BuildInput()
        {
            _map = new InputActionMap("Player");

            _moveAction = _map.AddAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _lookAction = _map.AddAction("Look", InputActionType.Value);
            _lookAction.AddBinding("<Mouse>/delta");
            _lookAction.AddBinding("<Gamepad>/rightStick");

            _jumpAction = _map.AddAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _sprintAction = _map.AddAction("Sprint", InputActionType.Button);
            _sprintAction.AddBinding("<Keyboard>/leftShift");
            _sprintAction.AddBinding("<Gamepad>/leftStickPress");
        }
    }
}
