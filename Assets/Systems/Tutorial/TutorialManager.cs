namespace Systems.Dimension
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class TutorialManager : MonoBehaviour
    {
        [System.Serializable]
        public class TutorialStep
        {
            public string stepName; 
            public GameObject popup;  
            public float startDelay = 2f;  
            public float hideDelay = 2f;
        }

        public List<TutorialStep> tutorialSteps;
        private int _currentStepIndex = 0;
        private bool _tutorialEnabled = true;

        void Start()
        {
            foreach (var step in tutorialSteps)
            {
                step.popup.SetActive(false); 
            }

            if (tutorialSteps.Count > 0)
            {
                StartCoroutine(StartNextStep(tutorialSteps[0].startDelay)); 
            }
        }

        private IEnumerator StartNextStep(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_currentStepIndex < tutorialSteps.Count && _tutorialEnabled)
            {
                TutorialStep step = tutorialSteps[_currentStepIndex];
                step.popup.SetActive(true);
                CompleteCurrentStep();
            }
        }

        public void CompleteCurrentStep()
        {
            if (!_tutorialEnabled || _currentStepIndex >= tutorialSteps.Count)
                return;

            TutorialStep step = tutorialSteps[_currentStepIndex];
            StartCoroutine(HidePopupAndProceed(step));
        }

        private IEnumerator HidePopupAndProceed(TutorialStep step)
        {
            yield return new WaitForSeconds(step.hideDelay);
            step.popup.SetActive(false);
            
            _currentStepIndex++;
            if (_currentStepIndex < tutorialSteps.Count)
            {
                StartCoroutine(StartNextStep(tutorialSteps[_currentStepIndex].startDelay));
            }
        }

        public void DisableTutorial()
        {
            _tutorialEnabled = false;
            foreach (var step in tutorialSteps)
            {
                step.popup.SetActive(false);
            }
        }
    }
}