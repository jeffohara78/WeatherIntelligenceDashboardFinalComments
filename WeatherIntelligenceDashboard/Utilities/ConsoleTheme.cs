namespace WeatherIntelligenceDashboard.Utilities;

public static class ConsoleTheme
{
    public static void Title(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine(new string('=', 76));
        Console.WriteLine(Center(text, 76));
        Console.WriteLine(new string('=', 76));
        Console.ResetColor();
    }

    public static void Section(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"--- {text} ---");
        Console.ResetColor();
    }

    public static void MenuOption(string number, string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{number,2}");
        Console.ResetColor();
        Console.WriteLine($". {text}");
    }

    public static void TableHeader(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Favorite(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($">> {text}");
        Console.ResetColor();
    }

    public static void Highlight(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Muted(string text)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Success(string text)
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.Write(" OK ");
        Console.ResetColor();
        Console.WriteLine($" {text}");
    }

    public static void Error(string text)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.Write(" ERROR ");
        Console.ResetColor();
        Console.WriteLine($" {text}");
    }

    public static void Pause()
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine();
        Console.Write("Press Enter to continue...");
        Console.ResetColor();
        Console.ReadLine();
    }

    private static string Center(string text, int width)
    {
        if (text.Length >= width)
        {
            return text;
        }

        int left = (width - text.Length) / 2;
        return new string(' ', left) + text;
    }
}
