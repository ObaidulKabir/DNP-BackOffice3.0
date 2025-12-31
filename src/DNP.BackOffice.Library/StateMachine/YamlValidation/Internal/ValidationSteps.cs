using System.Collections.Frozen;

namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

internal static class StructuralValidation
{
    internal sealed record BuildOutcome(StateMachineConfiguration? Configuration, ValidationIssue? Error);

    internal static BuildOutcome TryBuildConfiguration(DslMapping root)
    {
        if (!root.Children.TryGetValue("machine", out var machineNode) || machineNode is not DslMapping machine)
        {
            return new BuildOutcome(null, Issues.MissingSection("root", "machine", "Add 'machine' root section"));
        }

        if (!TryGetMapping(machine, "meta", out var metaNode, out var metaMissingError))
        {
            return new BuildOutcome(null, metaMissingError);
        }

        var meta = metaNode!;

        if (!TryGetScalar(meta, "id", out var metaId))
        {
            return new BuildOutcome(null, Issues.MissingMetaField("id"));
        }

        if (!TryGetScalar(meta, "version", out var metaVersion))
        {
            return new BuildOutcome(null, Issues.MissingMetaField("version"));
        }

        if (!TryGetScalar(meta, "domain", out var metaDomain))
        {
            return new BuildOutcome(null, Issues.MissingMetaField("domain"));
        }

        if (!TryGetScalar(meta, "description", out var metaDescription))
        {
            return new BuildOutcome(null, Issues.MissingMetaField("description"));
        }

        if (!TryGetMapping(machine, "states", out var statesNode, out var statesMissingError))
        {
            return new BuildOutcome(null, statesMissingError);
        }

        var states = new Dictionary<string, StateDefinition>(StringComparer.Ordinal);
        foreach (var (stateId, stateNode) in statesNode!.Children)
        {
            if (stateNode is not DslMapping stateMap)
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("states", $"machine.states.{stateId}", "State must be a mapping", "Use an indented mapping with 'type'"));
            }

            if (!TryGetScalar(stateMap, "type", out var typeValue))
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("states", $"machine.states.{stateId}", "State is missing 'type'", "Add 'type normal|terminal|exception'"));
            }

            _ = TryGetScalar(stateMap, "label", out var labelValue);
            states[stateId] = new StateDefinition(stateId, typeValue!, labelValue ?? stateId);
        }

        if (!TryGetScalar(machine, "initial", out var initialState))
        {
            return new BuildOutcome(null, Issues.MissingSection("machine", "initial", "Add 'initial <stateId>'"));
        }

        if (!machine.Children.TryGetValue("transitions", out var transitionsNode))
        {
            return new BuildOutcome(null, Issues.MissingSection("machine", "transitions", "Add 'transitions' list"));
        }

        if (transitionsNode is not DslSequence transitionsSeq)
        {
            return new BuildOutcome(null, Issues.InvalidSectionType("transitions", "machine.transitions", "Transitions must be a list", "Use '-' list items"));
        }

        var transitions = new List<TransitionDefinition>();
        for (var i = 0; i < transitionsSeq.Items.Count; i++)
        {
            if (transitionsSeq.Items[i] is not DslMapping transitionMap)
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("transitions", $"machine.transitions[{i}]", "Transition must be a mapping", "Use 'from', 'to', 'on' keys"));
            }

            if (!TryGetScalar(transitionMap, "from", out var fromState) ||
                !TryGetScalar(transitionMap, "to", out var toState) ||
                !TryGetScalar(transitionMap, "on", out var onEvent))
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("transitions", $"machine.transitions[{i}]", "Transition missing required keys", "Add 'from', 'to', 'on'"));
            }

            _ = TryGetScalar(transitionMap, "guards", out var guardsRaw);
            _ = TryGetScalar(transitionMap, "emit", out var emitRaw);

            transitions.Add(new TransitionDefinition(
                fromState!,
                toState!,
                onEvent!,
                ParseBracketList(guardsRaw),
                ParseBracketList(emitRaw)));
        }

        var terminalStates = new HashSet<string>(StringComparer.Ordinal);
        if (machine.Children.TryGetValue("terminal", out var terminalNode))
        {
            if (terminalNode is not DslSequence terminalSeq)
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("terminal", "machine.terminal", "Terminal must be a list", "Use '-' list items"));
            }

            foreach (var item in terminalSeq.Items)
            {
                if (item is DslScalar s)
                {
                    terminalStates.Add(s.Value);
                }
            }
        }

        var guards = new Dictionary<string, GuardDefinition>(StringComparer.Ordinal);
        if (machine.Children.TryGetValue("guards", out var guardsNode))
        {
            if (guardsNode is not DslMapping guardsMap)
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("guards", "machine.guards", "Guards must be a mapping", "Define guards under 'guards'"));
            }

            foreach (var (guardId, guardNode) in guardsMap.Children)
            {
                if (guardNode is not DslMapping guardMap)
                {
                    return new BuildOutcome(null, Issues.InvalidSectionType("guards", $"machine.guards.{guardId}", "Guard must be a mapping", "Add 'when' and 'message'"));
                }

                _ = TryGetScalar(guardMap, "when", out var whenValue);
                _ = TryGetScalar(guardMap, "message", out var messageValue);
                guards[guardId] = new GuardDefinition(guardId, whenValue ?? string.Empty, messageValue ?? string.Empty);
            }
        }

        var invariants = new Dictionary<string, InvariantDefinition>(StringComparer.Ordinal);
        if (machine.Children.TryGetValue("invariants", out var invariantsNode))
        {
            if (invariantsNode is not DslMapping invariantsMap)
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("invariants", "machine.invariants", "Invariants must be a mapping", "Define invariants under 'invariants'"));
            }

            foreach (var (invariantId, invariantNode) in invariantsMap.Children)
            {
                if (invariantNode is not DslMapping invariantMap)
                {
                    return new BuildOutcome(null, Issues.InvalidSectionType("invariants", $"machine.invariants.{invariantId}", "Invariant must be a mapping", "Add 'when', 'severity', 'message'"));
                }

                _ = TryGetScalar(invariantMap, "when", out var whenValue);
                _ = TryGetScalar(invariantMap, "severity", out var severityValue);
                _ = TryGetScalar(invariantMap, "message", out var messageValue);
                invariants[invariantId] = new InvariantDefinition(invariantId, whenValue ?? string.Empty, severityValue ?? string.Empty, messageValue ?? string.Empty);
            }
        }

        var events = new Dictionary<string, EventDefinition>(StringComparer.Ordinal);
        if (machine.Children.TryGetValue("events", out var eventsNode))
        {
            if (eventsNode is not DslMapping eventsMap)
            {
                return new BuildOutcome(null, Issues.InvalidSectionType("events", "machine.events", "Events must be a mapping", "Define events under 'events'"));
            }

            foreach (var (eventId, eventNode) in eventsMap.Children)
            {
                if (eventNode is not DslMapping eventMap)
                {
                    return new BuildOutcome(null, Issues.InvalidSectionType("events", $"machine.events.{eventId}", "Event must be a mapping", "Add 'payload'"));
                }

                _ = TryGetScalar(eventMap, "payload", out var payloadRaw);
                events[eventId] = new EventDefinition(eventId, ParseBracketList(payloadRaw));
            }
        }

        var configuration = new StateMachineConfiguration(
            new StateMachineMeta(metaId!, metaVersion!, metaDomain!, metaDescription!),
            states,
            initialState!,
            terminalStates,
            transitions,
            guards,
            invariants,
            events);

        return new BuildOutcome(configuration, null);
    }

    private static bool TryGetMapping(DslMapping parent, string key, out DslMapping? mapping, out ValidationIssue? error)
    {
        mapping = null;
        error = null;

        if (!parent.Children.TryGetValue(key, out var node))
        {
            error = Issues.MissingSection("machine", key, $"Add '{key}' section");
            return false;
        }

        if (node is not DslMapping m)
        {
            error = Issues.InvalidSectionType(key, $"machine.{key}", $"Section '{key}' must be a mapping", "Fix YAML structure");
            return false;
        }

        mapping = m;
        return true;
    }

    private static bool TryGetScalar(DslMapping parent, string key, out string? value)
    {
        value = null;
        if (!parent.Children.TryGetValue(key, out var node) || node is not DslScalar s)
        {
            return false;
        }

        value = s.Value;
        return true;
    }

    internal static IReadOnlyList<string> ParseBracketList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        var text = raw.Trim();
        if (text.StartsWith("[", StringComparison.Ordinal))
        {
            text = text[1..];
        }

        if (text.EndsWith("]", StringComparison.Ordinal))
        {
            text = text[..^1];
        }

        var tokens = text
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        return tokens;
    }
}

internal static class ReferentialValidation
{
    internal static ValidationIssue? TryValidate(StateMachineConfiguration configuration)
    {
        foreach (var (transition, index) in configuration.Transitions.Select((t, i) => (t, i)))
        {
            if (!configuration.States.ContainsKey(transition.From))
            {
                return Issues.UndefinedState($"machine.transitions[{index}].from", transition.From);
            }

            if (!configuration.States.ContainsKey(transition.To))
            {
                return Issues.UndefinedState($"machine.transitions[{index}].to", transition.To);
            }

            foreach (var guard in transition.Guards)
            {
                if (!configuration.Guards.ContainsKey(guard))
                {
                    return Issues.UndefinedGuard($"machine.transitions[{index}].guards", guard);
                }
            }

            foreach (var emitted in transition.Emit)
            {
                if (!configuration.Events.ContainsKey(emitted))
                {
                    return Issues.UndefinedEvent($"machine.transitions[{index}].emit", emitted);
                }
            }
        }

        return null;
    }
}

internal static class LogicalValidation
{
    internal static ValidationIssue? TryValidate(StateMachineConfiguration configuration)
    {
        if (!configuration.States.ContainsKey(configuration.InitialState))
        {
            return Issues.InvalidInitial("Initial state is not defined", "Define the initial state in 'states'");
        }

        if (configuration.TerminalStates.Contains(configuration.InitialState))
        {
            return Issues.InvalidInitial("Initial state cannot be terminal", "Choose a non-terminal initial state");
        }

        if (configuration.TerminalStates.Count == 0)
        {
            return Issues.NoTerminalDefined();
        }

        foreach (var transition in configuration.Transitions)
        {
            if (configuration.TerminalStates.Contains(transition.From))
            {
                return Issues.TerminalOutgoing("machine.transitions", transition.From);
            }
        }

        var referencedStates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in configuration.Transitions)
        {
            referencedStates.Add(t.From);
            referencedStates.Add(t.To);
        }

        var reachable = ReachableFromInitial(configuration);
        foreach (var (stateId, _) in configuration.States)
        {
            if (string.Equals(stateId, configuration.InitialState, StringComparison.Ordinal))
            {
                continue;
            }

            if (configuration.TerminalStates.Contains(stateId))
            {
                continue;
            }

            if (!referencedStates.Contains(stateId))
            {
                continue;
            }

            if (!reachable.Contains(stateId))
            {
                return Issues.UnreachableState(stateId);
            }
        }

        var outgoingByState = configuration.Transitions
            .GroupBy(t => t.From, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var (stateId, state) in configuration.States)
        {
            if (configuration.TerminalStates.Contains(stateId))
            {
                continue;
            }

            if (IsExceptionState(state))
            {
                continue;
            }

            if (!referencedStates.Contains(stateId))
            {
                continue;
            }

            if (!outgoingByState.TryGetValue(stateId, out var count) || count == 0)
            {
                return Issues.DeadEndState(stateId);
            }
        }

        var graph = BuildGraph(configuration);
        foreach (var transition in configuration.Transitions)
        {
            var skipped = FindSkippedIntermediateState(graph, transition.From, transition.To);
            if (skipped is not null)
            {
                return Issues.StateSkipping($"machine.transitions.{transition.From}->{transition.To}", skipped);
            }
        }

        if (ContainsCycle(graph))
        {
            return Issues.CircularTransition();
        }

        return null;
    }

    private static bool IsExceptionState(StateDefinition state) =>
        string.Equals(state.Type, "exception", StringComparison.OrdinalIgnoreCase);

    private static HashSet<string> ReachableFromInitial(StateMachineConfiguration configuration)
    {
        var graph = BuildGraph(configuration);
        var visited = new HashSet<string>(StringComparer.Ordinal) { configuration.InitialState };
        var queue = new Queue<string>();
        queue.Enqueue(configuration.InitialState);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!graph.TryGetValue(current, out var nextStates))
            {
                continue;
            }

            foreach (var next in nextStates)
            {
                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited;
    }

    private static Dictionary<string, HashSet<string>> BuildGraph(StateMachineConfiguration configuration)
    {
        var graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var transition in configuration.Transitions)
        {
            if (!graph.TryGetValue(transition.From, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                graph[transition.From] = set;
            }

            set.Add(transition.To);
        }

        return graph;
    }

    private static string? FindSkippedIntermediateState(Dictionary<string, HashSet<string>> graph, string from, string to)
    {
        if (!graph.TryGetValue(from, out var firstHop))
        {
            return null;
        }

        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal) { from };
        var parent = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var next in firstHop)
        {
            if (string.Equals(next, to, StringComparison.Ordinal))
            {
                continue;
            }

            if (visited.Add(next))
            {
                parent[next] = from;
                queue.Enqueue(next);
            }
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();

            if (!graph.TryGetValue(node, out var nextStates))
            {
                continue;
            }

            foreach (var next in nextStates)
            {
                if (visited.Add(next))
                {
                    parent[next] = node;
                    if (string.Equals(next, to, StringComparison.Ordinal))
                    {
                        return ResolveFirstIntermediate(from, to, parent);
                    }

                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }

    private static string ResolveFirstIntermediate(string from, string to, Dictionary<string, string> parent)
    {
        var current = to;
        var path = new List<string> { to };
        while (parent.TryGetValue(current, out var p) && !string.Equals(p, from, StringComparison.Ordinal))
        {
            path.Add(p);
            current = p;
        }

        if (!parent.TryGetValue(current, out var directParent) || !string.Equals(directParent, from, StringComparison.Ordinal))
        {
            return to;
        }

        path.Add(directParent);
        path.Reverse();

        return path.Count >= 2 ? path[1] : to;
    }

    private static bool ContainsCycle(Dictionary<string, HashSet<string>> graph)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in graph.Keys)
        {
            if (Visit(node))
            {
                return true;
            }
        }

        return false;

        bool Visit(string node)
        {
            if (stack.Contains(node))
            {
                return true;
            }

            if (!visited.Add(node))
            {
                return false;
            }

            stack.Add(node);
            if (graph.TryGetValue(node, out var nextStates))
            {
                foreach (var next in nextStates)
                {
                    if (string.Equals(next, node, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (Visit(next))
                    {
                        return true;
                    }
                }
            }

            stack.Remove(node);
            return false;
        }
    }
}

internal static class GuardInvariantValidation
{
    private static readonly FrozenSet<string> AllowedOperators = new[] { "==", "!=" }.ToFrozenSet(StringComparer.Ordinal);
    private static readonly FrozenSet<string> AllowedInvariantSeverities = new[] { "blocking", "warning" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal static ValidationIssue? TryValidate(StateMachineConfiguration configuration, IReadOnlySet<string> knownContextProperties)
    {
        foreach (var (guardId, guard) in configuration.Guards)
        {
            var location = $"machine.guards.{guardId}";

            if (string.IsNullOrWhiteSpace(guard.When))
            {
                return Issues.InvalidGuardExpression(location, "Guard is missing 'when'", "Add 'when' expression");
            }

            var parsed = TryParseContextComparison(guard.When);
            if (parsed is null)
            {
                return Issues.InvalidGuardExpression(location, "Invalid guard expression", "Use 'context.<property> == <literal>'");
            }

            var (property, op) = parsed.Value;
            if (!AllowedOperators.Contains(op))
            {
                return Issues.InvalidGuardExpression(location, "Unsupported guard operator", "Use '==' or '!='");
            }

            if (!knownContextProperties.Contains(property))
            {
                return Issues.UnknownContextProperty(location, property);
            }
        }

        foreach (var (invariantId, invariant) in configuration.Invariants)
        {
            var location = $"machine.invariants.{invariantId}";

            if (string.IsNullOrWhiteSpace(invariant.Severity))
            {
                return Issues.InvalidInvariant(location, "Invariant is missing 'severity'", "Use 'blocking' or 'warning'");
            }

            if (!AllowedInvariantSeverities.Contains(invariant.Severity))
            {
                return Issues.InvalidInvariant(location, "Invariant severity is invalid", "Use 'blocking' or 'warning'");
            }
        }

        return null;
    }

    private static (string Property, string Operator)? TryParseContextComparison(string expression)
    {
        var normalized = expression.Trim();
        if (!normalized.StartsWith("context.", StringComparison.Ordinal))
        {
            return null;
        }

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return null;
        }

        var left = parts[0];
        var op = parts[1];
        var property = left["context.".Length..];

        return (property, op);
    }
}

internal static class SafetyValidation
{
    internal static ValidationIssue? TryValidate(StateMachineConfiguration configuration, StateMachineYamlValidationBaseline? baseline)
    {
        if (!InitialCanReachAnyTerminal(configuration))
        {
            return Issues.NoPathToTerminal();
        }

        if (!HasBlockingInvariant(configuration))
        {
            return Issues.NoBlockingInvariants();
        }

        if (baseline is not null)
        {
            var baselineParse = DslParser.TryParse(baseline.YamlText);
            if (baselineParse.Error is not null || baselineParse.Root is null)
            {
                return null;
            }

            var baselineBuild = StructuralValidation.TryBuildConfiguration(baselineParse.Root);
            if (baselineBuild.Error is not null || baselineBuild.Configuration is null)
            {
                return null;
            }

            var baselineConfig = baselineBuild.Configuration;
            if (string.Equals(configuration.Meta.Version, baselineConfig.Meta.Version, StringComparison.Ordinal) &&
                BehaviorChanged(configuration, baselineConfig))
            {
                return Issues.VersionNotUpdated();
            }
        }

        return null;
    }

    private static bool HasBlockingInvariant(StateMachineConfiguration configuration) =>
        configuration.Invariants.Values.Any(i => string.Equals(i.Severity, "blocking", StringComparison.OrdinalIgnoreCase));

    private static bool InitialCanReachAnyTerminal(StateMachineConfiguration configuration)
    {
        if (configuration.TerminalStates.Count == 0)
        {
            return false;
        }

        var graph = configuration.Transitions
            .GroupBy(t => t.From, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(t => t.To).ToArray(), StringComparer.Ordinal);

        var visited = new HashSet<string>(StringComparer.Ordinal) { configuration.InitialState };
        var queue = new Queue<string>();
        queue.Enqueue(configuration.InitialState);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (configuration.TerminalStates.Contains(current))
            {
                return true;
            }

            if (!graph.TryGetValue(current, out var next))
            {
                continue;
            }

            foreach (var n in next)
            {
                if (visited.Add(n))
                {
                    queue.Enqueue(n);
                }
            }
        }

        return false;
    }

    private static bool BehaviorChanged(StateMachineConfiguration current, StateMachineConfiguration baseline)
    {
        if (!string.Equals(current.InitialState, baseline.InitialState, StringComparison.Ordinal))
        {
            return true;
        }

        if (!current.TerminalStates.SetEquals(baseline.TerminalStates))
        {
            return true;
        }

        var currentTransitions = CanonicalTransitions(current.Transitions);
        var baselineTransitions = CanonicalTransitions(baseline.Transitions);
        return !string.Equals(currentTransitions, baselineTransitions, StringComparison.Ordinal);
    }

    private static string CanonicalTransitions(IReadOnlyList<TransitionDefinition> transitions)
    {
        return string.Join("|", transitions
            .Select(t => new
            {
                t.From,
                t.To,
                t.On,
                Guards = string.Join(",", t.Guards.OrderBy(x => x, StringComparer.Ordinal)),
                Emit = string.Join(",", t.Emit.OrderBy(x => x, StringComparer.Ordinal))
            })
            .OrderBy(x => x.From, StringComparer.Ordinal)
            .ThenBy(x => x.To, StringComparer.Ordinal)
            .ThenBy(x => x.On, StringComparer.Ordinal)
            .Select(x => $"{x.From}->{x.To}@{x.On}[{x.Guards}][{x.Emit}]"));
    }
}

internal static class WarningValidation
{
    internal static IReadOnlyList<ValidationIssue> GetWarnings(StateMachineConfiguration configuration)
    {
        var warnings = new List<ValidationIssue>();

        var usedStates = new HashSet<string>(StringComparer.Ordinal)
        {
            configuration.InitialState
        };

        foreach (var transition in configuration.Transitions)
        {
            usedStates.Add(transition.From);
            usedStates.Add(transition.To);
        }

        foreach (var stateId in configuration.States.Keys)
        {
            if (!usedStates.Contains(stateId))
            {
                warnings.Add(Issues.UnusedStateWarning(stateId));
            }
        }

        var usedGuards = new HashSet<string>(StringComparer.Ordinal);
        foreach (var transition in configuration.Transitions)
        {
            foreach (var guard in transition.Guards)
            {
                usedGuards.Add(guard);
            }
        }

        foreach (var guardId in configuration.Guards.Keys)
        {
            if (!usedGuards.Contains(guardId))
            {
                warnings.Add(Issues.UnusedGuardWarning(guardId));
            }
        }

        var usedEvents = new HashSet<string>(StringComparer.Ordinal);
        foreach (var transition in configuration.Transitions)
        {
            foreach (var emitted in transition.Emit)
            {
                usedEvents.Add(emitted);
            }
        }

        foreach (var eventId in configuration.Events.Keys)
        {
            if (!usedEvents.Contains(eventId))
            {
                warnings.Add(Issues.UnusedEventWarning(eventId));
            }
        }

        return warnings;
    }
}
