namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// An editor tool (Architecture and Rendering Design, section 6.2) and the gesture the browser collects for it:
/// <c>select</c> and <c>click</c> report clicks, <c>move</c> reports a drag, and <c>polygon</c> and <c>polyline</c>
/// report a completed list of points.
/// </summary>
public sealed record EditorTool(string Key, string Label, string Gesture, string Help)
{
    public const string Select = "select";
    public const string Move = "move";
    public const string Area = "area";
    public const string Elevation = "elevation";
    public const string Linear = "linear";
    public const string Building = "building";
    public const string Hexside = "hexside";
    public const string Stairway = "stairway";
    public const string Annotation = "annotation";

    public static IReadOnlyList<EditorTool> All
    {
        get;
    } =
    [
        new(Select, "Select", "select", "Click a feature to select it and a hex to inspect it. Drag to pan."),
        new(Move, "Move", "move", "Drag to move the selected feature by whole pixels."),
        new(Area, "Area", "polygon", "Click vertices; double-click or Enter closes. Backspace removes the last point."),
        new(Elevation, "Elevation", "polygon", "Draw a hill level as a polygon; set the level in the options."),
        new(Linear, "Linear", "polyline", "Click points along a road or stream; double-click or Enter ends it."),
        new(Building, "Building", "click", "Click a hex to place a footprint from the kit: centered, span, or flush."),
        new(Hexside, "Hexside", "click", "Click near a hexside to add or remove wall, hedge, or other hexside terrain."),
        new(Stairway, "Stairway", "click", "Click a hex to toggle its stairway."),
        new(Annotation, "Annotation", "click", "Click near a hexside to toggle a slope, railroad embankment, or partial orchard."),
    ];

    public static EditorTool Get(string key) => All.FirstOrDefault(tool => tool.Key == key) ?? All[0];
}
