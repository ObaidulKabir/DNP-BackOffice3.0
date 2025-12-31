namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

public interface IStateMachineYamlValidator
{
    ValidationResult Validate(string yamlText, StateMachineYamlValidationBaseline? baseline = null);
}

