namespace COMMON;

public class ImgSize
{
    private const char ImgSizeSeperator = '_';
    public static ImgSize Big => new("big");
    public static ImgSize Middle => new("middle");
    public static ImgSize Small => new("small");

    private string Value { get; }

    private ImgSize(string value)
    {
        Value = value;
    }

    private static bool IsImgSize(string str)
    {
        return Big.Equals(str) || Middle.Equals(str) || Small.Equals(str);
    }

    public static string ConvertImgSize(string path, ImgSize imgSize)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        if (!path.Contains(ImgSizeSeperator)) return path;
        var fileNameArr = Path.GetFileNameWithoutExtension(path).Split(ImgSizeSeperator);
        if (fileNameArr.Length != 2 || !IsImgSize(fileNameArr[1])) return path;

        return PathHelper.Combine(Path.GetDirectoryName(path),
            fileNameArr[0] + ImgSizeSeperator + imgSize + Path.GetExtension(path));
    }

    #region Others

    public override string ToString()
    {
        return Value;
    }

    private bool Equals(ImgSize other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
    }

    private bool Equals(string other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(Value, other);
    }

    public override bool Equals(object obj)
    {
        return obj is ImgSize other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static bool operator ==(ImgSize left, ImgSize right)
    {
        return left == null ? right == null : left.Equals(right);
    }

    public static bool operator !=(ImgSize left, ImgSize right)
    {
        return !(left == right);
    }

    #endregion
}