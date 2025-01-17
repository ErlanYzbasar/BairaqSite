using System.Data;
using System.Text;
using COMMON.DiffMatchPatch;
using MODEL;
using MODEL.Enums;

namespace COMMON;

public static class DiffHelper
{
    // private const int ContextLength = 5;
    private static diff_match_patch DiffMatchPatch => new();
    private static List<Operation> SavingOperationList => new() { Operation.DELETE, Operation.INSERT };

    public static List<Diff> GetDiffList(string oldText, string newText)
    {
        var diffList = DiffMatchPatch.diff_main(oldText, newText, false);
        diffList = DiffMatchPatch
            .diff_cleanupSemantic(diffList)
            // .AddContext()
            .FindAll(x => SavingOperationList.Contains(x.Operation));
        diffList.ForEach(x => x.Text = x.Text.Trim());
        return diffList;
    }

    // private static List<Diff> AddContext(this List<Diff> diffList)
    // {
    //     var result = new List<Diff>();
    //     for (var i = 0; i < diffList.Count; i++)
    //     {
    //         var diff = diffList[i];
    //         if (!SavingOperationList.Contains(diff.Operation)) continue;
    //
    //         var prevContext = i > 0 ? GetContext(diffList[i - 1].Text, false) : "";
    //         var nextContext = i < diffList.Count - 1 ? GetContext(diffList[i + 1].Text, true) : "";
    //
    //         result.Add(new Diff(diff.Operation, prevContext + diff.Text + nextContext));
    //     }
    //
    //     return result;
    // }
    //
    // private static string GetContext(string text, bool fromStart)
    // {
    //     return fromStart
    //         ? text.Substring(0, Math.Min(ContextLength, text.Length))
    //         : text.Substring(Math.Max(0, text.Length - ContextLength));
    // }
}