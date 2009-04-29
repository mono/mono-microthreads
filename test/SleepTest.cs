/**
 * Starts n threads, each sleeps a constant amount of time.
 * Checks if the actual slept time was what was asked.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Mono.MicroThreads;

namespace SleepTest
{
	class Sleeper
	{
		static Random s_random = new Random();

		int m_sleepTime;

		public Sleeper()
		{
			m_sleepTime = s_random.Next(10000) + 10;
		}
		
		public void Run()
		{
			while(true) {
				Stopwatch sw = new Stopwatch();
				sw.Start();
				MicroThread.CurrentThread.Sleep(m_sleepTime);
				sw.Stop();
				long diff = Math.Abs(m_sleepTime - sw.ElapsedMilliseconds);
				if(diff > 10)
					Console.WriteLine("Tried to sleep {0} ms, actual {1} ms, diff {2}.", 
									  m_sleepTime, sw.ElapsedMilliseconds, diff);
			}

			//Console.WriteLine("Thread {0} has used {1} ticks", MicroThread.CurrentThread, MicroThread.CurrentThread.UsedTicks);
		}
	}

	class SleepTest
	{
		static int s_threads = 10;

		static int Main(string[] args)
		{

			if(args.Length != 1)
			{
				Console.WriteLine("usage: SleepTest <numthreads>");
				return 1;
			}

			s_threads = Int32.Parse(args[0]);

			for (int i = 0; i < s_threads; i++)
			{
				Sleeper s = new Sleeper();
				MicroThread t = new MicroThread(s.Run);
				t.Start();
			}

			Scheduler.Run();

			return 0;
		}
	}
}
