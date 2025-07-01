using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraSystems
{
    public class CameraController : MonoBehaviour
    {
        private const float DEFAULT_ZOOM = 25f;

        private const float MAX_CAMERA_ANGLE = 89f;
        private const float MIN_CAMERA_ANGLE = 15f;
        private const float BASE_CAMERA_ANGLE_X = 45f;
        private const float BASE_CAMERA_ANGLE_Y = -90f;

        [SerializeField] private CinemachineCamera targetCamera;
        private CinemachinePositionComposer _positionComposer;
        private CinemachineConfiner3D confiner3D;

        [Header("Input")] [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference zoomAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference panAction;
        [SerializeField] private InputActionReference orbitAction;
        [SerializeField] private InputActionReference centerAction;

        [SerializeField] private List<InputActionReference> cameraZoomBlockActions = new List<InputActionReference>();
        [SerializeField] private List<InputActionReference> cameraMoveBlockActions = new List<InputActionReference>();
        public static bool CameraZoomBlocked = false;

        [Header("Movement")] [SerializeField] private float movementSpeed = 2.5f;
        private Vector2 _moveInput;

        [Header("Pan")] [SerializeField] private float dragPanSpeed = 2.5f;
        private Vector2 _lastMousePosition;
        private Vector2 _panDelta;
        private bool _dragPanActive = false;

        [Header("Zoom")] [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float zoomStep = 0.25f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 40f;
        private Vector2 _zoomInput;
        private float _targetCameraDistance = DEFAULT_ZOOM;

        [Header("Orbit")] [SerializeField] private float orbitSmoothFactor = 0.1f;
        [SerializeField] private float orbitSpeed = 0.01f;


        private Vector2 _lookInput;

        private float _targetOrbitRotationX = BASE_CAMERA_ANGLE_X;
        private float _targetOrbitRotationY = BASE_CAMERA_ANGLE_Y;

        private float _rotationSpeedMultiplier = 1f;
        private float _movementSpeedMultiplier = 1f;
        private bool _invertX;
        private bool _invertY;


        private Bounds _currentBounds;
        private bool _useBounds;

        private void Awake()
        {
            _positionComposer = targetCamera.GetComponent<CinemachinePositionComposer>();
            confiner3D = targetCamera.GetComponent<CinemachineConfiner3D>();
            _currentBounds = confiner3D.BoundingVolume.bounds;
            if (_currentBounds.size == Vector3.zero)
            {
                Debug.LogWarning("Camera bounds are not set. Camera movement will not be confined.");
                _useBounds = false;
            }
            else
            {
                _useBounds = true;
            }
        }

        private void Start()
        {
            var mainCamera = CinemachineCore.FindPotentialTargetBrain(targetCamera).OutputCamera;
            var pos = mainCamera.transform.position +
                      mainCamera.transform.forward * _targetCameraDistance;
            transform.position = new Vector3(pos.x, transform.position.y, pos.z);
        }

        private void Update()
        {
            HandleInput();
            HandlePanning();
            HandleCameraZoom();
            HandleCameraOrbit();
            HandleMovement();
            HandleCameraCentring();
        }

        private void HandleInput()
        {
            _moveInput = moveAction.action.ReadValue<Vector2>();
            _zoomInput = zoomAction.action.ReadValue<Vector2>();

            _lookInput = lookAction.action.ReadValue<Vector2>();
            if (_invertX) _lookInput.x = -_lookInput.x;
            if (_invertY) _lookInput.y = -_lookInput.y;
        }

        private void HandleMovement()
        {
            foreach (InputActionReference action in cameraMoveBlockActions)
            {
                if (action.action.IsPressed()) return;
            }

            var moveDirection = transform.forward * _moveInput.y + transform.right * _moveInput.x +
                                transform.right * _panDelta.x + transform.forward * _panDelta.y;
            _panDelta = Vector2.zero;

            if (moveDirection != Vector3.zero)
            {
                var mainCamera = CinemachineCore.FindPotentialTargetBrain(targetCamera).OutputCamera;
                var camPos = mainCamera.transform.position;
                if (IsExactlyTouching(_currentBounds, camPos))
                {
                    var directionToCam = (camPos - targetCamera.transform.position).normalized;

                    Debug.DrawRay(mainCamera.transform.position, directionToCam * 10, Color.red);

                    var dot = Vector3.Dot(moveDirection, directionToCam);
                    if (dot < 0f)
                    {
                        moveDirection -= directionToCam * dot;
                    }
                }
            }

            var desiredPosition = transform.position +
                                  moveDirection * (movementSpeed * Time.deltaTime * _movementSpeedMultiplier);

            transform.position = new Vector3(desiredPosition.x, transform.position.y, desiredPosition.z);
        }

        bool IsExactlyTouching(Bounds bounds, Vector3 position)
        {
            if (!bounds.Contains(position)) return false;

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            return Mathf.Approximately(position.x, min.x) || Mathf.Approximately(position.x, max.x) ||
                   Mathf.Approximately(position.y, min.y) || Mathf.Approximately(position.y, max.y) ||
                   Mathf.Approximately(position.z, min.z) || Mathf.Approximately(position.z, max.z);
        }

        bool IsGoingOutOfBounds(Bounds bounds, Vector3 position)
        {
            return !bounds.Contains(position);
        }

        private void HandlePanning()
        {
            if (panAction.action.WasPressedThisFrame())
            {
                _dragPanActive = true;
                _lastMousePosition = Input.mousePosition;
            }

            if (panAction.action.WasReleasedThisFrame())
            {
                _dragPanActive = false;
            }

            if (!_dragPanActive) return;

            Vector2 mouseDelta = (Vector2)Input.mousePosition - _lastMousePosition;
            _panDelta = -mouseDelta * (dragPanSpeed * _movementSpeedMultiplier);
            _lastMousePosition = Input.mousePosition;
        }

        private void HandleCameraZoom()
        {
            foreach (InputActionReference action in cameraZoomBlockActions)
            {
                if (action.action.IsPressed()) return;
            }

            var mainCamera = CinemachineCore.FindPotentialTargetBrain(targetCamera).OutputCamera;
            var camPos = mainCamera.transform.position;
            var isZoomingAgenestBounds = _zoomInput.y < 0 &&
                                         IsExactlyTouching(_currentBounds, camPos);
            if (CameraZoomBlocked || isZoomingAgenestBounds) return;

            _targetCameraDistance -= _zoomInput.y * zoomStep;
            _targetCameraDistance = Mathf.Clamp(_targetCameraDistance, minZoom, maxZoom);

            _positionComposer.CameraDistance = Mathf.Lerp(
                _positionComposer.CameraDistance,
                _targetCameraDistance, Time.deltaTime * zoomSpeed
            );
        }

        private void HandleCameraOrbit()
        {
            var oAction = orbitAction.action.IsPressed();
            if (oAction)
            {
                Vector2 orbitDirection = _lookInput * (orbitSpeed * _rotationSpeedMultiplier);

                _targetOrbitRotationX -= orbitDirection.y;
                _targetOrbitRotationX = Mathf.Clamp(_targetOrbitRotationX, MIN_CAMERA_ANGLE, MAX_CAMERA_ANGLE);

                _targetOrbitRotationY += orbitDirection.x;
            }

            Quaternion targetRotation = Quaternion.Euler(_targetOrbitRotationX, _targetOrbitRotationY, 0);

            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * orbitSmoothFactor
            );

            // Handle the position transform to ensure it follows the orbit so when move it moves instantily when the movement is applied
            var mainCamera = CinemachineCore.FindPotentialTargetBrain(targetCamera).OutputCamera;
            var camPos = mainCamera.transform.position;
            if (oAction && IsExactlyTouching(_currentBounds, camPos))
            {
                var pos = camPos +
                          mainCamera.transform.forward * _targetCameraDistance;
                transform.position = new Vector3(pos.x, transform.position.y, pos.z);
            }


            transform.rotation = Quaternion.Euler(0, _targetOrbitRotationY, 0);
        }

        private void HandleCameraCentring()
        {
            if (!centerAction.action.triggered) return;

            ResetCameraPosition();
        }

        public void ResetCameraPosition()
        {
            Transform cameraDefaultPosition = GameObject.FindGameObjectWithTag("CameraDefaultPosition").transform;
            transform.position = cameraDefaultPosition.position;

            _targetOrbitRotationX = BASE_CAMERA_ANGLE_X;
            _targetOrbitRotationY = cameraDefaultPosition.eulerAngles.y;
            _targetCameraDistance = DEFAULT_ZOOM;
        }

        public void SetCameraRotationSpeed(float newRotationSpeedMultiplier) =>
            _rotationSpeedMultiplier = newRotationSpeedMultiplier;

        public void SetCameraMovementSpeed(float newMovementSpeedMultiplier) =>
            _movementSpeedMultiplier = newMovementSpeedMultiplier;

        public void SetCameraInvertX(bool invertX) => _invertX = invertX;
        public void SetCameraInvertY(bool invertY) => _invertY = invertY;
        public void SetFOV(float fov) => targetCamera.Lens.FieldOfView = fov;
    }
}