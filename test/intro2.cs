using System;
using Mono.MicroThreads;

class Program
{
	static void Main()
	{
		MicroThread t1 = new MicroThread(Run1);
		MicroThread t2 = new MicroThread(Run2);
		t1.Start();
		t2.Start();
		Scheduler.Run();
	}

	static void Run1()
	{
		for(int y = 0; y < 4; y++)
		{
			Console.WriteLine("y = {0}", y);
			MicroThread.CurrentThread.Yield();
		}
	}

	static void Run2()
	{
		for(int x = 0; x < 6; x++)
		{
			Console.WriteLine("x = {0}", x);
			if(x % 2 == 0)
				MicroThread.CurrentThread.Yield();
		}
	}
}
