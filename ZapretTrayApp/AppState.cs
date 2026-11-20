namespace ZapretTrayApp;

internal sealed class AppState
{
    public string? ZapretRoot { get; set; }
    public string? SelectedStrategy { get; set; }
    public bool StartWithWindows { get; set; }
    public bool StopOnExit { get; set; }
}
