namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

public sealed record ValidationIssue(
    string ErrorCode,
    ValidationSeverity Severity,
    string Section,
    string Location,
    string Message,
    string Suggestion);

