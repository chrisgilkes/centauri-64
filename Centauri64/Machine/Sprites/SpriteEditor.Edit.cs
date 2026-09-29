using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework.Input;

namespace Centauri64.Machine.Sprites;

public sealed partial class SpriteEditor
{
    private enum ConfirmKind
    {
        None,
        DeleteFrame,
        DeleteAnimation,
        DeleteSprite
    }

    private const int MaxUndo = 32;
    private const int PreviewScale = 4;

    public event Action<string>? Notice;

    private readonly List<int[,]> _undo = new();
    private bool _dirty;
    private bool _painting;
    private bool _showingHelp;
    private bool _copyingSprite;
    private bool _renamingSprite;
    private ConfirmKind _confirm;
    private string _message = "";

    public void MarkSaved()
    {
        _dirty = false;
    }

    private void MarkDirty()
    {
        _dirty = true;
    }

    private void ShowMessage(string text)
    {
        _message = text;
    }

    private void ClearUndo()
    {
        _undo.Clear();
    }

    private void PushUndo()
    {
        if (_frame == null)
            return;

        _undo.Add(ClonePixels(_frame.Pixels));

        if (_undo.Count > MaxUndo)
            _undo.RemoveAt(0);
    }

    private void Undo()
    {
        if (_frame == null || _undo.Count == 0)
        {
            ShowMessage("NOTHING TO UNDO");
            return;
        }

        var pixels = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        CopyPixels(pixels, _frame.Pixels);
        MarkDirty();
    }

    private void BeginPaintStroke()
    {
        if (_painting)
            return;

        PushUndo();
        _painting = true;
        MarkDirty();
    }

    private static int[,] ClonePixels(int[,] source)
    {
        var copy = new int[
            CentauriSprite.HEIGHT,
            CentauriSprite.WIDTH];

        CopyPixels(source, copy);

        return copy;
    }

    private static void CopyPixels(int[,] source, int[,] destination)
    {
        for (var y = 0; y < CentauriSprite.HEIGHT; y++)
        {
            for (var x = 0; x < CentauriSprite.WIDTH; x++)
            {
                destination[y, x] = source[y, x];
            }
        }
    }

    private void UpdateConfirm(
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        if (keyboard.IsKeyDown(Keys.Escape) &&
            previousKeyboard.IsKeyUp(Keys.Escape) ||
            keyboard.IsKeyDown(Keys.N) &&
            previousKeyboard.IsKeyUp(Keys.N))
        {
            _confirm = ConfirmKind.None;
            return;
        }

        if (!keyboard.IsKeyDown(Keys.Y) ||
            !previousKeyboard.IsKeyUp(Keys.Y))
        {
            return;
        }

        var kind = _confirm;
        _confirm = ConfirmKind.None;

        switch (kind)
        {
            case ConfirmKind.DeleteFrame:
                DeleteCurrentFrame();
                MarkDirty();
                ClearUndo();
                break;

            case ConfirmKind.DeleteAnimation:
                DeleteCurrentAnimation();
                MarkDirty();
                ClearUndo();
                break;

            case ConfirmKind.DeleteSprite:
                DeleteCurrentSprite();
                break;
        }
    }

    private void CopyCurrentSprite(string name)
    {
        if (_asset == null)
            return;

        var copy = _asset.Clone(name);
        _assets.Add(copy);
        MarkDirty();
        ClearUndo();
        SelectAssetByName(name);
    }

    private void RenameCurrentSprite(string name)
    {
        if (_asset == null)
            return;

        if (!_assets.Rename(_asset.Name, name))
        {
            ShowMessage("NAME TAKEN");
            return;
        }

        MarkDirty();
        SelectAssetByName(name);
    }

    private void DeleteCurrentSprite()
    {
        if (_asset == null)
            return;

        _assets.Remove(_asset.Name);
        MarkDirty();
        ClearUndo();

        var assets = _assets.Assets;

        if (assets.Count == 0)
        {
            _asset = null;
            _animation = null;
            _frame = null;
            _currentAssetIndex = 0;
            return;
        }

        var index = _currentAssetIndex;

        if (index >= assets.Count)
            index = assets.Count - 1;

        SelectAsset(index);
    }

    private void SelectAssetByName(string name)
    {
        var assets = _assets.Assets;

        for (var i = 0; i < assets.Count; i++)
        {
            if (assets[i].Name == name)
            {
                SelectAsset(i);
                return;
            }
        }
    }

    private bool PickColourAt(int spriteX, int spriteY)
    {
        if (_frame == null)
            return false;

        SelectedColour = _frame.Pixels[spriteY, spriteX];
        return true;
    }
}
