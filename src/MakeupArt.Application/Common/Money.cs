namespace MakeupArt.Application.Common;

public static class Money
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static string Format(decimal value, string currency = "INR")
        => currency == "INR" ? $"₹{value:N2}" : $"{currency} {value:N2}";
}
