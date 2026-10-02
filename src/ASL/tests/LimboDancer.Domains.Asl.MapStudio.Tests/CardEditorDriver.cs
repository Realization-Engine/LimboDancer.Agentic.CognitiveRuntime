using Bunit;
using EditorPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.CardEditor;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Drives the card editor's forms (pass 28, ruling R28.1) as a player fills them in.</summary>
internal static class CardEditorDriver
{
    /// <summary>Sets the card's boards, one row each: <c>bd01</c>, or <c>bd04@0,0</c> and <c>bd02@0,1/r</c> (column, row, /r when turned).</summary>
    public static void Boards(IRenderedComponent<EditorPage> editor, params string[] boards)
    {
        while (editor.FindAll("#edit-boards tbody tr").Count > boards.Length)
        {
            editor.Find($"#edit-board-{editor.FindAll("#edit-boards tbody tr").Count - 1}-remove").Click();
        }

        while (editor.FindAll("#edit-boards tbody tr").Count < boards.Length)
        {
            editor.Find("#edit-board-add").Click();
        }

        for (var index = 0; index < boards.Length; index++)
        {
            var token = boards[index];
            var at = token.IndexOf('@', StringComparison.Ordinal);
            var slot = at < 0 ? "0,0" : token[(at + 1)..];
            var reversed = slot.EndsWith("/r", StringComparison.Ordinal);
            var parts = (reversed ? slot[..^2] : slot).Split(',');
            editor.Find($"#edit-board-{index}").Change(at < 0 ? token : token[..at]);
            editor.Find($"#edit-board-{index}-column").Change(parts[0]);
            editor.Find($"#edit-board-{index}-row").Change(parts.Length > 1 ? parts[1] : string.Empty);
            editor.Find($"#edit-board-{index}-reversed").Change(reversed);
        }
    }

    /// <summary>Adds an OB group to a side with one building area of one hex and one counter line there.</summary>
    public static void AddGroup(IRenderedComponent<EditorPage> editor, int side, string name, string elr, string hex, string definition)
    {
        editor.Find($"#edit-group-add-{side}").Click();
        var group = editor.FindAll($"#edit-side-fields-{side} .card-group-fields").Count - 1;
        var id = $"#edit-group-{side}-{group}";
        editor.Find($"{id}-name").Change(name);
        editor.Find($"{id}-elr").Change(elr);
        editor.Find($"{id}-area-add").Click();
        editor.Find($"{id}-area-0-id").Change(hex);
        editor.Find($"{id}-area-0-hexes").Change(hex);
        editor.Find($"{id}-pick-definition").Change(definition);
        editor.Find($"{id}-pick-area").Change(hex);
        editor.Find($"{id}-pick-add").Click();
    }

    /// <summary>Adds an SSR with its status and tokens.</summary>
    public static void AddRule(IRenderedComponent<EditorPage> editor, string text, string status, string tokens = "")
    {
        editor.Find("#edit-ssr-add").Click();
        var at = editor.FindAll(".card-rules-list > li").Count - 1;
        editor.Find($"#edit-ssr-{at}-text").Change(text);
        editor.Find($"#edit-ssr-{at}-status").Change(status);
        if (tokens.Length > 0)
        {
            editor.Find($"#edit-ssr-{at}-tokens").Change(tokens);
        }
    }
}
