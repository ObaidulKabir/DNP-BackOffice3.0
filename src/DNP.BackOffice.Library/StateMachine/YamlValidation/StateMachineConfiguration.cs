namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

public sealed record StateMachineConfiguration(
    StateMachineMeta Meta,
    IReadOnlyDictionary<string, StateDefinition> States,
    string InitialState,
    IReadOnlySet<string> TerminalStates,
    IReadOnlyList<TransitionDefinition> Transitions,
    IReadOnlyDictionary<string, GuardDefinition> Guards,
    IReadOnlyDictionary<string, InvariantDefinition> Invariants,
    IReadOnlyDictionary<string, EventDefinition> Events);

public sealed record StateMachineMeta(
    string Id,
    string Version,
    string Domain,
    string Description);

public sealed record StateDefinition(
    string Id,
    string Type,
    string Label);

public sealed record TransitionDefinition(
    string From,
    string To,
    string On,
    IReadOnlyList<string> Guards,
    IReadOnlyList<string> Emit);

public sealed record GuardDefinition(
    string Id,
    string When,
    string Message);

public sealed record InvariantDefinition(
    string Id,
    string When,
    string Severity,
    string Message);

public sealed record EventDefinition(
    string Id,
    IReadOnlyList<string> Payload);

