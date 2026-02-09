using System.Linq;
using System.Threading.Tasks;
using Ami.BroAudio;
using HelloDev.Objectives;
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
        [SerializeField]
        SoundID startTutorialSound, stepStartedSound, stepCompletedSound, tutorialCompletedSound, updateCounterSound;

        public void ReceiveContext(GameContext context)
        {
        }

        public bool SelfInitialize { get; set; } = false;
        public bool IsInitialized { get; }

        public Task InitializeAsync()
        {
            TutorialManager.Instance.OnStepCompleted.SafeSubscribe(OnStepCompleted);
            TutorialManager.Instance.OnStepStarted.SafeSubscribe(OnStepStarted);
            return Task.CompletedTask;
        }

        private void OnStepStarted(TutorialRuntime tutorialRuntime, TutorialStepRuntime stepRuntime)
        {
            if (stepRuntime.HasSubsteps)
            {
                stepRuntime.OnSubstepCompleted.SafeSubscribe(OnSubstepCompleted);
            }
        }

        private void OnSubstepCompleted(TutorialStepRuntime step, TutorialSubstep_SO subStepSO)
        {
            PlayStepCompletedSound();
        }

        private void OnStepCompleted(TutorialRuntime tutorialRuntime, TutorialStepRuntime stepRuntime)
        {
            if (tutorialRuntime.Steps.Last().CurrentState != ObjectiveState.Completed)
            {
                PlayStepCompletedSound();
            }

            if (stepRuntime.HasSubsteps) stepRuntime.OnSubstepCompleted.SafeUnsubscribe(OnSubstepCompleted);
        }

        private void PlaySound(SoundID sound)
        {
            BroAudio.Play(sound);
        }

        public void PlayTutorialStartedSound()
        {
            PlaySound(startTutorialSound);
        }

        public void PlayTutorialCompletedSound()
        {
            PlaySound(tutorialCompletedSound);
        }

        public void PlayStepCompletedSound()
        {
            PlaySound(stepCompletedSound);
        }

        public void PlayStepStartedSound()
        {
            PlaySound(stepStartedSound);
        }

        public void PlayUpdateCounterSound()
        {
            PlaySound(updateCounterSound);
        }

        public void Shutdown()
        {
        }
    }
}