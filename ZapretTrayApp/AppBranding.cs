using System.Reflection;

namespace ZapretTrayApp;

internal static class AppBranding
{
    public static Icon LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("ZapretTrayApp.app.ico")
            ?? throw new InvalidOperationException("В сборке не найден файл иконки приложения.");
        return new Icon(stream);
    }

    public static Icon LoadIcon(Size size)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("ZapretTrayApp.app.ico")
            ?? throw new InvalidOperationException("В сборке не найден файл иконки приложения.");
        return new Icon(stream, size);
    }
}
