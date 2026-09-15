namespace Centauri64.Basic;

public readonly struct BasicValue
{
    public int Integer { get; }
    public string? String { get; }

    public bool IsInteger { get; }
    public bool IsString => !IsInteger;

    public BasicValue(int value)
    {
        Integer = value;
        String = null;
        IsInteger = true;
    }

    public BasicValue(string value)
    {
        Integer = 0;
        String = value;
        IsInteger = false;
    }

    public override string ToString()
    {
        return IsInteger
            ? Integer.ToString()
            : String ?? string.Empty;
    }
}