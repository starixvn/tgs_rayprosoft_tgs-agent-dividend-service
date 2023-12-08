namespace Tgs.Agent.Dividend.Domain;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset GetFirstDayOfMonth(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.AddDays(1 - dateTimeOffset.Day);
    }

    public static DateTimeOffset GetLastDayOfMonth(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.GetFirstDayOfMonth().AddMonths(1).AddDays(-1);
    }

    public static DateTimeOffset GetMonday(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.Subtract(
            new TimeSpan(((int)dateTimeOffset.DayOfWeek == 0 ? 7 : (int)dateTimeOffset.DayOfWeek) - 1, 0, 0, 0));
    }

    public static DateTimeOffset GetSunday(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.Subtract(
            new TimeSpan(((int)dateTimeOffset.DayOfWeek == 0 ? 7 : (int)dateTimeOffset.DayOfWeek) - 7, 0, 0, 0));
    }

    public static DateTimeOffset GetSpecifyDayOfMonth(this DateTimeOffset dateTimeOffset, int day)
    {
        return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, day, 0, 0, 0, dateTimeOffset.Offset);
    }
}