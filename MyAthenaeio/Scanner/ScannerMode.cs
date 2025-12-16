namespace MyAthenaeio.Scanner
{
    internal enum ScannerMode
    {
        Disabled,         // App minimized, scanner off
        FocusedFieldOnly, // App active
        BackgroundService // App minimized
    }
}
