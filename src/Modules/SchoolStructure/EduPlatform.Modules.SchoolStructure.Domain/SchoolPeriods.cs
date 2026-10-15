namespace EduPlatform.Modules.SchoolStructure.Domain;

public static class SchoolPeriods
{
    private static readonly (TimeOnly Start, TimeOnly End)[] Bells =
    [
        (new TimeOnly(8, 0), new TimeOnly(8, 40)),
        (new TimeOnly(8, 50), new TimeOnly(9, 30)),
        (new TimeOnly(9, 50), new TimeOnly(10, 30)),
        (new TimeOnly(10, 40), new TimeOnly(11, 20)),
        (new TimeOnly(11, 30), new TimeOnly(12, 10)),
        (new TimeOnly(12, 20), new TimeOnly(13, 0)),
        (new TimeOnly(13, 10), new TimeOnly(13, 50)),
        (new TimeOnly(14, 0), new TimeOnly(14, 40)),
    ];

    public static int Count => Bells.Length;

    public static (TimeOnly Start, TimeOnly End) For(int period)
    {
        if (period < ScheduleSlot.MinPeriod || period > Bells.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(period));
        }

        return Bells[period - 1];
    }
}
