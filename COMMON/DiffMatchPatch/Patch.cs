using System.Text;
using MODEL.Enums;

namespace COMMON.DiffMatchPatch;

/**
 * Class representing one patch operation.
 */
public class Patch
{
    public readonly List<Diff> diffs = new();
    public int start1;
    public int start2;
    public int length1;
    public int length2;

    /**
     * Emulate GNU diff's format.
     * Header: @@ -382,8 +481,9 @@
     * Indices are printed as 1-based, not 0-based.
     * @return The GNU diff string.
     */
    public override string ToString()
    {
        string coords1, coords2;
        if (length1 == 0)
        {
            coords1 = start1 + ",0";
        }
        else if (length1 == 1)
        {
            coords1 = Convert.ToString(start1 + 1);
        }
        else
        {
            coords1 = start1 + 1 + "," + length1;
        }

        if (length2 == 0)
        {
            coords2 = start2 + ",0";
        }
        else if (length2 == 1)
        {
            coords2 = Convert.ToString(start2 + 1);
        }
        else
        {
            coords2 = start2 + 1 + "," + length2;
        }

        var text = new StringBuilder();
        text.Append("@@ -").Append(coords1).Append(" +").Append(coords2)
            .Append(" @@\n");
        // Escape the body of the patch with %xx notation.
        foreach (var aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.INSERT:
                    text.Append('+');
                    break;
                case Operation.DELETE:
                    text.Append('-');
                    break;
                case Operation.EQUAL:
                    text.Append(' ');
                    break;
            }

            text.Append(diff_match_patch.EncodeURI(aDiff.Text)).Append("\n");
        }

        return text.ToString();
    }
}