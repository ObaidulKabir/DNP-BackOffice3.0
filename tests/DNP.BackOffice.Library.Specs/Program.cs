using DNP.BackOffice.Library.StateMachine.YamlValidation;

namespace DNP.BackOffice.Library.Specs;

internal static class Program
{
    private static int Main()
    {
        var validator = new StateMachineYamlValidator();

        var tests = new List<(string Name, Action Run)>
        {
            ("TC-A-001 Invalid YAML indentation => SMV-001", () =>
            {
                var yaml = "machine\n   meta\n    id x\n";
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-001");
            }),
            ("TC-A-002 Duplicate keys => SMV-002", () =>
            {
                var yaml = string.Join("\n", new[]
                {
                    "machine",
                    "  meta",
                    "    id x",
                    "    id y",
                    "    version 1.0.0",
                    "    domain D",
                    "    description desc",
                    "  states",
                    "    A",
                    "      type normal",
                    "    B",
                    "      type terminal",
                    "  initial A",
                    "  transitions",
                    "    - from A",
                    "      to B",
                    "      on Go",
                    "  terminal",
                    "    - B",
                    "  invariants",
                    "    I1",
                    "      when context.orderCancelled != true",
                    "      severity blocking",
                    "      message msg",
                    "  events",
                    "    E1",
                    "      payload [ orderId ]"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-002");
            }),
            ("TC-B-001 Missing machine => SMV-101", () =>
            {
                var yaml = string.Join("\n", new[]
                {
                    "meta",
                    "  id x"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-101");
            }),
            ("TC-B-002 Missing states => SMV-101", () =>
            {
                var yaml = string.Join("\n", new[]
                {
                    "machine",
                    "  meta",
                    "    id x",
                    "    version 1.0.0",
                    "    domain D",
                    "    description desc",
                    "  initial A",
                    "  transitions",
                    "    - from A",
                    "      to B",
                    "      on Go"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-101");
            }),
            ("TC-B-003 Transitions not list => SMV-102", () =>
            {
                var yaml = string.Join("\n", new[]
                {
                    "machine",
                    "  meta",
                    "    id x",
                    "    version 1.0.0",
                    "    domain D",
                    "    description desc",
                    "  states",
                    "    A",
                    "      type normal",
                    "    B",
                    "      type terminal",
                    "  initial A",
                    "  transitions",
                    "    from A"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-102");
            }),
            ("TC-B-004 Missing version => SMV-103", () =>
            {
                var yaml = string.Join("\n", new[]
                {
                    "machine",
                    "  meta",
                    "    id x",
                    "    domain D",
                    "    description desc",
                    "  states",
                    "    A",
                    "      type normal",
                    "    B",
                    "      type terminal",
                    "  initial A",
                    "  transitions",
                    "    - from A",
                    "      to B",
                    "      on Go"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-103");
            }),
            ("TC-C-001 Transition undefined from => SMV-201", () =>
            {
                var yaml = ValidBaseYamlWithTransitions(
                    "    - from X\n      to B\n      on Go",
                    additionalStates: "");

                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-201");
            }),
            ("TC-C-002 Transition undefined to => SMV-201", () =>
            {
                var yaml = ValidBaseYamlWithTransitions(
                    "    - from A\n      to X\n      on Go",
                    additionalStates: "");

                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-201");
            }),
            ("TC-C-003 Transition undefined guard => SMV-202", () =>
            {
                var yaml = ValidBaseYamlWithTransitions(
                    "    - from A\n      to B\n      on Go\n      guards [ G_UNKNOWN ]");

                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-202");
            }),
            ("TC-C-004 Transition emits undefined event => SMV-203", () =>
            {
                var yaml = ValidBaseYamlWithTransitions(
                    "    - from A\n      to B\n      on Go\n      emit [ E_UNKNOWN ]");

                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-203");
            }),
            ("TC-D-001 Initial not defined => SMV-301", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["initial"] = "  initial X"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-301");
            }),
            ("TC-D-002 Initial is terminal => SMV-301", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["initial"] = "  initial B"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-301");
            }),
            ("TC-D-003 No terminal state => SMV-502", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["terminal"] = ""
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-502");
            }),
            ("TC-D-004 Terminal has outgoing transition => SMV-501", () =>
            {
                var yaml = ValidBaseYamlWithTransitions(
                    "    - from B\n      to A\n      on Back",
                    additionalTransitions: "\n    - from A\n      to B\n      on Go");
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-501");
            }),
            ("TC-E-001 Unreachable state => SMV-302", () =>
            {
                var yaml = ValidBaseYaml(additionalStates: "\n    C\n      type normal", overrides: new()
                {
                    ["transitions"] = string.Join("\n", new[]
                    {
                        "  transitions",
                        "    - from A",
                        "      to B",
                        "      on Go",
                        "    - from C",
                        "      to B",
                        "      on GoFromC"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-302");
            }),
            ("TC-E-002 Dead-end non-terminal state => SMV-303", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["states"] = string.Join("\n", new[]
                    {
                        "  states",
                        "    A",
                        "      type normal",
                        "    B",
                        "      type normal",
                        "    C",
                        "      type terminal"
                    }),
                    ["initial"] = "  initial A",
                    ["terminal"] = "  terminal\n    - C",
                    ["transitions"] = string.Join("\n", new[]
                    {
                        "  transitions",
                        "    - from A",
                        "      to B",
                        "      on ToB",
                        "    - from A",
                        "      to C",
                        "      on ToC"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-303");
            }),
            ("TC-E-003 Implicit state skipping => SMV-304", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["states"] = string.Join("\n", new[]
                    {
                        "  states",
                        "    A",
                        "      type normal",
                        "    Finishing",
                        "      type normal",
                        "    B",
                        "      type terminal"
                    }),
                    ["initial"] = "  initial A",
                    ["terminal"] = "  terminal\n    - B",
                    ["transitions"] = string.Join("\n", new[]
                    {
                        "  transitions",
                        "    - from A",
                        "      to Finishing",
                        "      on Step1",
                        "    - from Finishing",
                        "      to B",
                        "      on Step2",
                        "    - from A",
                        "      to B",
                        "      on Skip"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-304");
            }),
            ("TC-E-004 Circular transition => SMV-305", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["states"] = string.Join("\n", new[]
                    {
                        "  states",
                        "    A",
                        "      type normal",
                        "    B",
                        "      type normal",
                        "    C",
                        "      type terminal"
                    }),
                    ["initial"] = "  initial A",
                    ["terminal"] = "  terminal\n    - C",
                    ["transitions"] = string.Join("\n", new[]
                    {
                        "  transitions",
                        "    - from A",
                        "      to B",
                        "      on Go",
                        "    - from B",
                        "      to A",
                        "      on Back",
                        "    - from B",
                        "      to C",
                        "      on Finish"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-305");
            }),
            ("TC-F-001 Unsupported guard operation => SMV-401", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["guards"] = string.Join("\n", new[]
                    {
                        "  guards",
                        "    G1",
                        "      when isPaid()",
                        "      message m"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-401");
            }),
            ("TC-F-002 Guard unknown context property => SMV-402", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["guards"] = string.Join("\n", new[]
                    {
                        "  guards",
                        "    G1",
                        "      when context.paymentDone == true",
                        "      message m"
                    }),
                    ["transitions"] = "  transitions\n    - from A\n      to B\n      on Go\n      guards [ G1 ]"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-402");
            }),
            ("TC-F-003 Guard missing when => SMV-401", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["guards"] = string.Join("\n", new[]
                    {
                        "  guards",
                        "    G1",
                        "      message m"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-401");
            }),
            ("TC-G-001 Invariant missing severity => SMV-403", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["invariants"] = string.Join("\n", new[]
                    {
                        "  invariants",
                        "    I1",
                        "      when context.orderCancelled != true",
                        "      message msg"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-403");
            }),
            ("TC-G-002 Invariant invalid severity => SMV-403", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["invariants"] = string.Join("\n", new[]
                    {
                        "  invariants",
                        "    I1",
                        "      when context.orderCancelled != true",
                        "      severity block",
                        "      message msg"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-403");
            }),
            ("TC-G-003 No blocking invariant => SMV-602", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["invariants"] = string.Join("\n", new[]
                    {
                        "  invariants",
                        "    I1",
                        "      when context.orderCancelled != true",
                        "      severity warning",
                        "      message msg"
                    })
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-602");
            }),
            ("TC-H-001 No path to terminal => SMV-601", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["transitions"] = "  transitions\n    - from A\n      to A\n      on Loop"
                });
                var result = validator.Validate(yaml);
                AssertInvalidWithError(result, "SMV-601");
            }),
            ("TC-H-002 Behavior changed without version increment => SMV-603", () =>
            {
                var baseline = ValidBaseYaml();
                var current = ValidBaseYaml(overrides: new()
                {
                    ["transitions"] = "  transitions\n    - from A\n      to B\n      on Go2"
                });
                var result = validator.Validate(current, new StateMachineYamlValidationBaseline(baseline));
                AssertInvalidWithError(result, "SMV-603");
            }),
            ("TC-I-001 Unused state => warning", () =>
            {
                var yaml = ValidBaseYaml(additionalStates: "\n    Unused\n      type normal");
                var result = validator.Validate(yaml);
                AssertValidWithWarning(result);
            }),
            ("TC-I-002 Unused guard => warning", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["guards"] = string.Join("\n", new[]
                    {
                        "  guards",
                        "    G_UNUSED",
                        "      when context.paymentStatus == 'PAID'",
                        "      message m"
                    })
                });
                var result = validator.Validate(yaml);
                AssertValidWithWarning(result);
            }),
            ("TC-I-003 Unused event => warning", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["events"] = string.Join("\n", new[]
                    {
                        "  events",
                        "    E1",
                        "      payload [ orderId ]",
                        "    E_UNUSED",
                        "      payload [ orderId ]"
                    })
                });
                var result = validator.Validate(yaml);
                AssertValidWithWarning(result);
            }),
            ("TC-J-001 Fully valid configuration => success", () =>
            {
                var result = validator.Validate(ValidBaseYaml());
                AssertValid(result);
            }),
            ("TC-J-002 Valid exception routing => success", () =>
            {
                var yaml = ValidBaseYaml(overrides: new()
                {
                    ["states"] = string.Join("\n", new[]
                    {
                        "  states",
                        "    A",
                        "      type normal",
                        "    B",
                        "      type terminal",
                        "    EX",
                        "      type exception"
                    }),
                    ["transitions"] = string.Join("\n", new[]
                    {
                        "  transitions",
                        "    - from A",
                        "      to B",
                        "      on Go",
                        "    - from A",
                        "      to EX",
                        "      on Fail"
                    })
                });
                var result = validator.Validate(yaml);
                AssertValid(result);
            })
        };

        var failures = 0;
        foreach (var (name, run) in tests)
        {
            try
            {
                run();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL {name}");
                Console.WriteLine(ex.Message);
            }
        }

        Console.WriteLine($"Total: {tests.Count}, Failed: {failures}");
        return failures == 0 ? 0 : 1;
    }

    private static void AssertInvalidWithError(ValidationResult result, string expectedErrorCode)
    {
        if (result.IsValid)
        {
            throw new InvalidOperationException($"Expected invalid, got valid.");
        }

        var errors = result.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();

        if (errors.Count != 1)
        {
            var codes = string.Join(", ", errors.Select(e => e.ErrorCode));
            throw new InvalidOperationException($"Expected exactly 1 error (fail-fast). Got {errors.Count}: {codes}.");
        }

        var firstError = errors[0];
        if (firstError is null)
        {
            throw new InvalidOperationException("Expected at least one error.");
        }

        if (!string.Equals(firstError.ErrorCode, expectedErrorCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected error {expectedErrorCode}, got {firstError.ErrorCode}.");
        }

        AssertHasRequiredFields(firstError);
    }

    private static void AssertValid(ValidationResult result)
    {
        if (!result.IsValid)
        {
            var errors = string.Join(", ", result.Issues.Where(i => i.Severity == ValidationSeverity.Error).Select(i => i.ErrorCode));
            throw new InvalidOperationException($"Expected valid, got errors: {errors}.");
        }
    }

    private static void AssertValidWithWarning(ValidationResult result)
    {
        AssertValid(result);

        var warnings = result.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
        if (warnings.Count == 0)
        {
            throw new InvalidOperationException("Expected at least one warning.");
        }

        foreach (var warning in warnings)
        {
            AssertHasRequiredFields(warning);
        }
    }

    private static void AssertHasRequiredFields(ValidationIssue issue)
    {
        if (string.IsNullOrWhiteSpace(issue.ErrorCode))
        {
            throw new InvalidOperationException("Expected non-empty ErrorCode.");
        }

        if (string.IsNullOrWhiteSpace(issue.Section))
        {
            throw new InvalidOperationException($"Expected non-empty Section for {issue.ErrorCode}.");
        }

        if (string.IsNullOrWhiteSpace(issue.Location))
        {
            throw new InvalidOperationException($"Expected non-empty Location for {issue.ErrorCode}.");
        }

        if (string.IsNullOrWhiteSpace(issue.Message))
        {
            throw new InvalidOperationException($"Expected non-empty Message for {issue.ErrorCode}.");
        }

        if (string.IsNullOrWhiteSpace(issue.Suggestion))
        {
            throw new InvalidOperationException($"Expected non-empty Suggestion for {issue.ErrorCode}.");
        }
    }

    private static string ValidBaseYaml(
        string additionalStates = "",
        Dictionary<string, string>? overrides = null)
    {
        overrides ??= new Dictionary<string, string>();

        var meta = overrides.TryGetValue("meta", out var metaOverride)
            ? metaOverride
            : string.Join("\n", new[]
            {
                "  meta",
                "    id m1",
                "    version 1.0.0",
                "    domain D",
                "    description desc"
            });

        var states = overrides.TryGetValue("states", out var statesOverride)
            ? statesOverride
            : string.Join("\n", new[]
            {
                "  states",
                "    A",
                "      type normal",
                "    B",
                "      type terminal" + additionalStates
            });

        var initial = overrides.TryGetValue("initial", out var initialOverride)
            ? initialOverride
            : "  initial A";

        var terminal = overrides.TryGetValue("terminal", out var terminalOverride)
            ? terminalOverride
            : "  terminal\n    - B";

        var transitions = overrides.TryGetValue("transitions", out var transitionsOverride)
            ? transitionsOverride
            : string.Join("\n", new[]
            {
                "  transitions",
                "    - from A",
                "      to B",
                "      on Go"
            });

        var guards = overrides.TryGetValue("guards", out var guardsOverride)
            ? guardsOverride
            : string.Empty;

        var invariants = overrides.TryGetValue("invariants", out var invariantsOverride)
            ? invariantsOverride
            : string.Join("\n", new[]
            {
                "  invariants",
                "    I1",
                "      when context.orderCancelled != true",
                "      severity blocking",
                "      message msg"
            });

        var events = overrides.TryGetValue("events", out var eventsOverride)
            ? eventsOverride
            : string.Join("\n", new[]
            {
                "  events",
                "    E1",
                "      payload [ orderId ]"
            });

        var sections = new List<string> { "machine", meta, states, initial, terminal, transitions };

        if (!string.IsNullOrWhiteSpace(guards))
        {
            sections.Add(guards);
        }

        if (!string.IsNullOrWhiteSpace(invariants))
        {
            sections.Add(invariants);
        }

        if (!string.IsNullOrWhiteSpace(events))
        {
            sections.Add(events);
        }

        return string.Join("\n", sections.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static string ValidBaseYamlWithTransitions(
        string transitionsListItem,
        string additionalStates = "",
        string additionalTransitions = "")
    {
        var transitions = "  transitions\n" + transitionsListItem + additionalTransitions;

        return ValidBaseYaml(additionalStates: additionalStates, overrides: new Dictionary<string, string>
        {
            ["transitions"] = transitions
        });
    }
}
