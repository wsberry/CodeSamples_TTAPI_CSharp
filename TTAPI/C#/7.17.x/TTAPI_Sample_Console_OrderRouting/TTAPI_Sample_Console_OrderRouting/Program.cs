using System;
using System.Collections.Generic;
using System.Linq;
using slx.tt;
using slx.tt.console;

namespace TTAPI_Sample_Console_OrderRouting
{
    class Program
    {
        static void Main()
        {
            var program = new TTProgram(new TTAPIFunctions(), CommandLineOptions.RunMultiThreaded());

            program.Run();
        }
    }
}
