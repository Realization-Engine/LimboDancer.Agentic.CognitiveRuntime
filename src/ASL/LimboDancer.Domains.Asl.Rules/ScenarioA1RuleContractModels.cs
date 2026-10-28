namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The type of a contract's fact (the Rules-first plan, section 5): what a page shows to enter it, and what JSON value it takes.</summary>
public enum ContractFactType
{
    /// <summary>A yes or no: a JSON true or false.</summary>
    Flag,

    /// <summary>A number.</summary>
    Number,

    /// <summary>Free text: a definition id, a terrain word.</summary>
    Text,

    /// <summary>One of the fact's choices.</summary>
    Choice,

    /// <summary>A Location, as the caller writes it ("bd04:G5:0"); picked on the map.</summary>
    Hex,

    /// <summary>A unit's id; picked on the map.</summary>
    Unit,
}

/// <summary>Who may know a fact under the rules (the plan's section 5): a viewer who may not receives it as unknown.</summary>
public enum ContractDisclosure
{
    /// <summary>Every viewer.</summary>
    All,

    /// <summary>The side that owns the unit the fact concerns, and the adjudicator.</summary>
    Owner,

    /// <summary>The adjudicator alone, who sees the whole state.</summary>
    Adjudicator,
}

/// <summary>A rule a contract decides, with the physical page of the PDF it is printed on.</summary>
public sealed record ContractRule(string Rule, int Page);

/// <summary>
/// A fact of a contract: its name in the facts JSON, its type, whether it may be unknown (only where a side may lack it under the rules, never in
/// place of missing code), who may know it, and, for an owner's fact, the Unit fact whose owner that is.
/// </summary>
public sealed record ContractFact(string Name, ContractFactType Type, bool MayBeUnknown, ContractDisclosure Disclosure, string Description)
{
    /// <summary>The values a Choice fact takes.</summary>
    public IReadOnlyList<string>? Choices
    {
        get; init;
    }

    /// <summary>For an Owner fact, the name of the Unit fact it concerns; the viewer must be that unit's side.</summary>
    public string? Concerns
    {
        get; init;
    }
}

/// <summary>An event a command built on the contract would write: its type and what it holds.</summary>
public sealed record ContractEvent(string Type, string Content);

/// <summary>Where the contract applies: the phase and the step of the ASOP (Appendix 2).</summary>
public sealed record ContractPlace(string Phase, string AsopStep);

/// <summary>A reason of a verdict, with the rule and page it rests on.</summary>
public sealed record ContractReason(string Text, string Rule, int Page);

/// <summary>A roll the verdict requires; nothing is rolled. The target is the highest Final DR that succeeds, when the contract knows it.</summary>
public sealed record ContractRoll(string Kind, int? Target, string Text);

/// <summary>A contract's answer: an outcome from its list, the reasons, and the rolls it requires.</summary>
public sealed record ContractVerdict(string Outcome, IReadOnlyList<ContractReason> Reasons, IReadOnlyList<ContractRoll> Rolls)
{
    /// <summary>The outcome every contract has: a fact it needs is missing or unknown to the viewer, or a fact is not of its type.</summary>
    public const string NotEvaluated = "not-evaluated";
}

/// <summary>
/// A wiring contract (the Rules-first plan, section 5): what a calculator decides, the facts it takes, the verdicts it returns, the events a command
/// built on it would write, and where it applies. It refers to nothing outside Rules. <see cref="ScenarioA1RuleContracts.Evaluate"/> drives it from
/// JSON, so a page or a test can ask it without code of its own.
/// </summary>
public sealed record RuleContract(string Id, string Title, IReadOnlyList<ContractRule> Rules, IReadOnlyList<ContractFact> Facts,
    IReadOnlyList<string> Outcomes, IReadOnlyList<ContractEvent> Events, ContractPlace Place)
{
    /// <summary>The calculator behind the contract, as a function of the checked facts.</summary>
    internal Func<ContractFacts, ContractVerdict> Decide
    {
        get; init;
    } = _ => new ContractVerdict(ContractVerdict.NotEvaluated, [], []);
}

/// <summary>The facts of one evaluation, checked against the contract: each read by name, null when unknown.</summary>
public sealed class ContractFacts
{
    private readonly IReadOnlyDictionary<string, object?> values;

    internal ContractFacts(IReadOnlyDictionary<string, object?> values)
    {
        this.values = values;
    }

    public bool? Flag(string name) => values.TryGetValue(name, out var value) ? value as bool? : null;

    public decimal? Number(string name) => values.TryGetValue(name, out var value) ? value as decimal? : null;

    public string? Text(string name) => values.TryGetValue(name, out var value) ? value as string : null;
}
