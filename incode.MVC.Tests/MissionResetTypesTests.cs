namespace incode.MVC.Tests;

public class MissionResetTypesTests
{
    [Theory]
    [InlineData("daily")]
    [InlineData("Weekly")]
    [InlineData(" monthly ")]
    [InlineData("YEARLY")]
    public void Repeating_types_are_not_one_time(string type) => Assert.False(MissionResetTypes.IsOneTime(type));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("once")]
    [InlineData("none")]
    [InlineData("something-unknown")]
    public void Other_values_are_one_time(string? type) => Assert.True(MissionResetTypes.IsOneTime(type));

    [Fact]
    public void One_time_cycle_start_is_fixed_not_today()
        => Assert.Equal(new DateTime(2000, 1, 1), MissionResetTypes.OneTimeCycleStart);
}
