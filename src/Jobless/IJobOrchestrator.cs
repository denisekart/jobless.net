namespace Jobless;

public interface IJobOrchestrator
{
    Task<RegistrationResult> Registering(IJobDefinition jobDefinition, CancellationToken cancellationToken);
    Task<Result> CanTransition(IJobDefinition message, JobStateTransition stateTransition, CancellationToken cancellationToken);
    Task<TransitionResult> Transitioning(IJobDefinition jobDefinition, JobStateTransition stateTransition, CancellationToken cancellationToken);
}