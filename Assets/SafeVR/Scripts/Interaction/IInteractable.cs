using UnityEngine;

namespace SafeVR.Interaction
{
    /// <summary>
    /// Core interface for any object in SafeVR that can be hovered or interacted with.
    /// Decoupled from specific input devices (Desktop mouse/keyboard, VR hand controllers, AR touch).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Returns the action prompt shown on the player's HUD or VR pointer (e.g. "Inspect Fire Extinguisher").
        /// </summary>
        string GetPromptText();

        /// <summary>
        /// True if this object can currently be interacted with.
        /// </summary>
        bool IsInteractable { get; }

        /// <summary>
        /// Called when an interactor (desktop ray, VR hand, AR pointer) starts targeting this object.
        /// </summary>
        void OnHoverEnter(Interactor interactor);

        /// <summary>
        /// Called when an interactor stops targeting this object.
        /// </summary>
        void OnHoverExit(Interactor interactor);

        /// <summary>
        /// Called when the player triggers the interaction action.
        /// </summary>
        void OnInteract(Interactor interactor);
    }
}
