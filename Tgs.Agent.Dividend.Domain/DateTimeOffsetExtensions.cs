using Tgs.Agent.Dividend.Domain.Enums;

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

    public static (DateTimeOffset, DateTimeOffset) GetInterval(SettlementIntervalType type,
        DateTimeOffset startDate,
        int? dayOfMonth)
    {
        return type switch
        {
            SettlementIntervalType.Daily => (startDate, startDate),
            SettlementIntervalType.Weekly => (startDate, startDate.GetSunday()),
            SettlementIntervalType.DoubleWeekly => (startDate, startDate.Day > 15 ? startDate.GetLastDayOfMonth() : new DateTimeOffset(startDate.Year, startDate.Month, 15,0,0,0, startDate.Offset)),
            SettlementIntervalType.Monthly => (startDate,
                dayOfMonth == null
                    ? startDate.GetLastDayOfMonth()
                    : startDate.AddMonths(1).GetSpecifyDayOfMonth(dayOfMonth.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}