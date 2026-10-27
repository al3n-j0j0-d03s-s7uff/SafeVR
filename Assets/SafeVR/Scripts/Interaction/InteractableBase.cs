using UnityEngine;
using UnityEngine.Events;

namespace SafeVR.Interaction
{
    /// <summary>
    /// Base class for interactive objects in SafeVR.
    /// Provides visual feedback (material highlight / color change) and UnityEvents.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Interaction Details")]
        [SerializeField] private string promptMessage = "Interact";
        [SerializeField] private bool isInteractable = true;
        [SerializeField] private bool singleInteractionOnly = false;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color highlightColor = new Color(0.2f, 0.9f, 0.4f, 1f);
        [SerializeField] private bool useEmissionHighlight = true;

        [Header("Events")]
        public UnityEvent onHoverEnter;
        public UnityEvent onHoverExit;
        public UnityEvent onInteract;

        private Material[] originalMaterials;
        private Material[] runtimeMaterials;
        private bool hasInteracted = false;
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        public virtual string GetPromptText()
        {
            return promptMessage;
        }

        public virtual bool IsInteractable
        {
            get => isInteractable && (!singleInteractionOnly || !hasInteracted);
            set => isInteractable = value;
        }

        protected virtual void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                // Create instance copies of materials so highlights don't affect shared project assets
                runtimeMaterials = targetRenderer.materials;
            }
        }

        public virtual void OnHoverEnter(Interactor interactor)
        {
            ApplyHighlight(true);
            onHoverEnter?.Invoke();
        }

        public virtual void OnHoverExit(Interactor interactor)
        {
            ApplyHighlight(false);
            onHoverExit?.Invoke();
        }

        public virtual void OnInteract(Interactor interactor)
        {
            if (!IsInteractable) return;

            hasInteracted = true;
            onInteract?.Invoke();

            if (singleInteractionOnly)
            {
                ApplyHighlight(false);
            }
        }

        protected void SetPrompt(string newPrompt)
        {
            promptMessage = newPrompt;
        }

        protected virtual void ApplyHighlight(bool active)
        {
            if (runtimeMaterials == null || runtimeMaterials.Length == 0) return;

            foreach (var mat in runtimeMaterials)
            {
                if (mat == null) continue;

                if (useEmissionHighlight && mat.HasProperty(EmissionColorProperty))
                {
                    if (active)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor(EmissionColorProperty, highlightColor * 1.5f);
                    }
                    else
                    {
                        mat.SetColor(EmissionColorProperty, Color.black);
                    }
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (runtimeMaterials != null)
            {
                foreach (var mat in runtimeMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
            }
        }
    }
}
