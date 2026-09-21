using System.Text.Json;

namespace LimboDancer.Abstractions.Actions;

public sealed class EffectDescriptor
{
    public EffectDescriptor(
        string id,
        string effectType,
        JsonElement definition,
        bool requiredVerification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(effectType);

        Id = id;
        EffectType = effectType;
        Definition = definition.Clone();
        RequiredVerification = requiredVerification;
    }

    public string Id { get; }

    public string EffectType { get; }

    public JsonElement Definition { get; }

    public bool RequiredVerification { get; }
}
