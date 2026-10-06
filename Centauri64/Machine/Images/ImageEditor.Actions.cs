using System;
using System.Collections.Generic;

using Centauri64.Machine;

namespace Centauri64.Machine.Images;

public sealed partial class ImageEditor
{
    private void BeginRename()
    {
        if (_image == null)
            return;

        var listing = _getBasicListing?.Invoke();
        if (NameReferenced(listing, _image.Name))
        {
            _dialog.ShowConfirm(
                "RENAME IMAGE",
                "THIS IMAGE MAY BE REFERENCED\nBY YOUR PROGRAM.\n\nRENAME ANYWAY?",
                () => ShowRenameDialog());
            return;
        }

        ShowRenameDialog();
    }

    private void ShowRenameDialog()
    {
        if (_image == null)
            return;

        var current = _image;
        _dialog.ShowTextEntry(
            "RENAME IMAGE",
            "NEW NAME:",
            current.Name,
            (name, _) =>
            {
                if (!TryNormalizeName(name, out var valid, out var error))
                {
                    Notice?.Invoke(error);
                    return;
                }

                if (!string.Equals(valid, current.Name, StringComparison.Ordinal) &&
                    _assets.Contains(valid))
                {
                    Notice?.Invoke("IMAGE NAME ALREADY EXISTS");
                    return;
                }

                current.Name = valid;
                _dirty = true;
                _statusTip = $"RENAMED {valid}";
            });
    }

    private void BeginDuplicate()
    {
        if (_image == null)
            return;

        var suggested = _assets.UniqueName(_image.Name);
        _dialog.ShowTextEntry(
            "DUPLICATE IMAGE",
            "NEW NAME:",
            suggested,
            (name, _) =>
            {
                if (_image == null)
                    return;

                if (!TryNormalizeName(name, out var valid, out var error))
                {
                    Notice?.Invoke(error);
                    return;
                }

                if (_assets.Contains(valid))
                {
                    Notice?.Invoke("IMAGE NAME ALREADY EXISTS");
                    return;
                }

                var copy = _image.Clone(valid);
                _assets.Add(copy);
                _selectedIndex = _assets.Count - 1;
                _image = copy;
                ClearHistory();
                _dirty = true;
                FitZoom();
                _statusTip = $"DUPLICATED {valid}";
            });
    }

    private void BeginDelete()
    {
        if (_image == null)
            return;

        var name = _image.Name;
        var listing = _getBasicListing?.Invoke();
        var warning = NameReferenced(listing, name)
            ? $"DELETE \"{name}\"?\n\nTHIS IMAGE MAY BE REFERENCED\nBY YOUR PROGRAM.\n\nTHIS CANNOT BE UNDONE\nAFTER LEAVING THE EDITOR."
            : $"DELETE \"{name}\"?\n\nTHIS CANNOT BE UNDONE\nAFTER LEAVING THE EDITOR.";

        _dialog.ShowConfirm(
            "DELETE IMAGE",
            warning,
            () =>
            {
                _assets.Remove(name);
                RemoveThumbnails(_image!);
                if (_assets.Count == 0)
                {
                    _image = null;
                    _selectedIndex = 0;
                }
                else
                {
                    _selectedIndex = Math.Clamp(_selectedIndex, 0, _assets.Count - 1);
                    _image = _assets.Images[_selectedIndex];
                }

                ClearHistory();
                _dirty = true;
                _statusTip = $"DELETED {name}";
            });
    }

    private void ConfirmClear()
    {
        if (_image == null)
            return;

        _dialog.ShowConfirm(
            "CLEAR FRAME",
            "CLEAR CURRENT FRAME TO TRANSPARENT?",
            () =>
            {
                PushUndo();
                _image.Clear();
                _dirty = true;
            });
    }

    private void NewFrame()
    {
        if (_image == null)
            return;

        try
        {
            _image.AddFrame();
            ClearHistory();
            _dirty = true;
            _statusTip = $"FRAME {_image.CurrentFrameIndex + 1}";
        }
        catch (Exception ex)
        {
            Notice?.Invoke(ex.Message.ToUpperInvariant());
        }
    }

    private void DuplicateFrame()
    {
        if (_image == null)
            return;

        try
        {
            _image.DuplicateFrame(_image.CurrentFrameIndex);
            ClearHistory();
            _dirty = true;
            _statusTip = $"DUPLICATED FRAME {_image.CurrentFrameIndex + 1}";
        }
        catch (Exception ex)
        {
            Notice?.Invoke(ex.Message.ToUpperInvariant());
        }
    }

    private void DeleteFrame()
    {
        if (_image == null)
            return;

        if (_image.FrameCount <= 1)
        {
            Notice?.Invoke("CANNOT DELETE LAST FRAME");
            return;
        }

        _dialog.ShowConfirm(
            "DELETE FRAME",
            $"DELETE FRAME {_image.CurrentFrameIndex + 1}?",
            () =>
            {
                try
                {
                    _image.DeleteFrame(_image.CurrentFrameIndex);
                    ClearHistory();
                    _dirty = true;
                    _statusTip = "FRAME DELETED";
                }
                catch (Exception ex)
                {
                    Notice?.Invoke(ex.Message.ToUpperInvariant());
                }
            });
    }

    private void PreviousFrame()
    {
        if (_image == null || _image.FrameCount <= 1)
            return;

        var index = _image.CurrentFrameIndex - 1;
        if (index < 0)
            index = _image.FrameCount - 1;
        SelectFrame(index);
    }

    private void NextFrame()
    {
        if (_image == null || _image.FrameCount <= 1)
            return;

        SelectFrame((_image.CurrentFrameIndex + 1) % _image.FrameCount);
    }

    private void SelectFrame(int index)
    {
        if (_image == null)
            return;

        EndStrokeIfNeeded();
        _image.SelectFrame(index);
        ClearHistory();
        _statusTip = $"FRAME {index + 1}/{_image.FrameCount}";
    }

    private static bool NameReferenced(string? listing, string name)
    {
        if (string.IsNullOrEmpty(listing))
            return false;

        return listing.IndexOf('"' + name + '"', StringComparison.OrdinalIgnoreCase) >= 0 ||
               listing.IndexOf("\"" + name.ToLowerInvariant() + "\"", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryNormalizeName(string name, out string valid, out string error)
    {
        valid = "";
        error = "";

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "INVALID NAME";
            return false;
        }

        valid = name.Trim().ToUpperInvariant();
        if (valid.Length is < 1 or > 16)
        {
            error = "NAME MUST BE 1-16 CHARACTERS";
            return false;
        }

        foreach (var ch in valid)
        {
            if (ch is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_')
                continue;

            error = "USE A-Z 0-9 OR _";
            return false;
        }

        if (char.IsDigit(valid[0]))
        {
            error = "NAME MUST START WITH A LETTER";
            return false;
        }

        return true;
    }

    private void PushUndo()
    {
        if (_image == null)
            return;

        _undo.Add(ClonePixels(_image));
        if (_undo.Count > MaxUndo)
            _undo.RemoveAt(0);

        _redo.Clear();
    }

    private void Undo()
    {
        if (_image == null || _undo.Count == 0)
        {
            _statusTip = "NOTHING TO UNDO";
            return;
        }

        _redo.Add(ClonePixels(_image));
        ApplyPixels(_image, _undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
        _image.MarkChanged();
        _dirty = true;
        _statusTip = "UNDO";
    }

    private void Redo()
    {
        if (_image == null || _redo.Count == 0)
        {
            _statusTip = "NOTHING TO REDO";
            return;
        }

        _undo.Add(ClonePixels(_image));
        ApplyPixels(_image, _redo[^1]);
        _redo.RemoveAt(_redo.Count - 1);
        _image.MarkChanged();
        _dirty = true;
        _statusTip = "REDO";
    }

    private void ClearHistory()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private void RemoveThumbnails(ImageAsset image)
    {
        var keys = new List<(ImageAsset Image, int Frame)>();
        foreach (var key in _thumbnails.Keys)
        {
            if (ReferenceEquals(key.Image, image))
                keys.Add(key);
        }

        foreach (var key in keys)
            _thumbnails.Remove(key);
    }

    private static int[,] ClonePixels(ImageAsset image)
    {
        var copy = new int[image.Height, image.Width];
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
                copy[y, x] = image.Pixels[y, x];
        }

        return copy;
    }

    private static void ApplyPixels(ImageAsset image, int[,] source)
    {
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
                image.Pixels[y, x] = source[y, x];
        }
    }

    private static void Plot(ImageAsset image, int x, int y, int colour)
    {
        image.SetPixel(x, y, colour);
    }

    private static void DrawLinePixels(
        ImageAsset image,
        int x0, int y0, int x1, int y1,
        int colour)
    {
        foreach (var (x, y) in TraceLine(x0, y0, x1, y1))
            image.SetPixel(x, y, colour);
    }

    private void CommitShape(ImageAsset image, int x0, int y0, int x1, int y1)
    {
        var colour = DrawColour();
        switch (_tool)
        {
            case DrawTool.Line:
                DrawLinePixels(image, x0, y0, x1, y1, colour);
                break;
            case DrawTool.Rectangle:
                if (UsesFilledShapes())
                    FillRectangle(image, x0, y0, x1, y1, colour);
                else
                {
                    DrawLinePixels(image, x0, y0, x1, y0, colour);
                    DrawLinePixels(image, x1, y0, x1, y1, colour);
                    DrawLinePixels(image, x1, y1, x0, y1, colour);
                    DrawLinePixels(image, x0, y1, x0, y0, colour);
                }
                break;
            case DrawTool.Circle:
                if (UsesFilledShapes())
                {
                    foreach (var (x, y) in TraceFilledEllipse(x0, y0, x1, y1))
                        image.SetPixel(x, y, colour);
                }
                else
                {
                    foreach (var (x, y) in TraceEllipse(x0, y0, x1, y1))
                        image.SetPixel(x, y, colour);
                }
                break;
        }
    }

    private static void FillRectangle(
        ImageAsset image,
        int x0, int y0, int x1, int y1,
        int colour)
    {
        var left = Math.Min(x0, x1);
        var right = Math.Max(x0, x1);
        var top = Math.Min(y0, y1);
        var bottom = Math.Max(y0, y1);

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
                image.SetPixel(x, y, colour);
        }
    }

    private static void FloodFill(ImageAsset image, int x, int y, int newColour)
    {
        var target = image.GetPixel(x, y);
        if (target == newColour)
            return;

        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((x, y));

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            if (cx < 0 || cy < 0 || cx >= image.Width || cy >= image.Height)
                continue;

            if (image.Pixels[cy, cx] != target)
                continue;

            image.Pixels[cy, cx] = newColour;
            queue.Enqueue((cx + 1, cy));
            queue.Enqueue((cx - 1, cy));
            queue.Enqueue((cx, cy + 1));
            queue.Enqueue((cx, cy - 1));
        }
    }

    private static IEnumerable<(int X, int Y)> TraceLine(int x0, int y0, int x1, int y1)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        var x = x0;
        var y = y0;

        while (true)
        {
            yield return (x, y);
            if (x == x1 && y == y1)
                yield break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    private static IEnumerable<(int X, int Y)> TraceEllipse(int x0, int y0, int x1, int y1)
    {
        var cx = (x0 + x1) / 2;
        var cy = (y0 + y1) / 2;
        var rx = Math.Abs(x1 - x0) / 2;
        var ry = Math.Abs(y1 - y0) / 2;
        if (rx == 0 && ry == 0)
        {
            yield return (cx, cy);
            yield break;
        }

        if (rx == 0)
        {
            for (var y = cy - ry; y <= cy + ry; y++)
                yield return (cx, y);
            yield break;
        }

        if (ry == 0)
        {
            for (var x = cx - rx; x <= cx + rx; x++)
                yield return (x, cy);
            yield break;
        }

        var seen = new HashSet<long>();
        for (var deg = 0; deg < 360; deg++)
        {
            var rad = deg * Math.PI / 180.0;
            var x = cx + (int)Math.Round(rx * Math.Cos(rad));
            var y = cy + (int)Math.Round(ry * Math.Sin(rad));
            var key = ((long)x << 32) ^ (uint)y;
            if (!seen.Add(key))
                continue;

            yield return (x, y);
        }
    }

    private static IEnumerable<(int X, int Y)> TraceFilledEllipse(int x0, int y0, int x1, int y1)
    {
        var cx = (x0 + x1) / 2.0;
        var cy = (y0 + y1) / 2.0;
        var rx = Math.Abs(x1 - x0) / 2.0;
        var ry = Math.Abs(y1 - y0) / 2.0;

        if (rx < 0.5 && ry < 0.5)
        {
            yield return ((int)Math.Round(cx), (int)Math.Round(cy));
            yield break;
        }

        if (rx < 0.5)
        {
            var x = (int)Math.Round(cx);
            var top = (int)Math.Floor(cy - ry);
            var bottom = (int)Math.Ceiling(cy + ry);
            for (var y = top; y <= bottom; y++)
                yield return (x, y);
            yield break;
        }

        if (ry < 0.5)
        {
            var y = (int)Math.Round(cy);
            var left = (int)Math.Floor(cx - rx);
            var right = (int)Math.Ceiling(cx + rx);
            for (var x = left; x <= right; x++)
                yield return (x, y);
            yield break;
        }

        var minX = (int)Math.Floor(cx - rx);
        var maxX = (int)Math.Ceiling(cx + rx);
        var minY = (int)Math.Floor(cy - ry);
        var maxY = (int)Math.Ceiling(cy + ry);
        var rx2 = rx * rx;
        var ry2 = ry * ry;

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if ((dx * dx) / rx2 + (dy * dy) / ry2 <= 1.0)
                    yield return (x, y);
            }
        }
    }
}
