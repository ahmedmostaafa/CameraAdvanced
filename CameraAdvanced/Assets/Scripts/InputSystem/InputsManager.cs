using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputSystem
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputsManager : MonoBehaviour
    {
        public static event Action<string> OnControlsChanged;
        public static event Action OnGamepadDisconnected;

        public const string KEYBOARD_SCHEME = "Keyboard&Mouse";
        public const string GAMEPAD_SCHEME = "Gamepad";
        
        private const string CAMERA_MAP_NAME = "CameraMap";
        
        [SerializeField] private InputActionAsset inputActionAsset;
        private static PlayerInput _playerInput;

        private InputActionMap _cameraMap;
        
        public static string CurrentControlScheme => _playerInput == null ? KEYBOARD_SCHEME : _playerInput.currentControlScheme;
    
        private void Awake()
        {
            UnityEngine.InputSystem.InputSystem.onDeviceChange += OnDeviceChanged;

            _playerInput = GetComponent<PlayerInput>();
            _playerInput.onControlsChanged += ControlsChanged;
            
            InitializeMaps();
        }

        private void InitializeMaps()
        {
            _cameraMap = inputActionAsset.FindActionMap(CAMERA_MAP_NAME);
            SetAllMapsActiveState(true);
        }
        
        private void SetAllMapsActiveState(bool isActive)
        {
            foreach (InputAction inputAction in inputActionAsset.actionMaps.SelectMany(inputActionMap =>
                         inputActionMap.actions))
            {
                SetActionActiveState(inputAction, isActive);
            }
        }

        private void SetActionMapActiveState(InputActionMap inputActionMap, bool isActive)
        {
            if (isActive) inputActionMap.Enable();
            else inputActionMap.Disable();
        }
        
        private void SetActionActiveState(InputAction action, bool isActive)
        {
            Debug.Log(action);
            if(isActive) action.Enable();
            else action.Disable();
        }
        
        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Disconnected)
            {
                Debug.Log("Device Disconnected: " + device.name);
                OnGamepadDisconnected?.Invoke();
            }
            else if (change == InputDeviceChange.Reconnected)
            {
                Debug.Log("Device Reconnected: " + device.name);
            }
        }

        private void ControlsChanged(PlayerInput playerInput)
        {
            OnControlsChanged?.Invoke(playerInput.currentControlScheme);
        }
    
        private void OnDestroy()
        {
            UnityEngine.InputSystem.InputSystem.onDeviceChange -= OnDeviceChanged;
            _playerInput.onControlsChanged -= ControlsChanged;
            
            SetAllMapsActiveState(false);
        }
    }
}
