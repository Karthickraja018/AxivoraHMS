namespace Axivora.Services.Interfaces
{
    public interface IAppointmentTransitionStrategy
    {
        bool CanHandle(string toStatus);
        void Validate(string fromStatus, string toStatus, string callerRole);
    }
}
