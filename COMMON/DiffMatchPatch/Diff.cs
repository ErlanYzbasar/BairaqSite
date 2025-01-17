using MODEL.Enums;

namespace COMMON.DiffMatchPatch;

/**
 * Class representing one diff operation.
 */
public class Diff
{
    // One of: INSERT, DELETE or EQUAL.
    public Operation Operation { get; set; }

    // The text associated with this diff operation.
    public string Text { get; set; }

    /**
     * Constructor.  Initializes the diff with the provided values.
     * @param operation One of INSERT, DELETE or EQUAL.
     * @param text The text being applied.
     */
    public Diff(Operation operation, string text)
    {
        // Construct a diff with the specified operation and text.
        Operation = operation;
        Text = text;
    }

    /**
     * Display a human-readable version of this Diff.
     * @return text version.
     */
    public override string ToString()
    {
        var prettyText = Text.Replace('\n', '\u00b6');
        return "Diff(" + Operation + ",\"" + prettyText + "\")";
    }

    /**
     * Is this Diff equivalent to another Diff?
     * @param d Another Diff to compare against.
     * @return true or false.
     */
    public override bool Equals(object obj)
    {
        // If parameter cannot be cast to Diff return false.
        if (obj is not Diff p)
        {
            return false;
        }

        // Return true if the fields match.
        return p.Operation == Operation && p.Text == Text;
    }

    public bool Equals(Diff obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // Return true if the fields match.
        return obj.Operation == Operation && obj.Text == Text;
    }

    public override int GetHashCode()
    {
        return Text.GetHashCode() ^ Operation.GetHashCode();
    }
}