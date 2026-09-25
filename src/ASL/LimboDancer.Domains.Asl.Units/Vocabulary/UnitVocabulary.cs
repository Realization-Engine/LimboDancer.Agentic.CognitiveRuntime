using System.Security.Cryptography;
using System.Text;

namespace LimboDancer.Domains.Asl.Units.Vocabulary;

public sealed record UnitVocabularyResult(UnitVocabulary? Vocabulary, IReadOnlyList<UnitDiagnostic> Diagnostics);

/// <summary>
/// A set of vocabulary packs combined into one taxonomy (Unit Display Design, section 3.1; ASL-UNIT-073). Kinds form a
/// tree under the core <c>unit</c> kind; a kind accepts the faces, attributes, and traits of every kind above it.
/// Packs may extend the kinds of packs they name in <c>extends</c> and never change another pack's declarations.
/// </summary>
public sealed class UnitVocabulary
{
    private static readonly KindDefinition Root = new(VocabularyNames.RootKind, "unit", null, ["front"], [], [], null,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["default"] = "{side} {kind} {identity}" },
        new Dictionary<string, string>(StringComparer.Ordinal) { ["default"] = "{kind}" }, null);

    private readonly Dictionary<string, KindDefinition> kinds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AttributeDefinition> attributes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TraitDefinition> traits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StateDefinition> states = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SideDefinition> sides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FaceDefinition> faces = new(StringComparer.Ordinal);
    private readonly Dictionary<string, KindInfo> info = new(StringComparer.Ordinal);
    private readonly List<string> stateOrder = [];

    private UnitVocabulary(IReadOnlyList<VocabularyPack> packs)
    {
        Packs = packs;
        Identity = string.Join('+', packs.Select(pack => pack.Identity));
        var hashes = string.Join('\n', packs.Select(pack => $"{pack.Identity} {pack.Hash}"));
        Hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(hashes)));
    }

    public IReadOnlyList<VocabularyPack> Packs
    {
        get;
    }

    /// <summary>The packs' identities, such as <c>asl@1.0.0</c>, joined with <c>+</c>.</summary>
    public string Identity
    {
        get;
    }

    /// <summary>A hash over every pack's identity and content hash.</summary>
    public string Hash
    {
        get;
    }

    public IEnumerable<KindDefinition> Kinds => kinds.Values.OrderBy(kind => kind.Name, StringComparer.Ordinal);

    public IEnumerable<SideDefinition> Sides => sides.Values.OrderBy(side => side.Name, StringComparer.Ordinal);

    public IEnumerable<StateDefinition> States => stateOrder.Select(name => states[name]);

    public IEnumerable<TraitDefinition> Traits => traits.Values;

    /// <summary>The built-in vocabulary: the <c>asl</c> pack alone.</summary>
    public static UnitVocabulary Asl() =>
        Create([VocabularyPackReader.Asl()]).Vocabulary ?? throw new InvalidDataException("The asl vocabulary pack does not combine.");

    public static UnitVocabularyResult Create(IReadOnlyList<VocabularyPack> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);
        var diagnostics = new List<UnitDiagnostic>();
        var vocabulary = new UnitVocabulary(packs);
        vocabulary.kinds[Root.Name] = Root;
        var owner = new Dictionary<string, VocabularyPack>(StringComparer.Ordinal);
        var byName = new Dictionary<string, VocabularyPack>(StringComparer.Ordinal);
        foreach (var pack in packs)
        {
            if (!byName.TryAdd(pack.Pack, pack))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-004", $"The pack '{pack.Pack}' is loaded twice.", pack.Identity));
            }
        }

        foreach (var pack in packs)
        {
            foreach (var reference in pack.Extends)
            {
                var at = reference.IndexOf('@', StringComparison.Ordinal);
                var name = at < 0 ? reference : reference[..at];
                if (!byName.TryGetValue(name, out var extended) || (at >= 0 && !Compatible(extended.Version, reference[(at + 1)..])))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-008", $"The pack extends '{reference}', which is not loaded.", pack.Identity));
                }
            }

            foreach (var side in pack.Sides)
            {
                if (vocabulary.sides.TryGetValue(side.Name, out var existing) && existing.Label != side.Label)
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-007", $"The side '{side.Name}' is already declared with another label.", pack.Identity));
                }

                vocabulary.sides.TryAdd(side.Name, side);
            }

            foreach (var face in pack.Faces)
            {
                if (vocabulary.faces.TryGetValue(face.Name, out var existing) && existing.Label != face.Label)
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-007", $"The face '{face.Name}' is already declared with another label.", pack.Identity));
                }

                vocabulary.faces.TryAdd(face.Name, face);
            }

            void Own(string name)
            {
                if (!owner.TryAdd(name, pack))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-007", $"'{name}' is already declared by the {owner[name].Pack} pack.", pack.Identity));
                }
            }

            foreach (var attribute in pack.Attributes)
            {
                Own(attribute.Name);
                vocabulary.attributes.TryAdd(attribute.Name, attribute);
            }

            foreach (var trait in pack.Traits)
            {
                Own(trait.Name);
                vocabulary.traits.TryAdd(trait.Name, trait);
            }

            foreach (var state in pack.States)
            {
                Own(state.Name);
                if (vocabulary.states.TryAdd(state.Name, state))
                {
                    vocabulary.stateOrder.Add(state.Name);
                }
            }

            foreach (var kind in pack.Kinds)
            {
                Own(kind.Name);
                vocabulary.kinds.TryAdd(kind.Name, kind);
            }
        }

        // References are checked once everything is declared, so packs may be listed in any order.
        bool Visible(VocabularyPack from, string name) =>
            owner.TryGetValue(name, out var declaring) && (declaring == from || from.Extends.Any(reference =>
                reference == declaring.Pack || reference.StartsWith(declaring.Pack + "@", StringComparison.Ordinal)));

        foreach (var pack in packs)
        {
            foreach (var attribute in pack.Attributes)
            {
                CheckFaces(vocabulary, attribute.Faces, pack.Identity + " " + attribute.Name, diagnostics);
            }

            foreach (var trait in pack.Traits)
            {
                CheckFaces(vocabulary, trait.Faces, pack.Identity + " " + trait.Name, diagnostics);
            }

            foreach (var state in pack.States.Where(state => state.Face is not null))
            {
                CheckFaces(vocabulary, [state.Face!], pack.Identity + " " + state.Name, diagnostics);
            }

            foreach (var kind in pack.Kinds)
            {
                var path = pack.Identity + " " + kind.Name;
                if (kind.Extends is { } parent && parent != VocabularyNames.RootKind && !Visible(pack, parent))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-005", $"The parent kind '{parent}' is not declared by this pack or a pack it extends.", path));
                }

                CheckFaces(vocabulary, kind.Faces, path, diagnostics);
                foreach (var attribute in kind.Attributes.Where(name => !vocabulary.attributes.ContainsKey(name) || !Visible(pack, name)))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-005", $"The attribute '{attribute}' is not declared by this pack or a pack it extends.", path));
                }

                foreach (var trait in kind.Traits.Where(name => !vocabulary.traits.ContainsKey(name) || !Visible(pack, name)))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-005", $"The trait '{trait}' is not declared by this pack or a pack it extends.", path));
                }
            }

            foreach (var augment in pack.Augments)
            {
                var path = pack.Identity + " augments " + augment.Kind;
                if (!vocabulary.kinds.ContainsKey(augment.Kind) || !Visible(pack, augment.Kind))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-005", $"The augmented kind '{augment.Kind}' is not declared by a pack this pack extends.", path));
                }

                foreach (var attribute in augment.Attributes.Where(name => !vocabulary.attributes.ContainsKey(name) || !Visible(pack, name)))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-005", $"The attribute '{attribute}' is not declared by this pack or a pack it extends.", path));
                }

                foreach (var trait in augment.Traits.Where(name => !vocabulary.traits.ContainsKey(name) || !Visible(pack, name)))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-005", $"The trait '{trait}' is not declared by this pack or a pack it extends.", path));
                }
            }
        }

        if (diagnostics.Count == 0)
        {
            vocabulary.Resolve(packs, diagnostics);
        }

        return new UnitVocabularyResult(diagnostics.Count == 0 ? vocabulary : null, diagnostics);
    }

    /// <summary>
    /// Whether a document's pack reference, such as <c>asl@1.0.0</c>, is served by a loaded pack: the same pack and
    /// major version, at the same or a later minor and patch version. Packs only add declarations within a major version.
    /// </summary>
    public bool Serves(string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var at = reference.IndexOf('@', StringComparison.Ordinal);
        if (at < 0 || !VocabularyPackReader.IsVersion(reference[(at + 1)..]))
        {
            return false;
        }

        return Packs.Any(pack => pack.Pack == reference[..at] && Compatible(pack.Version, reference[(at + 1)..]));
    }

    /// <summary>Whether a loaded version serves a wanted one: the same major version, and not older.</summary>
    private static bool Compatible(string loadedVersion, string wantedVersion) =>
        Version.TryParse(loadedVersion, out var loaded) && Version.TryParse(wantedVersion, out var wanted) &&
        loaded.Major == wanted.Major && loaded >= wanted;

    public bool TryGetKind(string name, out KindDefinition kind) => kinds.TryGetValue(name, out kind!);

    public KindDefinition Kind(string name) =>
        kinds.TryGetValue(name, out var kind) ? kind : throw new KeyNotFoundException($"The kind '{name}' is not declared.");

    public bool HasKind(string name) => kinds.ContainsKey(name);

    /// <summary>Whether <paramref name="kind"/> is <paramref name="ancestor"/> or extends it, directly or not.</summary>
    public bool IsA(string kind, string ancestor) => info.TryGetValue(kind, out var entry) && entry.Ancestors.Contains(ancestor);

    /// <summary>The kind and every kind above it, nearest first, ending with <c>unit</c>.</summary>
    public IReadOnlyList<string> Ancestors(string kind) => Info(kind).Ancestors;

    /// <summary>The kind's depth in the tree: 1 for <c>unit</c>, 2 for a kind directly under it, and so on.</summary>
    public int Depth(string kind) => Info(kind).Ancestors.Count;

    /// <summary>The faces a kind declares, its ancestors' first, in declaration order.</summary>
    public IReadOnlyList<string> Faces(string kind) => Info(kind).Faces;

    public IReadOnlyCollection<string> AcceptedAttributes(string kind) => Info(kind).Attributes;

    public IReadOnlyCollection<string> AcceptedTraits(string kind) => Info(kind).Traits;

    public int? SizeClass(string kind) => Info(kind).SizeClass;

    /// <summary>Whether units of a kind face a hexspine, so a document may give their facing (C3.2).</summary>
    public bool HasFacing(string kind) => Info(kind).Facing;

    /// <summary>Whether units of a kind may give a turret facing apart from their hull facing (D3.12).</summary>
    public bool HasTurret(string kind) => Info(kind).Turret;

    public bool TryGetAttribute(string name, out AttributeDefinition attribute) => attributes.TryGetValue(name, out attribute!);

    public bool TryGetTrait(string name, out TraitDefinition trait) => traits.TryGetValue(name, out trait!);

    public bool TryGetState(string name, out StateDefinition state) => states.TryGetValue(name, out state!);

    public bool TryGetSide(string name, out SideDefinition side) => sides.TryGetValue(name, out side!);

    public bool HasFace(string name) => faces.ContainsKey(name);

    public string FaceLabel(string name) => faces.TryGetValue(name, out var face) ? face.Label : name;

    public string SideLabel(string? name) => name is not null && sides.TryGetValue(name, out var side) ? side.Label : name ?? string.Empty;

    /// <summary>
    /// The attribute a name means for a kind: a qualified name such as <c>asl:firepower</c> exactly, or a local name
    /// such as <c>firepower</c> when exactly one attribute the kind accepts has it. Returns false when the kind accepts
    /// no such attribute; <paramref name="ambiguous"/> is set when several match.
    /// </summary>
    public bool TryResolveAttribute(string kind, string name, out AttributeDefinition attribute, out bool ambiguous)
    {
        ArgumentNullException.ThrowIfNull(name);
        ambiguous = false;
        attribute = null!;
        if (!info.TryGetValue(kind, out var entry))
        {
            return false;
        }

        if (name.Contains(':', StringComparison.Ordinal))
        {
            return entry.Attributes.Contains(name) && attributes.TryGetValue(name, out attribute!);
        }

        var matches = entry.Attributes.Where(accepted => VocabularyNames.Local(accepted) == name).ToArray();
        ambiguous = matches.Length > 1;
        if (matches.Length != 1)
        {
            return false;
        }

        attribute = attributes[matches[0]];
        return true;
    }

    /// <summary>The name a document writes for an attribute of a kind: the local name unless another accepted attribute shares it.</summary>
    public string ShortName(string kind, string attribute) =>
        TryResolveAttribute(kind, VocabularyNames.Local(attribute), out var resolved, out _) && resolved.Name == attribute
            ? VocabularyNames.Local(attribute)
            : attribute;

    /// <summary>The accessible-name template for a kind and face, from the nearest kind that declares one.</summary>
    public string AccessibleTemplate(string kind, string face) => Template(kind, face, definition => definition.AccessibleName);

    /// <summary>The template naming a kind when it is carried as attached equipment.</summary>
    public string AttachedTemplate(string kind, string face) => Template(kind, face, definition => definition.AttachedName);

    private string Template(string kind, string face, Func<KindDefinition, IReadOnlyDictionary<string, string>> select)
    {
        foreach (var name in Ancestors(kind))
        {
            var templates = select(kinds[name]);
            if (templates.TryGetValue(face, out var template) || templates.TryGetValue("default", out template))
            {
                return template;
            }
        }

        return "{kind}";
    }

    private KindInfo Info(string kind) =>
        info.TryGetValue(kind, out var entry) ? entry : throw new KeyNotFoundException($"The kind '{kind}' is not declared.");

    private static void CheckFaces(UnitVocabulary vocabulary, IEnumerable<string> names, string path, List<UnitDiagnostic> diagnostics)
    {
        foreach (var face in names.Where(face => !vocabulary.faces.ContainsKey(face)))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-005", $"The face '{face}' is not declared.", path));
        }
    }

    private void Resolve(IReadOnlyList<VocabularyPack> packs, List<UnitDiagnostic> diagnostics)
    {
        var augments = packs.SelectMany(pack => pack.Augments).ToLookup(augment => augment.Kind, StringComparer.Ordinal);
        foreach (var kind in kinds.Keys)
        {
            var chain = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var current = kind; ; current = kinds[current].Extends ?? VocabularyNames.RootKind)
            {
                if (!seen.Add(current))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-009", $"The kind '{kind}' extends itself through '{current}'.", kind));
                    return;
                }

                chain.Add(current);
                if (current == VocabularyNames.RootKind)
                {
                    break;
                }
            }

            var faceList = new List<string>();
            var attributeSet = new HashSet<string>(StringComparer.Ordinal);
            var traitSet = new HashSet<string>(StringComparer.Ordinal);
            int? sizeClass = null;
            var facing = false;
            var turret = false;
            for (var index = chain.Count - 1; index >= 0; index--)
            {
                var definition = kinds[chain[index]];
                faceList.AddRange(definition.Faces.Where(face => !faceList.Contains(face)));
                attributeSet.UnionWith(definition.Attributes);
                traitSet.UnionWith(definition.Traits);
                foreach (var augment in augments[definition.Name])
                {
                    attributeSet.UnionWith(augment.Attributes);
                    traitSet.UnionWith(augment.Traits);
                }

                sizeClass = definition.SizeClass ?? sizeClass;
                facing = definition.Facing ?? facing;
                turret = definition.Turret ?? turret;
            }

            info[kind] = new KindInfo(chain, faceList, attributeSet, traitSet, sizeClass, facing, turret);
        }
    }

    private sealed record KindInfo(IReadOnlyList<string> Ancestors, IReadOnlyList<string> Faces, IReadOnlySet<string> Attributes,
        IReadOnlySet<string> Traits, int? SizeClass, bool Facing, bool Turret);
}
