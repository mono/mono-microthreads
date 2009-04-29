/*
 * Starts n threads, each of which do m loops, yielding in each loop.
 * Prints yields per secs
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Mono.MicroThreads;

namespace YieldTest
{
	class YieldTest
	{
		static int s_threads = 10;
		static int s_loops = 10;
		//static long s_totalYields = 0;

		static int Main(string[] args)
		{
			if(args.Length != 2)
			{
				Console.WriteLine("usage: YieldTest <numthreads> <numloops>");
				return 1;
			}

			s_threads = Int32.Parse(args[0]);
			s_loops = Int32.Parse(args[1]);

			for (int i = 0; i < s_threads; i++)
			{
				MicroThread t = new MicroThread(Run);
				t.Start();
			}

			DateTime t1 = DateTime.Now;

			Scheduler.Run();

			DateTime t2 = DateTime.Now;
			TimeSpan ts = t2 - t1;

			//Console.WriteLine("Total yields {0}", s_totalYields);
			Console.WriteLine("{0} threads * {1} loops = {2} yields in {3:F2}s, {4:F0} yields/s", s_threads, s_loops, s_threads * s_loops, 
				ts.TotalSeconds, (s_threads * s_loops)/ts.TotalSeconds);
			/*
			long mem = System.GC.GetTotalMemory(false);
			Console.WriteLine("Mem {0:F2}M", mem / 1000000.0);
			*/
			return 0;
		}

		static void Run()
		{
			for (int i = 0; i < s_loops; i++)
			{
				//s_totalYields++;

				MicroThread.CurrentThread.Yield();
			}
		}
	}
}
