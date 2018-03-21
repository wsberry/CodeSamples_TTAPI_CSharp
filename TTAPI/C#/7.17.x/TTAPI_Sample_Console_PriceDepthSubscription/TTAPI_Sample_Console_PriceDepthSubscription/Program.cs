// **********************************************************************************************************************
//
//	Copyright © 2005-2013 Trading Technologies International, Inc.
//	All Rights Reserved Worldwide
//
// 	* * * S T R I C T L Y   P R O P R I E T A R Y * * *
//
//  WARNING: This file and all related programs (including any computer programs, example programs, and all source code) 
//  are the exclusive property of Trading Technologies International, Inc. (“TT”), are protected by copyright law and 
//  international treaties, and are for use only by those with the express written permission from TT.  Unauthorized 
//  possession, reproduction, distribution, use or disclosure of this file and any related program (or document) derived 
//  from it is prohibited by State and Federal law, and by local law outside of the U.S. and may result in severe civil 
//  and criminal penalties.
//
// ************************************************************************************************************************

using slx.tt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using slx.tt.console;

namespace TTAPI_Sample_Console_PriceDepthSubscription
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
