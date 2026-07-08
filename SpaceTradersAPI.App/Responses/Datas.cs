using System.Diagnostics;
using System.Text.Json.Nodes;

namespace SpaceTradersAPI.App.Responses;

public record class Datas<T>(T Data);

public record class DatasWithMeta<T>(T Data, Meta Meta);

public record class Meta(int Total, int Page, int Limit);

public record class ShipNavWraper(Models.V2.ShipNav Nav);

public record class ErrorResponse(Error Error);

public record class Error(int Code, string Message, JsonNode Data)
{
    public static implicit operator Exception(Error err)
    => new(err.ToString());
}

public readonly union Result<T>(T, Error)
{
    public override string ToString()
    => this switch
    {
        T t => t.ToString()!,
        Error err => err.ToString(),
        null => throw new UnreachableException(),
    };
}
