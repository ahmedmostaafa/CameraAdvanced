using System.Collections.Generic;
using System.Linq;
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
        private CinemachineConfiner3D _confiner3D;
        
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference zoomAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference panAction;
        [SerializeField] private InputActionReference orbitAction;
        [SerializeField] private InputActionReference centerAction;

        [SerializeField] private List<InputActionReference> cameraZoomBlockActions = new List<InputActionReference>();
        [SerializeField] private List<InputActionReference> cameraMoveBlockActions = new List<InputActionReference>();
        public static bool CameraZoomBlocked = false;

        [Header("Movement")]
        [SerializeField] private float movementSpeed = 2.5f;
        private Vector2 _moveInput;

        [Header("Pan")] 
        [SerializeField] private float dragPanSpeed = 2.5f;
        private Vector2 _lastMousePosition;
        private Vector2 _panDelta;
        private bool _dragPanActive = false;
        
        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float zoomStep = 0.25f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 40f;
        private Vector2 _zoomInput;
        private float _targetCameraDistance = DEFAULT_ZOOM;
        
        [Header("Orbit")]
        [SerializeField] private float orbitSmoothFactor = 0.1f;
        [SerializeField] private float orbitSpeed = 0.01f;
        private Vector2 _lookInput;
        
        private float _targetOrbitRotationX = BASE_CAMERA_ANGLE_X;
        private float _targetOrbitRotationY = BASE_CAMERA_ANGLE_Y;

        private float _rotationSpeedMultiplier = 1f;
        private float _movementSpeedMultiplier = 1f;
        private bool _invertX;
        private bool _invertY;

        private void Awake()
        {
            _positionComposer = targetCamera.GetComponent<CinemachinePositionComposer>();
            _confiner3D = targetCamera.GetComponent<CinemachineConfiner3D>();
        }
        
        private void Update()
        {
            HandleInput();
            HandlePanning();
            
            HandleMovement();
            
            HandleCameraZoom();
            HandleCameraCentring();
            HandleCameraOrbit();
        }

        private void HandleInput()
        {
            _moveInput = moveAction.action.ReadValue<Vector2>();
            _zoomInput = zoomAction.action.ReadValue<Vector2>();
            
            Debug.Log(_moveInput);
            
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

            Vector3 moveDirection = transform.forward * _moveInput.y + transform.right * _moveInput.x;
            transform.position += moveDirection * (movementSpeed * Time.deltaTime * _movementSpeedMultiplier);
            
            if (_panDelta != Vector2.zero)
            {
                Vector3 panOffset = transform.right * _panDelta.x + transform.forward * _panDelta.y;
                transform.position += panOffset * Time.deltaTime;
                _panDelta = Vector2.zero;
            }
            
            if (_useBounds)
            {
                Vector3 clampedPosition = transform.position;
                clampedPosition.x = Mathf.Clamp(clampedPosition.x, _currentBounds.min.x, _currentBounds.max.x);
                clampedPosition.z = Mathf.Clamp(clampedPosition.z, _currentBounds.min.z, _currentBounds.max.z);
                transform.position = clampedPosition;
            }
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

            if (CameraZoomBlocked) return;
            
            _targetCameraDistance -= _zoomInput.y * zoomStep;
            _targetCameraDistance = Mathf.Clamp(_targetCameraDistance, minZoom, maxZoom);

            _positionComposer.CameraDistance = Mathf.Lerp(
                _positionComposer.CameraDistance,
                _targetCameraDistance,
                Time.deltaTime * zoomSpeed
            );
        }
        
        private void HandleCameraOrbit()
        {
            if (orbitAction.action.IsPressed())
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

        public void SetCameraRotationSpeed(float newRotationSpeedMultiplier) => _rotationSpeedMultiplier = newRotationSpeedMultiplier;
        public void SetCameraMovementSpeed(float newMovementSpeedMultiplier) => _movementSpeedMultiplier = newMovementSpeedMultiplier;
        public void SetCameraInvertX(bool invertX) => _invertX = invertX;
        public void SetCameraInvertY(bool invertY) => _invertY = invertY;
        public void SetFOV(float fov) => targetCamera.Lens.FieldOfView = fov;
        
        private Bounds _currentBounds;
        private bool _useBounds = false;

        public void SetCameraBounds(Collider boundingCollider)
        {
            _confiner3D.BoundingVolume = boundingCollider;
            _currentBounds = boundingCollider.bounds;
            _useBounds = true; 
        }
    } 
}
