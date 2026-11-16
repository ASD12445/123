using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private bool dontDestroyOnLoad = true;

        public WorldTime WorldTime { get; private set; }
        public bool CursorLocked { get; private set; } = true;

        private InputAction _toggleCursorAction;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            WorldTime = GetComponent<WorldTime>();
            if (WorldTime == null)
                WorldTime = gameObject.AddComponent<WorldTime>();

            _toggleCursorAction = new InputAction("ToggleCursor", InputActionType.Button, "<Keyboard>/escape");
            _toggleCursorAction.performed += _ => ToggleCursor();
            _toggleCursorAction.Enable();

            ApplyExposureSettings();
            SetCursorLocked(true);
        }

        private static void ApplyExposureSettings()
        {
            var volume = FindFirstObjectByType<UnityEngine.Rendering.Volume>();
            if (volume == null)
                return;

            var profile = volume.profile;
            if (profile == null)
                return;

            if (profile.TryGet(out UnityEngine.Rendering.Exposure exposure))
            {
                exposure.mode.Override(UnityEngine.Rendering.ExposureMode.Automatic);
                exposure.limitMin.Override(2f);
                exposure.limitMax.Override(14f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_toggleCursorAction != null)
            {
                _toggleCursorAction.Disable();
                _toggleCursorAction.Dispose();
            }
        }

        public void ToggleCursor()
        {
            SetCursorLocked(!CursorLocked);
        }

        public void SetCursorLocked(bool locked)
        {
            CursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
