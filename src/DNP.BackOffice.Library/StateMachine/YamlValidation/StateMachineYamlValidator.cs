using System.Collections.Frozen;

namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

public sealed class StateMachineYamlValidator : IStateMachineYamlValidator
{
    private static readonly FrozenSet<string> KnownContextProperties = new[]
    {
        "paymentStatus",
        "artworkStatus",
        "orderCancelled",
        "retryCount",
        "maxRetry"
    }.ToFrozenSet(StringComparer.Ordinal);

    public ValidationResult Validate(string yamlText, StateMachineYamlValidationBaseline? baseline = null)
    {
        var parseOutcome = DslParser.TryParse(yamlText);
        if (parseOutcome.Error is not null)
        {
            return new ValidationResult(new[] { parseOutcome.Error });
        }

        var structureOutcome = StructuralValidation.TryBuildConfiguration(parseOutcome.Root!);
        if (structureOutcome.Error is not null)
        {
            return new ValidationResult(new[] { structureOutcome.Error });
        }

        var configuration = structureOutcome.Configuration!;

        var referentialError = ReferentialValidation.TryValidate(configuration);
        if (referentialError is not null)
        {
            return new ValidationResult(new[] { referentialError });
        }

        var logicalError = LogicalValidation.TryValidate(configuration);
        if (logicalError is not null)
        {
            return new ValidationResult(new[] { logicalError });
        }

        var guardInvariantError = GuardInvariantValidation.TryValidate(configuration, KnownContextProperties);
        if (guardInvariantError is not null)
        {
            return new ValidationResult(new[] { guardInvariantError });
        }

        var safetyError = SafetyValidation.TryValidate(configuration, baseline);
        if (safetyError is not null)
        {
            return new ValidationResult(new[] { safetyError });
        }

        var warnings = WarningValidation.GetWarnings(configuration);
        return new ValidationResult(warnings);
    }
}
