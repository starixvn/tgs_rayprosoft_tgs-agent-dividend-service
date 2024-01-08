using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.UnitTest;

public class DateTimeOffsetExtensionsTests
{
    [TestCase("2023-12-24 00:00:00+8", "2023-12-24 00:00:00+8", "2023-12-24 00:00:00+8")]
    [TestCase("2023-12-25 00:00:00+8", "2023-12-25 00:00:00+8", "2023-12-25 00:00:00+8")]
    [TestCase("2024-01-01 00:00:00+8", "2024-01-01 00:00:00+8", "2024-01-01 00:00:00+8")]
    public void GetInterval_Daily(string startDateString, string resultStartDateString, string resultEndDateString)
    {
        var startDate = DateTimeOffset.Parse(startDateString);
        var (start, end) = DateTimeOffsetExtensions.GetInterval(SettlementIntervalType.Daily, startDate, null);

        Assert.That(start, Is.EqualTo(DateTimeOffset.Parse(resultStartDateString)));
        Assert.That(end, Is.EqualTo(DateTimeOffset.Parse(resultEndDateString)));
    }

    [TestCase("2023-12-24 00:00:00+8", "2023-12-24 00:00:00+8", "2023-12-24 00:00:00+8")]
    [TestCase("2023-12-18 00:00:00+8", "2023-12-18 00:00:00+8", "2023-12-24 00:00:00+8")]
    [TestCase("2023-12-25 00:00:00+8", "2023-12-25 00:00:00+8", "2023-12-31 00:00:00+8")]
    [TestCase("2024-01-02 00:00:00+8", "2024-01-02 00:00:00+8", "2024-01-07 00:00:00+8")]
    public void GetInterval_Weekly(string startDateString, string resultStartDateString, string resultEndDateString)
    {
        var startDate = DateTimeOffset.Parse(startDateString);
        var (start, end) = DateTimeOffsetExtensions.GetInterval(SettlementIntervalType.Weekly, startDate, null);

        Assert.That(start, Is.EqualTo(DateTimeOffset.Parse(resultStartDateString)));
        Assert.That(end, Is.EqualTo(DateTimeOffset.Parse(resultEndDateString)));
    }

    [TestCase("2023-12-01 00:00:00+8", "2023-12-01 00:00:00+8", "2023-12-31 00:00:00+8")]
    [TestCase("2024-01-01 00:00:00+8", "2024-01-01 00:00:00+8", "2024-01-31 00:00:00+8")]
    [TestCase("2024-02-01 00:00:00+8", "2024-02-01 00:00:00+8", "2024-02-29 00:00:00+8")]
    public void GetInterval_Monthly(string startDateString, string resultStartDateString, string resultEndDateString)
    {
        var startDate = DateTimeOffset.Parse(startDateString);
        var (start, end) = DateTimeOffsetExtensions.GetInterval(SettlementIntervalType.Monthly, startDate, null);

        Assert.That(start, Is.EqualTo(DateTimeOffset.Parse(resultStartDateString)));
        Assert.That(end, Is.EqualTo(DateTimeOffset.Parse(resultEndDateString)));
    }

    [TestCase("2023-12-01 00:00:00+8", "2023-12-01 00:00:00+8", "2024-01-05 00:00:00+8", 5)]
    [TestCase("2023-12-06 00:00:00+8", "2023-12-06 00:00:00+8", "2024-01-05 00:00:00+8", 5)]
    [TestCase("2024-01-06 00:00:00+8", "2024-01-06 00:00:00+8", "2024-02-05 00:00:00+8", 5)]
    [TestCase("2024-02-06 00:00:00+8", "2024-02-06 00:00:00+8", "2024-03-10 00:00:00+8", 10)]
    [TestCase("2024-03-11 00:00:00+8", "2024-03-11 00:00:00+8", "2024-04-10 00:00:00+8", 10)]
    [TestCase("2024-04-11 00:00:00+8", "2024-04-11 00:00:00+8", "2024-05-03 00:00:00+8", 3)]
    public void GetInterval_Monthly_With_Defined_Day_Of_Month(string startDateString, string resultStartDateString, string resultEndDateString, int dayOfMonth)
    {
        var startDate = DateTimeOffset.Parse(startDateString);
        var (start, end) = DateTimeOffsetExtensions.GetInterval(SettlementIntervalType.Monthly, startDate, dayOfMonth);

        Assert.That(start, Is.EqualTo(DateTimeOffset.Parse(resultStartDateString)));
        Assert.That(end, Is.EqualTo(DateTimeOffset.Parse(resultEndDateString)));
    }

    [TestCase("2023-12-06 00:00:00+8", "2023-12-06 00:00:00+8", "2024-01-05 00:00:00+8", 5)]
    [TestCase("2024-01-11 00:00:00+8", "2024-01-11 00:00:00+8", "2024-02-10 00:00:00+8", 10)]
    public void GetInterval_Monthly_With_After_Defined_Day_Of_Month(string startDateString, string resultStartDateString, string resultEndDateString, int dayOfMonth)
    {
        var startDate = DateTimeOffset.Parse(startDateString);
        var (start, end) = DateTimeOffsetExtensions.GetInterval(SettlementIntervalType.Monthly, startDate, dayOfMonth);

        Assert.That(start, Is.EqualTo(DateTimeOffset.Parse(resultStartDateString)));
        Assert.That(end, Is.EqualTo(DateTimeOffset.Parse(resultEndDateString)));
    }

    [TestCase("2023-12-06 00:00:00+8", "2023-12-06 00:00:00+8", "2023-12-15 00:00:00+8")]
    [TestCase("2024-01-11 00:00:00+8", "2024-01-11 00:00:00+8", "2024-01-15 00:00:00+8")]
    [TestCase("2024-01-16 00:00:00+8", "2024-01-16 00:00:00+8", "2024-01-31 00:00:00+8")]
    [TestCase("2024-02-16 00:00:00+8", "2024-02-16 00:00:00+8", "2024-02-29 00:00:00+8")]
    [TestCase("2023-02-16 00:00:00+8", "2023-02-16 00:00:00+8", "2023-02-28 00:00:00+8")]
    public void GetInterval_Double_Weekly(string startDateString, string resultStartDateString, string resultEndDateString)
    {
        var startDate = DateTimeOffset.Parse(startDateString);
        var (start, end) = DateTimeOffsetExtensions.GetInterval(SettlementIntervalType.DoubleWeekly, startDate, null);

        Assert.That(start, Is.EqualTo(DateTimeOffset.Parse(resultStartDateString)));
        Assert.That(end, Is.EqualTo(DateTimeOffset.Parse(resultEndDateString)));
    }


    [TestCase("2024-01-07 19:00:00+0", "2024-01-01", "2024-01-07")]
    public void Get_Weekly_DateOnly_For_From_And_To(string startDateString, string resultStartDateString, string resultEndDateString)
    {
        var startDate = DateTimeOffset.Parse(startDateString).ToOffset(TimeSpan.FromHours(8)).AddDays(-1);

        var from = DateOnly.FromDateTime(startDate.GetMonday().DateTime);

        var to = DateOnly.FromDateTime(startDate.GetSunday().DateTime);

        Assert.That(from, Is.EqualTo(DateOnly.Parse(resultStartDateString)));
        Assert.That(to, Is.EqualTo(DateOnly.Parse(resultEndDateString)));
    }

    [TestCase("2023-12-31 19:00:00+0", "2023-12-01 00:00:00+8", "2023-12-31 00:00:00+8")]
    [TestCase("2024-01-31 19:00:00+0", "2024-01-01 00:00:00+8", "2024-01-31 00:00:00+8")]
    public void Get_Monthly_DateOnly_For_From_And_To(string startDateString, string resultStartDateString, string resultEndDateString)
    {
        var startDate = DateTimeOffset.Parse(startDateString).ToOffset(TimeSpan.FromHours(8));

        var reportDate = new DateTimeOffset(startDate.Year,
            startDate.Month,
            startDate.Day,
            0,
            0,
            0,
            startDate.Offset);

        var monthFrom = reportDate.AddMonths(-1).GetFirstDayOfMonth();

        var monthTo = reportDate.AddMonths(-1).GetLastDayOfMonth();

        Assert.That(monthFrom, Is.EqualTo(DateTimeOffset.Parse(resultStartDateString)));
        Assert.That(monthTo, Is.EqualTo(DateTimeOffset.Parse(resultEndDateString)));
    }
}