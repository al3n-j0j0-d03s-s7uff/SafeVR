using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeVR.Interaction;

namespace SafeVR.Player
{
    /// <summary>
    /// Displays a clean center reticle / crosshair and contextual interaction prompt.
    /// Updates dynamically when Interactor hovers over IInteractable objects.
    /// </summary>
    public class ReticleHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Interactor interactor;
        [SerializeField] private Image reticleDot;
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private TMP_Text promptText;

        [Header("Styling")]
        [SerializeField] private Color defaultDotColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color activeDotColor = new Color(0.2f, 0.9f, 0.5f, 1f);
        [SerializeField] private Vector3 normalDotScale = Vector3.one;
        [SerializeField] private Vector3 activeDotScale = new Vector3(1.5f, 1.5f, 1.5f);

        private void Awake()
        {
            if (interactor == null)
            {
                interactor = FindFirstObjectByType<Interactor>();
            }
        }

        private void OnEnable()
        {
            if (interactor != null)
            {
                interactor.OnHoverChanged += HandleHoverChanged;
            }
            SetPromptVisible(false);
        }

        private void OnDisable()
        {
            if (interactor != null)
            {
                interactor.OnHoverChanged -= HandleHoverChanged;
            }
        }

        private void HandleHoverChanged(IInteractable interactable)
        {
            if (interactable != null && interactable.IsInteractable)
            {
                SetPromptVisible(true);
                if (promptText != null)
                {
                    promptText.text = interactable.GetPromptText();
                }

                if (reticleDot != null)
                {
                    reticleDot.color = activeDotColor;
                    reticleDot.transform.localScale = activeDotScale;
                }
            }
            else
            {
                SetPromptVisible(false);
                if (reticleDot != null)
                {
                    reticleDot.color = defaultDotColor;
                    reticleDot.transform.localScale = normalDotScale;
                }
            }
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptPanel != null)
            {
                promptPanel.SetActive(visible);
            }
        }
    }
}
