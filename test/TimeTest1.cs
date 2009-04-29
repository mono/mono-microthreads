using System;
using System.Collections.Generic;
using Mono.MicroThreads;

namespace Test
{
	class TestClass
	{
		static Channel<int> s_channel;

		static int res = 0;

		static int s_loops = 1000000;

		static void Main()
		{
			DateTime time1 = DateTime.Now;
			
			s_channel = new Channel<int>();

			MicroThread t2 = new MicroThread(run2);
			t2.Start();

			MicroThread t3 = new MicroThread(run2);
			t3.Start();

			MicroThread t1 = new MicroThread(run1);
			t1.Start();

			Console.WriteLine("Starting producer/consumer test, loops {0}", s_loops);

			Scheduler.Run();

			DateTime time2 = DateTime.Now;

			Console.WriteLine("total {0}", res);
			Console.WriteLine("time {0}ms", (time2 - time1).TotalMilliseconds);
			Console.WriteLine("END");
		}

		static void run1()
		{
			for(int i = 0; i < s_loops; i++)
			{
				s_channel.Send(i);
			}

			Scheduler.Exit();
		}

		static void run2()
		{
			while(true)
			{
				int val = s_channel.Receive();
				res ++;
			}
		}
	}
}

