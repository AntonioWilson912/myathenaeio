using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAthenaeio.Scanner
{
    internal enum ScannerMode
    {
        Disabled,         // App minimized, scanner off
        FocusedFieldOnly, // App active
        BackgroundService // App minimized
    }
}
