namespace Centauri64.Machine;

public struct ScreenCell
{
    public char Character;
    public int Foreground;
    public int Background;

    public ScreenCell(
        char character,
        int foreground,
        int background)
    {
        Character = character;
        Foreground = foreground;
        Background = background;
    }
}