namespace Axivora.Services.Interfaces
{
    public interface IAppointmentTransitionValidator
    {
        void ValidateTransition(string fromStatus, string toStatus, string callerRole);
    }
}
