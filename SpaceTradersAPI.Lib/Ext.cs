using System.Diagnostics;
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

    extension<T>(Task<Result<T>> task)
    where T : V2.IAwaitable
    {
        public Task Await()
        => task.ValueOrThrowAsync()
            .ContinueWith(t => t.Result.Await(), TaskContinuationOptions.ExecuteSynchronously)
            .Unwrap();
    }

    extension<T>(IAsyncEnumerable<T> values)
    {
        public async IAsyncEnumerable<TResult> MapValuesAsync<TResult>(Func<T, TResult> mapper)
        {
            await foreach (var item in values)
                yield return mapper(item);
        }
    }

    extension<T, TAccount>(IAsyncEnumerable<T> values)
    where T : V2.IInitWith<T, TAccount>
    where TAccount : IAccount
    {
        public async IAsyncEnumerable<T> MapInitAsync(TAccount account)
        {
            await foreach (var item in values)
                yield return item.InitWith(account);
        }
    }
    extension<T, TAccount>(Task<Result<T>> task)
    where T : V2.IInitWith<T, TAccount>
    where TAccount : IAccount
    {
        public Task<Result<T>> MapInitAsync(TAccount account)
        => task.ContinueWith(t => new Result<T>(t.Result.ValueOrThrow.InitWith(account)), TaskContinuationOptions.ExecuteSynchronously);
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

        public (int fuelCost, int duration)? CalculateTravelCost(V2.IPosition destination, V2.ShipNavFlightMode flightMode, int engineSpeed)
        {
            // https://github.com/SpaceTradersAPI/api-docs/wiki/Travel-Fuel-and-Time
            var dist = position.DistanceWith(destination);

            var (fuel, mult) = flightMode switch
            {
                V2.ShipNavFlightMode.Drift => (1, 250),
                V2.ShipNavFlightMode.Stealth => ((int)Math.Max(1, Math.Round(dist, MidpointRounding.AwayFromZero)), 30),
                V2.ShipNavFlightMode.Cruise => ((int)Math.Max(1, Math.Round(dist, MidpointRounding.AwayFromZero)), 25),
                V2.ShipNavFlightMode.Burn => ((int)Math.Max(2, 2 * Math.Round(dist, MidpointRounding.AwayFromZero)), 12.5d),
                _ => throw new UnreachableException(),
            };

            return (fuel, (int)Math.Round(Math.Round(Math.Max(1, dist), MidpointRounding.AwayFromZero) * (mult / engineSpeed) + 15, MidpointRounding.AwayFromZero));
        }

        static long Square(int a)
        => a * (long)a;
    }
}
