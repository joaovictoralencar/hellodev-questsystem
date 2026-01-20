using HelloDev.QuestSystem.Tutorials;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicTutorialExample.UI
{
    /// <summary>
    /// Triggers a tutorial when the player enters a trigger zone.
    /// Attach to a GameObject with a Collider set to IsTrigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialTrigger : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Tutorial")]
        [PropertyOrder(0)]
        [Required("Tutorial reference is required.")]
#else
        [Header("Tutorial")]
#endif
        [SerializeField] private Tutorial_SO tutorial;

#if ODIN_INSPECTOR
        [TitleGroup("Trigger Settings")]
        [PropertyOrder(10)]
#else
        [Header("Trigger Settings")]
#endif
        [SerializeField] private string playerTag = "Player";

#if ODIN_INSPECTOR
        [TitleGroup("Trigger Settings")]
        [PropertyOrder(11)]
#endif
        [SerializeField] private bool triggerOnce = true;

#if ODIN_INSPECTOR
        [TitleGroup("Trigger Settings")]
        [PropertyOrder(12)]
#endif
        [SerializeField] private bool disableAfterTrigger = true;

#if ODIN_INSPECTOR
        [TitleGroup("Debug")]
        [PropertyOrder(20)]
#else
        [Header("Debug")]
#endif
        [SerializeField] private bool logTriggerEvents;

        #endregion

        #region Private Fields

        private bool _hasTriggered;
        private Collider _collider;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the tutorial associated with this trigger.
        /// </summary>
        public Tutorial_SO Tutorial => tutorial;

        /// <summary>
        /// Gets whether this trigger has already fired.
        /// </summary>
        public bool HasTriggered => _hasTriggered;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _collider = GetComponent<Collider>();

            // Ensure collider is set as trigger
            if (_collider != null && !_collider.isTrigger)
            {
                QuestLogger.LogWarning(LogSubsystem.Tutorial,
                    $"[TutorialTrigger] Collider on '{gameObject.name}' is not set as trigger. Setting isTrigger = true.");
                _collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanTrigger(other)) return;

            TriggerTutorial();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CanTrigger2D(other)) return;

            TriggerTutorial();
        }

        #endregion

        #region Private Methods - Validation

        private bool CanTrigger(Collider other)
        {
            if (tutorial == null) return false;
            if (triggerOnce && _hasTriggered) return false;
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return false;

            return true;
        }

        private bool CanTrigger2D(Collider2D other)
        {
            if (tutorial == null) return false;
            if (triggerOnce && _hasTriggered) return false;
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return false;

            return true;
        }

        #endregion

        #region Private Methods - Trigger

        private void TriggerTutorial()
        {
            if (TutorialManager.Instance == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Tutorial,
                    "[TutorialTrigger] TutorialManager.Instance is null. Cannot start tutorial.");
                return;
            }

            // Check if already completed (for PlayOnce tutorials)
            if (TutorialManager.Instance.IsTutorialCompleted(tutorial.TutorialId))
            {
                if (logTriggerEvents)
                    QuestLogger.Log(LogSubsystem.Tutorial,
                        $"[TutorialTrigger] Tutorial '{tutorial.DevName}' already completed. Skipping.");

                HandlePostTrigger();
                return;
            }

            // Start the tutorial
            TutorialRuntime runtime = TutorialManager.Instance.StartTutorial(tutorial);

            if (runtime != null)
            {
                if (logTriggerEvents)
                    QuestLogger.Log(LogSubsystem.Tutorial,
                        $"[TutorialTrigger] Started tutorial '{tutorial.DevName}'.");

                HandlePostTrigger();
            }
            else
            {
                if (logTriggerEvents)
                    QuestLogger.Log(LogSubsystem.Tutorial,
                        $"[TutorialTrigger] Failed to start tutorial '{tutorial.DevName}'. May be already active or queued.");
            }
        }

        private void HandlePostTrigger()
        {
            _hasTriggered = true;

            if (disableAfterTrigger)
                gameObject.SetActive(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the trigger so it can fire again.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        /// <summary>
        /// Manually triggers the tutorial without requiring collision.
        /// </summary>
        public void TriggerManually()
        {
            if (triggerOnce && _hasTriggered) return;

            TriggerTutorial();
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();

            // Draw trigger area
            Gizmos.color = _hasTriggered ? Color.gray : Color.cyan;

            if (_collider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (_collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw label
            if (tutorial != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.5f,
                    $"Tutorial: {tutorial.DevName}");
            }
        }
#endif

        #endregion
    }
}
