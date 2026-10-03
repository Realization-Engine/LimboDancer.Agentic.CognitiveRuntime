namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A marker for component tests (pass 29): when it is registered, the Play page asks the gate without first yielding to draw its busy state, so
/// a proposal and its confirmation run on the test's own thread and their result is there when the click returns. Without it a test's next
/// click can be queued behind the proposal's continuation and return before it is handled. The Studio never registers it.
/// </summary>
public sealed class ImmediateProposals;
