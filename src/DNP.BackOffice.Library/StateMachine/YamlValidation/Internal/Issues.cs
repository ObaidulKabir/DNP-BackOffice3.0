namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

internal static class Issues
{
    internal static ValidationIssue SyntaxInvalid(string section, string location) =>
        new(
            "SMV-001",
            ValidationSeverity.Error,
            section,
            location,
            "Invalid YAML syntax",
            "Fix indentation or missing colon");

    internal static ValidationIssue DuplicateKey(string section, string key) =>
        new(
            "SMV-002",
            ValidationSeverity.Error,
            section,
            key,
            "Duplicate key detected",
            "Ensure all keys are unique");

    internal static ValidationIssue MissingSection(string section, string missingSection, string suggestion) =>
        new(
            "SMV-101",
            ValidationSeverity.Error,
            section,
            section,
            $"Missing required section '{missingSection}'",
            suggestion);

    internal static ValidationIssue InvalidSectionType(string section, string location, string message, string suggestion) =>
        new(
            "SMV-102",
            ValidationSeverity.Error,
            section,
            location,
            message,
            suggestion);

    internal static ValidationIssue MissingMetaField(string field) =>
        new(
            "SMV-103",
            ValidationSeverity.Error,
            "meta",
            "machine.meta",
            $"Missing required field '{field}'",
            field == "version" ? "Add semantic version (e.g. 1.0.0)" : $"Add required field '{field}'");

    internal static ValidationIssue UndefinedState(string location, string state) =>
        new(
            "SMV-201",
            ValidationSeverity.Error,
            "transitions",
            location,
            $"State '{state}' is not defined",
            "Define the state or correct the name");

    internal static ValidationIssue UndefinedGuard(string location, string guard) =>
        new(
            "SMV-202",
            ValidationSeverity.Error,
            "transitions",
            location,
            $"Guard '{guard}' not found",
            "Define the guard or remove the reference");

    internal static ValidationIssue UndefinedEvent(string location, string @event) =>
        new(
            "SMV-203",
            ValidationSeverity.Error,
            "transitions",
            location,
            $"Event '{@event}' is not declared",
            "Declare the event under 'machine.events'");

    internal static ValidationIssue InvalidInitial(string message, string suggestion) =>
        new(
            "SMV-301",
            ValidationSeverity.Error,
            "initial",
            "machine.initial",
            message,
            suggestion);

    internal static ValidationIssue UnreachableState(string state) =>
        new(
            "SMV-302",
            ValidationSeverity.Error,
            "states",
            $"machine.states.{state}",
            $"State '{state}' is unreachable",
            "Add a transition that leads to this state");

    internal static ValidationIssue DeadEndState(string state) =>
        new(
            "SMV-303",
            ValidationSeverity.Error,
            "states",
            $"machine.states.{state}",
            $"State '{state}' has no outgoing transitions",
            "Add a transition or mark as terminal");

    internal static ValidationIssue StateSkipping(string location, string skipped) =>
        new(
            "SMV-304",
            ValidationSeverity.Error,
            "transitions",
            location,
            $"Transition skips intermediate state '{skipped}'",
            "Add intermediate transition explicitly");

    internal static ValidationIssue CircularTransition() =>
        new(
            "SMV-305",
            ValidationSeverity.Error,
            "transitions",
            "machine.transitions",
            "Circular transition detected between states",
            "Mark cycle explicitly or redesign flow");

    internal static ValidationIssue InvalidGuardExpression(string location, string message, string suggestion) =>
        new(
            "SMV-401",
            ValidationSeverity.Error,
            "guards",
            location,
            message,
            suggestion);

    internal static ValidationIssue UnknownContextProperty(string location, string property) =>
        new(
            "SMV-402",
            ValidationSeverity.Error,
            "guards",
            location,
            $"Context property '{property}' not recognized",
            "Use a valid context property");

    internal static ValidationIssue InvalidInvariant(string location, string message, string suggestion) =>
        new(
            "SMV-403",
            ValidationSeverity.Error,
            "invariants",
            location,
            message,
            suggestion);

    internal static ValidationIssue TerminalOutgoing(string location, string terminalState) =>
        new(
            "SMV-501",
            ValidationSeverity.Error,
            "transitions",
            location,
            $"Terminal state '{terminalState}' has outgoing transition",
            "Remove transition or explicitly allow override");

    internal static ValidationIssue NoTerminalDefined() =>
        new(
            "SMV-502",
            ValidationSeverity.Error,
            "terminal",
            "machine.terminal",
            "No terminal state defined",
            "Declare at least one terminal state");

    internal static ValidationIssue NoPathToTerminal() =>
        new(
            "SMV-601",
            ValidationSeverity.Error,
            "states",
            "machine.initial",
            "Initial state cannot reach any terminal state",
            "Add transitions leading to terminal state");

    internal static ValidationIssue NoBlockingInvariants() =>
        new(
            "SMV-602",
            ValidationSeverity.Error,
            "invariants",
            "machine.invariants",
            "No blocking invariants defined",
            "Add at least one global safety invariant");

    internal static ValidationIssue VersionNotUpdated() =>
        new(
            "SMV-603",
            ValidationSeverity.Error,
            "meta",
            "machine.meta.version",
            "Behavior change without version increment",
            "Increment version number");

    internal static ValidationIssue UnusedStateWarning(string state) =>
        new(
            "SMV-W001",
            ValidationSeverity.Warning,
            "states",
            $"machine.states.{state}",
            $"State '{state}' is unused",
            "Remove the state or add transitions that reference it");

    internal static ValidationIssue UnusedGuardWarning(string guard) =>
        new(
            "SMV-W002",
            ValidationSeverity.Warning,
            "guards",
            $"machine.guards.{guard}",
            $"Guard '{guard}' is unused",
            "Remove the guard or reference it from a transition");

    internal static ValidationIssue UnusedEventWarning(string @event) =>
        new(
            "SMV-W003",
            ValidationSeverity.Warning,
            "events",
            $"machine.events.{@event}",
            $"Event '{@event}' is unused",
            "Remove the event or emit it from a transition");
}

