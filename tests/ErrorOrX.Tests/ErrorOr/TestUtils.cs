namespace ErrorOrX.Tests.ErrorOr;

public static class Convert
{
    public static ErrorOr<string> ToString(int num) => num.ToString();

    public static ErrorOr<int> ToInt(string str)
    {
        if (int.TryParse(str, out var value))
        {
            return value;
        }
        return Error.Validation("Convert.ToInt", $"Cannot parse '{str}' as int");
    }

    public static Task<ErrorOr<int>> ToIntAsync(string str) => Task.FromResult(ToInt(str));

    public static Task<ErrorOr<string>> ToStringAsync(int num) => Task.FromResult(ErrorOrFactory.From(num.ToString()));
}
