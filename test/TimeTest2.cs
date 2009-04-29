
using System;
using System.Collections.Generic;
using Mono.MicroThreads;

namespace Test
{
	class TestClass
	{
		static int res = 0;
		static int s_loops = 1000000;

		static void Main()
		{
			DateTime time1 = DateTime.Now;

			MicroThread t = new MicroThread(loop);
			t.Start();

			Console.WriteLine("Starting yield test, loops {0}", s_loops);

			Scheduler.Run();

			DateTime time2 = DateTime.Now;

			Console.WriteLine("END {0}", res);

			TimeSpan ts = time2 - time1;

			Console.WriteLine("time {0}ms", ts.TotalMilliseconds);

		}

		static void loop()
		{
			while(res < s_loops)
			{
				res++;
				MicroThread.CurrentThread.Yield();
			}
		}

	}
}

