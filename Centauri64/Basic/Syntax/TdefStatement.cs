namespace Centauri64.Basic.Syntax;

public sealed class TdefStatement : Statement
{
    public Expression TileId { get; }
    public Expression AssetName { get; }

    public TdefStatement(
        Expression tileId,
        Expression assetName)
    {
        TileId = tileId;
        AssetName = assetName;
    }
}
