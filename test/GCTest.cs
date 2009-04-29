using System;
using System.Collections.Generic;
using System.Text;
using Mono.MicroThreads;

namespace GCTest
{
	class GCTest
	{
		static void Main()
		{
			MicroThread t = new MicroThread(MainRun);
			t.Start();

			Scheduler.Run();
		}

		static void MainRun()
		{
			int started = 0;

			while (true)
			{
				if (Scheduler.ThreadCount < 100)
				{
					MicroThread t = new MicroThread(Work);
					t.Start();
					started++;
				}

				Console.WriteLine("Threads {0}, started {1}", Scheduler.ThreadCount, started);

				Scheduler.Yield();
			}
		}

		static void Work()
		{
			for (int i = 0; i < 400; i++)
			{
				if (i == 300)
					throw new Exception("kala");
				Scheduler.Yield();
			}
		}
	}
}

