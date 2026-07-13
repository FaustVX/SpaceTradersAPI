using SpaceTradersAPI.Lib.Models;
using SpaceTradersAPI.Lib.Responses;

namespace SpaceTradersAPI.Lib;

public static class Ext
{
    extension<T>(Task<Result<T>> task)
    {
        public Task<T> ValueOrThrowAsync()
        => task.ContinueWith(t => t.Result.ValueOrThrow, TaskContinuationOptions.ExecuteSynchronously);

        public Task<Result<TResult>> MapvalueAsync<TResult>(Func<T, TResult> mapper)
        => task.ContinueWith(t => t.Result.MapValue(mapper), TaskContinuationOptions.ExecuteSynchronously);

        public Task<Result<T>> MapErrorAsync(Func<Error, Task<Result<T>>> errorMapper)
        => task.ContinueWith(t => t.Result.MapError(e => errorMapper(e).Result), TaskContinuationOptions.ExecuteSynchronously);
    }

    extension<T>(IAsyncEnumerable<T> values)
    {
        public async IAsyncEnumerable<TResult> MapValues<TResult>(Func<T, TResult> mapper)
        {
            await foreach (var item in values)
                yield return mapper(item);
        }
    }

    extension<T>(T @enum)
    where T : struct, Enum
    {
        public string ToUpperCase()
        => @enum.ToString().ToUpperInvariant();
    }

    extension(TimeSpan)
    {
        public static TimeSpan Min(TimeSpan val1, TimeSpan val2)
        => val1 <= val2 ? val1 : val2;

        public static TimeSpan Max(TimeSpan val1, TimeSpan val2)
        => val1 >= val2 ? val1 : val2;
    }

    extension(V2.IPosition position)
    {
        public double DistanceWith(V2.IPosition other)
        => Math.Sqrt(position.DistanceWithSquared(other));

        public long DistanceWithSquared(V2.IPosition other)
        => Square(other.X - position.X) + Square(other.Y - position.Y);

        static long Square(int a)
        => a * (long)a;
    }
}
