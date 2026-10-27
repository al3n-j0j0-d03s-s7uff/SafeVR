namespace SafeVR.Interaction
{
    public interface IHoldInteractable : IInteractable
    {
        float HoldDuration { get; } // required hold time to complete
        float CurrentHoldProgress { get; }
        bool IsHolding { get; }
        void OnHoldStart(Interactor interactor);
        void OnHoldTick(Interactor interactor, float deltaTime);
        void OnHoldEnd(Interactor interactor, bool completed);
        void OnHoldCancel(Interactor interactor);
    }
}
