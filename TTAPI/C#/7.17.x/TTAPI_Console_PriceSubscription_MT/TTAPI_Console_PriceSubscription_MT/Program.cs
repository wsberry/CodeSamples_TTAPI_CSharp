using slx.tt;
using slx.tt.console;

// ReSharper disable InconsistentNaming

namespace TTAPI_Console_PriceSubscription_MT
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

   

