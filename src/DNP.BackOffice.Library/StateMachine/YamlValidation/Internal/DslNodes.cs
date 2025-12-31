namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

internal abstract record DslNode;

internal sealed record DslScalar(string Value) : DslNode;

internal sealed record DslSequence(List<DslNode> Items) : DslNode;

internal sealed record DslMapping(Dictionary<string, DslNode> Children) : DslNode;

internal sealed record DslPlaceholder : DslNode
{
    public static readonly DslPlaceholder Instance = new();
}

