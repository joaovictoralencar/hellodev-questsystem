using HelloDev.QuestSystem.Tutorials;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelloDev.QuestSystem.BasicTutorialExample
{
    /// <summary>
    /// Simple test script to start a tutorial by pressing a key.
    /// Use this for testing tutorials without setting up triggers.
    /// </summary>
    public class TutorialTestStarter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Tutorial")]
        [SerializeField, Tooltip("The tutorial to start when the key is pressed.")]
        private Tutorial_SO tutorial;

        [Header("Debug")]
        [SerializeField, Tooltip("Log messages to console.")]
        private bool logMessages = true;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                StartTutorial();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the assigned tutorial.
        /// Can be called from UI buttons or other scripts.
        /// </summary>
        public void StartTutorial()
        {
            if (tutorial == null)
            {
                Debug.LogWarning("[TutorialTestStarter] No tutorial assigned.");
                return;
            }

            if (TutorialManager.Instance == null)
            {
                Debug.LogWarning("[TutorialTestStarter] TutorialManager.Instance is null. Add a TutorialManager to the scene.");
                return;
            }

            if (logMessages)
                Debug.Log($"[TutorialTestStarter] Starting tutorial: {tutorial.DevName}");

            TutorialManager.Instance.StartTutorial(tutorial);
        }

        #endregion
    }
}
