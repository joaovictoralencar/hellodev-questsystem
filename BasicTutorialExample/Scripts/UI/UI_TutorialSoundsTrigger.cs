using System.Threading.Tasks;
using Ami.BroAudio;
using HelloDev.QuestSystem.Tutorials;
using HelloDev.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HelloDev.QuestSystem.BasicTutorialExample
{
    public class UI_TutorialSoundsTrigger : MonoBehaviour, IBootstrapInitializable
    {
#if ODIN_INSPECTOR
        [TabGroup("Sounds")]
#else
        [Header("Sounds")]
#endif
        [SerializeField] SoundID startTutorialSound, showPanelSound, hidePanelSound, stepStartedSound, stepCompletedSound, tutorialCompletedSound;

        public void ReceiveContext(GameContext context)
        {
        }

        public bool SelfInitialize { get; set; } = false;
        public bool IsInitialized { get; }

        public Task InitializeAsync()
        {
            TutorialManager.Instance.OnTutorialStarted.SafeSubscribe(HandleTutorialStarted);
            TutorialManager.Instance.OnTutorialCompleted.SafeSubscribe(HandleTutorialCompleted);
            TutorialManager.Instance.OnStepStarted.SafeSubscribe(HandleStepStarted);
            TutorialManager.Instance.OnStepCompleted.SafeSubscribe(HandleStepCompleted);
            return Task.CompletedTask;
        }

        private void HandleStepCompleted(TutorialRuntime tutorialRuntime, TutorialStepRuntime stepRuntime)
        {
            BroAudio.Play(stepCompletedSound);
        }

        private void HandleTutorialCompleted(TutorialRuntime tutorialRuntime)
        {
            BroAudio.Play(tutorialCompletedSound);
        }

        private void HandleStepStarted(TutorialRuntime tutorialRuntime, TutorialStepRuntime tutorialStepRuntime)
        {
            BroAudio.Play(stepStartedSound);
        }

        private void HandleTutorialStarted(TutorialRuntime tutorialRuntime)
        {
            BroAudio.Play(stepCompletedSound);
        }

        public void Shutdown()
        {
            BroAudio.Play(stepCompletedSound);
        }

    }
}
