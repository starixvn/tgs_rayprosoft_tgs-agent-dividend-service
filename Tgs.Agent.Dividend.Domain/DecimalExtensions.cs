namespace Tgs.Agent.Dividend.Domain;

public static class DecimalExtensions
{
    public static decimal Negate(this decimal d)
    {
        return decimal.Negate(d);
    }

    public static decimal Truncate(this decimal d, int digits)
    {
        var step = (decimal)Math.Pow(10, digits);
        return decimal.Truncate(d * step) / step;
    }
}