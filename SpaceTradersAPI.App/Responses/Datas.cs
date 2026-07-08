namespace SpaceTradersAPI.App.Responses;

public record class Datas<T>(T Data);

public record class DatasWithMeta<T>(T Data, Meta Meta);

public record class Meta(int Total, int Page, int Limit);

public record class ShipNavWraper(Models.V2.ShipNav Nav);
