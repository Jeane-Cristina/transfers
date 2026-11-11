namespace Transfers.Application.Common;

public static class Cpf
{
    public static string Normalize(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());

    public static bool IsValid(string digits)
    {
        if (digits.Length != 11 || digits.Distinct().Count() == 1)
            return false;

        return CheckDigit(digits, 9) == digits[9] - '0' && CheckDigit(digits, 10) == digits[10] - '0';
    }

    public static int CheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (digits[i] - '0') * (length + 1 - i);

        var remainder = sum * 10 % 11;
        return remainder == 10 ? 0 : remainder;
    }
}
