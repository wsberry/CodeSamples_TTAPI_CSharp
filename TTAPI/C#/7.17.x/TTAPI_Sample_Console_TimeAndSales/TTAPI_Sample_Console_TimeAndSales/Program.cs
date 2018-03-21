
using slx.tt;
using slx.tt.console;

namespace TTAPI_Sample_Console_TimeAndSales
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
