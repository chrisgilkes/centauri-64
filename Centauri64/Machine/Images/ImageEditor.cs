using System;
using System.Collections.Generic;

using Centauri64.CreativeTools;
using Centauri64.Graphics;
using Centauri64.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Machine.Images;

public sealed partial class ImageEditor
{
    public enum DrawTool
    {
        Pencil,
        Eraser,
        Fill,
        Line,
        Rectangle,
        Circle,
        Pick
    }

    private readonly ImageAssetStore _assets;
    private readonly MenuBar _menuBar = new(16, 64);
    private readonly ToolBar _toolBar;
    private readonly StatusBar _statusBar;
    private readonly SimpleDialog _dialog = new();
    private readonly ImageTextureCache _canvasCache = new();
    private readonly Dictionary<(ImageAsset Image, int Frame), Thumbnail> _thumbnails = new();

    private ImageAsset? _image;
    private int _selectedIndex;
    private DrawTool _tool = DrawTool.Pencil;
    private int _colour = 7;
    private bool _showGrid = true;
    private float _zoom = 1f;
    private bool _fitZoom = true;
    private float _panX;
    private float _panY;
    private int _workspaceX;
    private int _workspaceY;
    private int _workspaceW;
    private int _workspaceH;
    private bool _panning;
    private int _panGrabX;
    private int _panGrabY;
    private float _panGrabPanX;
    private float _panGrabPanY;
    private int _previousScroll;

    private static readonly float[] ZoomSteps =
    {
        1f, 2f, 3f, 4f, 6f, 8f, 12f, 16f, 24f, 32f
    };
    private bool _dirty;
    private bool _strokeActive;
    private int _lastPixelX = -1;
    private int _lastPixelY = -1;
    private int _shapeStartX;
    private int _shapeStartY;
    private bool _shapeDragging;
    private bool _shapeFilled;
    private int _previewX = -1;
    private int _previewY = -1;
    private MouseState _previousMouse;
    private KeyboardState _keyboard;
    private string _statusTip = "";
    private Func<string?>? _getBasicListing;
    private CentauriSettings? _settings;
    private Action? _persistSettings;

    private readonly List<int[,]> _undo = new();
    private readonly List<int[,]> _redo = new();
    private const int MaxUndo = 32;

    private int _canvasX;
    private int _canvasY;
    private int _canvasW;
    private int _canvasH;

    private const int BrowserWidth = 120;
    private const int PaletteWidth = 88;
    private const int FrameStripHeight = 52;
    private const int ScreenW = CentauriMachine.DEVELOPMENT_WIDTH;
    private const int ScreenH = CentauriMachine.DEVELOPMENT_HEIGHT;

    private enum BrowserFilter
    {
        All,
        General,
        Sprite,
        Tileset,
        Background
    }

    private BrowserFilter _browserFilter = BrowserFilter.All;

    public bool IsActive { get; private set; }

    public event Action<string>? Notice;

    private sealed class Thumbnail
    {
        public Texture2D? Texture;
        public int Revision = int.MinValue;
    }

    public ImageEditor(ImageAssetStore assets)
    {
        _assets = assets;
        _toolBar = new ToolBar(16, 24, 40);
        _statusBar = new StatusBar(ScreenH - 16, 16);
        BuildMenus();
        BuildToolbar();
    }

    public void SetListingLookup(Func<string?> getBasicListing)
    {
        _getBasicListing = getBasicListing;
    }

    public void Open()
    {
        IsActive = true;
        _dirty = false;
        _strokeActive = false;
        _shapeDragging = false;
        _panning = false;
        _dialog.Close();
        _menuBar.Close();
        ClearHistory();
        _previousScroll = Mouse.GetState().ScrollWheelValue;

        if (_settings != null)
            _showGrid = _settings.Grid;

        if (_assets.Count == 0)
        {
            _image = null;
            _selectedIndex = 0;
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex, 0, _assets.Count - 1);
        _image = _assets.Images[_selectedIndex];
        FitZoom();
    }

    public void Close()
    {
        if (!IsActive)
            return;

        IsActive = false;
        _dialog.Close();
        _menuBar.Close();
        _strokeActive = false;
        _shapeDragging = false;

        if (_dirty)
            Notice?.Invoke("SAVE TAPE TO KEEP IMAGES");
    }

    public void MarkSaved() => _dirty = false;

    public void Update(
        GameTime gameTime,
        MouseState mouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        CentauriSettings settings,
        Action? persistSettings = null)
    {
        _settings = settings;
        _persistSettings = persistSettings;

        if (!IsActive)
            return;

        _keyboard = keyboard;

        if (_dialog.IsOpen)
        {
            _dialog.HandleKeyboard(keyboard, previousKeyboard);
            _dialog.HandleMouse(mouse, _previousMouse, ScreenW, ScreenH);
            _previousMouse = mouse;
            return;
        }

        if (WizardOpen)
        {
            HandleWizardKeyboard(keyboard, previousKeyboard);
            HandleWizardMouse(mouse, _previousMouse);
            _previousMouse = mouse;
            return;
        }

        if (_menuBar.IsOpen)
        {
            _menuBar.HandleKeyboard(keyboard, previousKeyboard);
            _menuBar.HandleMouse(mouse, _previousMouse, ScreenW);
            _previousMouse = mouse;
            return;
        }

        if (_menuBar.HandleMouse(mouse, _previousMouse, ScreenW))
        {
            _previousMouse = mouse;
            return;
        }

        if (WasPressed(keyboard, previousKeyboard, Keys.Escape))
        {
            Close();
            _previousMouse = mouse;
            return;
        }

        HandleShortcuts(keyboard, previousKeyboard);
        HandleZoomAndPan(mouse, keyboard);

        if (_toolBar.HandleMouse(mouse, _previousMouse))
        {
            if (!string.IsNullOrEmpty(_toolBar.HoverTip))
                _statusTip = _toolBar.HoverTip;
            _previousMouse = mouse;
            EndStrokeIfNeeded();
            return;
        }

        if (HandleBrowserMouse(mouse))
        {
            _previousMouse = mouse;
            EndStrokeIfNeeded();
            return;
        }

        if (HandleFrameStripMouse(mouse))
        {
            _previousMouse = mouse;
            EndStrokeIfNeeded();
            return;
        }

        if (HandlePaletteMouse(mouse))
        {
            _previousMouse = mouse;
            EndStrokeIfNeeded();
            return;
        }

        HandleCanvasMouse(mouse);
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, ScreenW, ScreenH),
            CreativeUiTheme.CanvasChrome);

        DrawBrowser(spriteBatch, font, pixel);
        DrawCanvas(spriteBatch, font, pixel);
        DrawPalette(spriteBatch, font, pixel);
        DrawFrameStrip(spriteBatch, font, pixel);

        _toolBar.Draw(spriteBatch, font, pixel, ScreenW);
        _menuBar.Draw(spriteBatch, font, pixel, ScreenW);

        var status = BuildStatusText();
        var tip = ShowTips() ? _statusTip : "";
        _statusBar.Draw(spriteBatch, font, pixel, ScreenW, status, tip);

        _dialog.Draw(spriteBatch, font, pixel, ScreenW, ScreenH);
        DrawWizard(spriteBatch, font, pixel);
    }

    private void BuildMenus()
    {
        _menuBar.SetMenus(new[]
        {
            new MenuDefinition("FILE",
                new MenuItem("EXIT", Close, "ESC")),
            new MenuDefinition("EDIT",
                new MenuItem("UNDO", Undo, "CTRL+Z", () => _undo.Count > 0),
                new MenuItem("REDO", Redo, "CTRL+Y", () => _redo.Count > 0),
                new MenuItem("CLEAR IMAGE", ConfirmClear, "", () => _image != null)),
            new MenuDefinition("IMAGE",
                new MenuItem("NEW IMAGE", BeginNewImage),
                new MenuItem("PROPERTIES", BeginProperties, "", () => _image != null),
                new MenuItem("RENAME IMAGE", BeginRename, "", () => _image != null),
                new MenuItem("DUPLICATE IMAGE", BeginDuplicate, "", () => _image != null),
                new MenuItem("DELETE IMAGE", BeginDelete, "", () => _image != null),
                new MenuItem("NEW FRAME", NewFrame, "", () => _image != null),
                new MenuItem("DUPLICATE FRAME", DuplicateFrame, "", () => _image != null),
                new MenuItem("DELETE FRAME", DeleteFrame, "", () => _image != null),
                new MenuItem("PREV FRAME", PreviousFrame, "[", () => _image != null),
                new MenuItem("NEXT FRAME", NextFrame, "]", () => _image != null)),
            new MenuDefinition("DRAW",
                new MenuItem("PENCIL", () => _tool = DrawTool.Pencil, "P"),
                new MenuItem("ERASER", () => _tool = DrawTool.Eraser, "E"),
                new MenuItem("FILL", () => _tool = DrawTool.Fill, "F"),
                new MenuItem("LINE", () => _tool = DrawTool.Line, "L"),
                new MenuItem("RECTANGLE", () => SelectShape(DrawTool.Rectangle, filled: false), "R"),
                new MenuItem("FILLED RECT", () => SelectShape(DrawTool.Rectangle, filled: true)),
                new MenuItem("CIRCLE", () => SelectShape(DrawTool.Circle, filled: false), "C"),
                new MenuItem("FILLED CIRCLE", () => SelectShape(DrawTool.Circle, filled: true)),
                new MenuItem("PICK COLOUR", () => _tool = DrawTool.Pick, "I"),
                new MenuItem("OUTLINE SHAPES", () => SetShapeFilled(false), "", () => _shapeFilled),
                new MenuItem("FILLED SHAPES", () => SetShapeFilled(true), "S", () => !_shapeFilled)),
            new MenuDefinition("VIEW",
                new MenuItem("GRID", ToggleGrid, "G"),
                new MenuItem("ZOOM IN", ZoomIn, "+ / WHEEL"),
                new MenuItem("ZOOM OUT", ZoomOut, "- / WHEEL"),
                new MenuItem("ZOOM FIT", FitZoom, "0"),
                new MenuItem("ZOOM 1X", () => SetZoom(1f)),
                new MenuItem("ZOOM 2X", () => SetZoom(2f)),
                new MenuItem("ZOOM 4X", () => SetZoom(4f)),
                new MenuItem("ZOOM 8X", () => SetZoom(8f)),
                new MenuItem("ZOOM 16X", () => SetZoom(16f)),
                new MenuItem("ZOOM 32X", () => SetZoom(32f))),
            new MenuDefinition("HELP",
                new MenuItem("CONTROLS", ShowHelp))
        });
    }

    private void BuildToolbar()
    {
        _toolBar.SetButtons(new[]
        {
            new ToolButton("pencil", "PEN", "PENCIL — DRAW USING THE SELECTED COLOUR", () => _tool = DrawTool.Pencil, () => _tool == DrawTool.Pencil),
            new ToolButton("erase", "ERASE", "ERASER — ERASE TO TRANSPARENT", () => _tool = DrawTool.Eraser, () => _tool == DrawTool.Eraser),
            new ToolButton("fill", "FILL", "FILL — FILL A CONNECTED AREA", () => _tool = DrawTool.Fill, () => _tool == DrawTool.Fill),
            new ToolButton("line", "LINE", "LINE — DRAW A LINE", () => _tool = DrawTool.Line, () => _tool == DrawTool.Line),
            new ToolButton("rect", "RECT", "RECTANGLE — OUTLINE OR FILLED (SEE SOLID)", () => _tool = DrawTool.Rectangle, () => _tool == DrawTool.Rectangle),
            new ToolButton("circ", "CIRC", "CIRCLE — OUTLINE OR FILLED (SEE SOLID)", () => _tool = DrawTool.Circle, () => _tool == DrawTool.Circle),
            new ToolButton("solid", "SOLID", "SOLID — TOGGLE FILLED SHAPES FOR RECT/CIRCLE", ToggleShapeFilled, () => _shapeFilled),
            new ToolButton("pick", "PICK", "PICK — SELECT A COLOUR FROM THE IMAGE", () => _tool = DrawTool.Pick, () => _tool == DrawTool.Pick),
            new ToolButton("zoomin", "Z+", "ZOOM IN — MORE DETAIL (MOUSE WHEEL)", ZoomIn),
            new ToolButton("zoomout", "Z-", "ZOOM OUT — SEE MORE OF THE IMAGE", ZoomOut),
            new ToolButton("frdup", "F+", "DUPLICATE FRAME", DuplicateFrame),
            new ToolButton("frnew", "NEWF", "NEW FRAME", NewFrame),
            new ToolButton("undo", "UNDO", "UNDO — UNDO LAST ACTION", Undo),
            new ToolButton("redo", "REDO", "REDO — REDO LAST UNDO", Redo),
            new ToolButton("grid", "GRID", "GRID — TOGGLE PIXEL GRID", ToggleGrid, () => _showGrid)
        });
    }

    private void SelectShape(DrawTool tool, bool filled)
    {
        _tool = tool;
        SetShapeFilled(filled);
    }

    private void ToggleShapeFilled() => SetShapeFilled(!_shapeFilled);

    private void SetShapeFilled(bool filled)
    {
        _shapeFilled = filled;
        _statusTip = filled
            ? "FILLED SHAPES — RECT AND CIRCLE FILL"
            : "OUTLINE SHAPES — RECT AND CIRCLE OUTLINES";
    }

    private bool ShowTips()
    {
        if (_settings == null)
            return true;

        if (!_settings.Tooltips)
            return false;

        return _settings.EditorExperience != EditorExperience.Classic;
    }

    private void SetZoom(float zoom)
    {
        if (_image == null)
        {
            _fitZoom = false;
            _zoom = zoom;
            return;
        }

        EnsureWorkspace();
        ZoomTo(zoom, _workspaceX + _workspaceW / 2, _workspaceY + _workspaceH / 2);
    }

    private void FitZoom()
    {
        _fitZoom = true;
        _panning = false;
        _statusTip = "ZOOM FIT";
        LayoutCanvas();
    }

    private void ZoomIn()
    {
        if (_image == null)
            return;

        EnsureWorkspace();
        var next = NextZoomStep(_zoom, zoomIn: true);
        ZoomTo(next, _workspaceX + _workspaceW / 2, _workspaceY + _workspaceH / 2);
        _statusTip = $"ZOOM {(int)_zoom}X";
    }

    private void ZoomOut()
    {
        if (_image == null)
            return;

        EnsureWorkspace();
        var next = NextZoomStep(_zoom, zoomIn: false);
        ZoomTo(next, _workspaceX + _workspaceW / 2, _workspaceY + _workspaceH / 2);
        _statusTip = _fitZoom ? "ZOOM FIT" : $"ZOOM {(int)_zoom}X";
    }

    private static float NextZoomStep(float current, bool zoomIn)
    {
        if (zoomIn)
        {
            foreach (var step in ZoomSteps)
            {
                if (step > current + 0.01f)
                    return step;
            }

            return ZoomSteps[^1];
        }

        for (var i = ZoomSteps.Length - 1; i >= 0; i--)
        {
            if (ZoomSteps[i] < current - 0.01f)
                return ZoomSteps[i];
        }

        return ZoomSteps[0];
    }

    private void ZoomTo(float newZoom, int focusScreenX, int focusScreenY)
    {
        if (_image == null)
            return;

        EnsureWorkspace();
        LayoutCanvas();

        newZoom = Math.Clamp(newZoom, ZoomSteps[0], ZoomSteps[^1]);

        // Keep the image pixel under the focus point stable.
        var focusPixelX = (focusScreenX - _canvasX) / _zoom;
        var focusPixelY = (focusScreenY - _canvasY) / _zoom;

        _fitZoom = false;
        _zoom = newZoom;
        _panX = focusScreenX - _workspaceX - focusPixelX * _zoom;
        _panY = focusScreenY - _workspaceY - focusPixelY * _zoom;
        ClampPan();
    }

    private void EnsureWorkspace()
    {
        _workspaceX = BrowserWidth;
        _workspaceY = _toolBar.Bottom;
        _workspaceW = ScreenW - BrowserWidth - PaletteWidth;
        _workspaceH = ScreenH - 16 - FrameStripHeight - _workspaceY;
    }

    private void ClampPan()
    {
        if (_image == null)
            return;

        var drawW = _image.Width * _zoom;
        var drawH = _image.Height * _zoom;

        if (drawW <= _workspaceW)
            _panX = (_workspaceW - drawW) / 2f;
        else
            _panX = Math.Clamp(_panX, _workspaceW - drawW, 0f);

        if (drawH <= _workspaceH)
            _panY = (_workspaceH - drawH) / 2f;
        else
            _panY = Math.Clamp(_panY, _workspaceH - drawH, 0f);
    }

    private void ToggleGrid()
    {
        _showGrid = !_showGrid;
        if (_settings != null)
        {
            _settings.Grid = _showGrid;
            _persistSettings?.Invoke();
        }

        _statusTip = _showGrid ? "GRID ON" : "GRID OFF";
    }

    private void ShowHelp()
    {
        _dialog.ShowHelp(
            "IMAGE EDITOR",
            "DRAW ARTWORK HERE — SPRITES/TILES/BG\n" +
            "MOUSE DRAW  WHEEL ZOOM  ALT-DRAG PAN\n" +
            "FRAME STRIP: SELECT / + ADD FRAMES\n" +
            "FILTERS: ALL GENERAL SPRITE TILE BG\n" +
            "SOLID: FILLED RECT/CIRCLE\n" +
            "CTRL+Z UNDO  0 FIT  ESC EXIT");
    }

    private string BuildStatusText()
    {
        if (_image == null)
            return "IMAGE EDITOR  —  NEW IMAGE TO BEGIN";

        var colourLabel = _colour < 0 ? "TRANSPARENT" : _colour.ToString();
        var mode = _image.Mode == CentauriDisplayMode.Arcade ? "ARCADE" : "STANDARD";
        var zoom = _fitZoom ? "FIT" : $"{FormatZoom(_zoom)}X";
        var shape = UsesFilledShapes() ? "FILLED" : "OUTLINE";
        var cat = _image.Category.ToString().ToUpperInvariant();
        return $"IMAGE:{_image.Name}  {cat}  {mode}  {_image.Width}X{_image.Height}  FR:{_image.CurrentFrameIndex + 1}/{_image.FrameCount}  TOOL:{_tool.ToString().ToUpperInvariant()}  {shape}  C:{colourLabel}  Z:{zoom}";
    }

    private static string FormatZoom(float zoom)
    {
        if (Math.Abs(zoom - MathF.Round(zoom)) < 0.01f)
            return ((int)MathF.Round(zoom)).ToString();

        return zoom.ToString("0.#");
    }

    private bool UsesFilledShapes()
    {
        if (_tool is not (DrawTool.Rectangle or DrawTool.Circle))
            return false;

        var shift =
            _keyboard.IsKeyDown(Keys.LeftShift) ||
            _keyboard.IsKeyDown(Keys.RightShift);

        return _shapeFilled || shift;
    }

    private static bool WasPressed(KeyboardState keyboard, KeyboardState previous, Keys key)
    {
        return keyboard.IsKeyDown(key) && previous.IsKeyUp(key);
    }

    private void EndStrokeIfNeeded()
    {
        if (_strokeActive)
        {
            _strokeActive = false;
            _lastPixelX = -1;
            _lastPixelY = -1;
            _image?.MarkChanged();
        }
    }
}
