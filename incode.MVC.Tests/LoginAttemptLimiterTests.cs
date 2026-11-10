using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Tests;

public class LoginAttemptLimiterTests
{
    private static LoginAttemptLimiter Create() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void Locks_out_after_max_failures()
    {
        var limiter = Create();

        for (var i = 0; i < LoginAttemptLimiter.MaxFailures - 1; i++)
            limiter.RecordFailure("a@example.com");
        Assert.False(limiter.IsLockedOut("a@example.com"));

        limiter.RecordFailure("a@example.com");
        Assert.True(limiter.IsLockedOut("a@example.com"));
    }

    [Fact]
    public void Email_is_case_and_whitespace_insensitive()
    {
        var limiter = Create();
        for (var i = 0; i < LoginAttemptLimiter.MaxFailures; i++)
            limiter.RecordFailure("A@Example.com ");

        Assert.True(limiter.IsLockedOut("a@example.com"));
    }

    [Fact]
    public void Reset_clears_the_counter_and_accounts_are_independent()
    {
        var limiter = Create();
        for (var i = 0; i < LoginAttemptLimiter.MaxFailures; i++)
            limiter.RecordFailure("a@example.com");

        Assert.False(limiter.IsLockedOut("b@example.com"));

        limiter.Reset("a@example.com");
        Assert.False(limiter.IsLockedOut("a@example.com"));
    }
}
