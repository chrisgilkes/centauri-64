using System;

using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private ExecutionResult ExecuteTdef(TdefStatement statement)
    {
        var tileId = Evaluate(statement.TileId);
        var assetName = Evaluate(statement.AssetName);

        if (!tileId.IsInteger)
        {
            throw new InvalidOperationException(
                "TDEF id must be numeric.");
        }

        if (!assetName.IsString)
        {
            throw new InvalidOperationException(
                "TDEF name must be a string.");
        }

        _machine.DefineTile(tileId.Integer, assetName.String!);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteMap(MapStatement statement)
    {
        var columns = Evaluate(statement.Columns);
        var rows = Evaluate(statement.Rows);

        if (!columns.IsInteger || !rows.IsInteger)
        {
            throw new InvalidOperationException(
                "MAP expects numeric columns and rows.");
        }

        if (!_arrays.TryGetValue(statement.ArrayName, out var array))
        {
            throw new InvalidOperationException(
                $"Array {statement.ArrayName} has not been DIMensioned.");
        }

        _machine.SetTileMap(
            array,
            columns.Integer,
            rows.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteLoadMap(LoadMapStatement statement)
    {
        var name = Evaluate(statement.Name);

        if (!name.IsString)
        {
            throw new InvalidOperationException(
                "LOADMAP expects a string name.");
        }

        _machine.LoadMap(name.String!);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteCamera(CameraStatement statement)
    {
        if (statement.Follow)
        {
            var spriteIndex = Evaluate(statement.SpriteIndex!);

            if (!spriteIndex.IsInteger)
            {
                throw new InvalidOperationException(
                    "CAMERA FOLLOW expects a sprite number.");
            }

            _machine.SetCameraFollow(spriteIndex.Integer);

            return ExecutionResult.Continue();
        }

        var x = Evaluate(statement.X!);
        var y = Evaluate(statement.Y!);

        if (!x.IsInteger || !y.IsInteger)
        {
            throw new InvalidOperationException(
                "CAMERA expects numeric coordinates.");
        }

        _machine.SetCamera(x.Integer, y.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteCamOff()
    {
        _machine.ClearCamera();

        return ExecutionResult.Continue();
    }

    private BasicValue EvaluateTileAtFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 2)
        {
            throw new InvalidOperationException(
                "TILEAT expects two arguments.");
        }

        var x = Evaluate(function.Arguments[0]);
        var y = Evaluate(function.Arguments[1]);

        if (!x.IsInteger || !y.IsInteger)
        {
            throw new InvalidOperationException(
                "TILEAT expects pixel coordinates.");
        }

        return new BasicValue(
            _machine.TileAt(x.Integer, y.Integer));
    }
}
