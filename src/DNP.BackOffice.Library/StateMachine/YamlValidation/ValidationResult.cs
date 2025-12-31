namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

public sealed record ValidationResult(IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsValid => Issues.All(i => i.Severity != ValidationSeverity.Error);

    public static ValidationResult Success() => new(Array.Empty<ValidationIssue>());
}

