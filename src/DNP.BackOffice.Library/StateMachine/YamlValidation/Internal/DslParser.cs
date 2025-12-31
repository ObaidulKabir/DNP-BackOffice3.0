namespace DNP.BackOffice.Library.StateMachine.YamlValidation;

internal static class DslParser
{
    internal sealed record ParseOutcome(DslMapping? Root, ValidationIssue? Error);

    internal static ParseOutcome TryParse(string yamlText)
    {
        if (string.IsNullOrWhiteSpace(yamlText))
        {
            return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
        }

        var root = new DslMapping(new Dictionary<string, DslNode>(StringComparer.Ordinal));
        var stack = new List<Frame> { new(0, root) };

        var lines = yamlText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var trimmedStart = rawLine.TrimStart(' ');
            if (trimmedStart.StartsWith('#'))
            {
                continue;
            }

            if (rawLine.Contains('\t'))
            {
                return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
            }

            var indentSpaces = rawLine.Length - trimmedStart.Length;
            if (indentSpaces % 2 != 0)
            {
                return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
            }

            var level = indentSpaces / 2;

            FinalizePendingKeysAtOrAboveLevel(stack, level);

            while (stack.Count - 1 > level)
            {
                FinalizePendingKey(stack[^1]);
                stack.RemoveAt(stack.Count - 1);
            }

            if (stack.Count - 1 < level)
            {
                if (stack.Count - 1 != level - 1)
                {
                    return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
                }

                var parent = stack[^1];
                if (parent.Node is DslMapping mappingParent && parent.PendingKey is not null)
                {
                    DslNode child = trimmedStart.StartsWith("- ", StringComparison.Ordinal)
                        ? new DslSequence(new List<DslNode>())
                        : new DslMapping(new Dictionary<string, DslNode>(StringComparer.Ordinal));

                    mappingParent.Children[parent.PendingKey] = child;
                    parent.PendingKey = null;
                    stack.Add(new Frame(level, child));
                }
                else
                {
                    return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
                }
            }

            var frame = stack[^1];
            var content = trimmedStart.TrimEnd();

            if (content.StartsWith("-", StringComparison.Ordinal))
            {
                if (frame.Node is not DslSequence seq)
                {
                    return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
                }

                var itemText = content[1..].TrimStart(' ');
                if (string.IsNullOrWhiteSpace(itemText))
                {
                    return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
                }

                var firstSpace = itemText.IndexOf(' ');
                if (firstSpace < 0)
                {
                    seq.Items.Add(new DslScalar(itemText));
                    continue;
                }

                var key = itemText[..firstSpace];
                var value = itemText[(firstSpace + 1)..].Trim();
                var itemMapping = new DslMapping(new Dictionary<string, DslNode>(StringComparer.Ordinal)
                {
                    [key] = new DslScalar(value)
                });
                seq.Items.Add(itemMapping);

                stack.Add(new Frame(level + 1, itemMapping));
                continue;
            }

            if (frame.Node is not DslMapping currentMapping)
            {
                return new ParseOutcome(null, Issues.SyntaxInvalid("YAML", "YAML"));
            }

            var parts = content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var mappingKey = parts[0];
            if (currentMapping.Children.ContainsKey(mappingKey))
            {
                return new ParseOutcome(null, Issues.DuplicateKey("YAML", mappingKey));
            }

            if (parts.Length == 1)
            {
                currentMapping.Children[mappingKey] = DslPlaceholder.Instance;
                frame.PendingKey = mappingKey;
                continue;
            }

            currentMapping.Children[mappingKey] = new DslScalar(parts[1].Trim());
        }

        FinalizePendingKeysAtOrAboveLevel(stack, 0);
        FinalizeAllPlaceholders(root);
        return new ParseOutcome(root, null);
    }

    private static void FinalizeAllPlaceholders(DslNode node)
    {
        if (node is DslMapping map)
        {
            foreach (var (key, value) in map.Children.ToArray())
            {
                if (ReferenceEquals(value, DslPlaceholder.Instance))
                {
                    map.Children[key] = new DslMapping(new Dictionary<string, DslNode>(StringComparer.Ordinal));
                    continue;
                }

                FinalizeAllPlaceholders(value);
            }
        }

        if (node is DslSequence seq)
        {
            foreach (var item in seq.Items)
            {
                FinalizeAllPlaceholders(item);
            }
        }
    }

    private static void FinalizePendingKeysAtOrAboveLevel(List<Frame> stack, int level)
    {
        for (var i = stack.Count - 1; i >= 0; i--)
        {
            if (stack[i].Level < level)
            {
                break;
            }

            FinalizePendingKey(stack[i]);
        }
    }

    private static void FinalizePendingKey(Frame frame)
    {
        if (frame.Node is not DslMapping map)
        {
            frame.PendingKey = null;
            return;
        }

        if (frame.PendingKey is null)
        {
            return;
        }

        if (map.Children.TryGetValue(frame.PendingKey, out var value) && ReferenceEquals(value, DslPlaceholder.Instance))
        {
            map.Children[frame.PendingKey] = new DslMapping(new Dictionary<string, DslNode>(StringComparer.Ordinal));
        }

        frame.PendingKey = null;
    }

    private sealed class Frame(int level, DslNode node)
    {
        public int Level { get; } = level;
        public DslNode Node { get; } = node;
        public string? PendingKey { get; set; }
    }
}
