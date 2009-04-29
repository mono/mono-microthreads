
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Mono.MicroThreads;


namespace SocketTest
{
	class Program
	{
		static int s_threads = 2;
		static long s_blocks = 100 * 100;
		static int s_blockSize = 1<<14;

		static void Main(string[] args)
		{
			if (args.Length == 0 || args[0] == "server")
			{
				MicroThread pt = new MicroThread(ServerPrintRun);
				pt.Start();

				Server.s_bufferSize = s_blockSize;
				MicroThread t = new MicroThread(Server.ListenRun);
				t.Start();
				Scheduler.Run();
			}
			else
			{
				Console.WriteLine("Starting {0} threads, sending {1} {2} byte blocks", s_threads, s_blocks, s_blockSize);

				MicroThread pt = new MicroThread(ClientPrintRun);
				pt.Start();

				for (int i = 0; i < s_threads; i++)
				{
					Client c = new Client(s_blocks, s_blockSize);
					MicroThread t = new MicroThread(c.Run);
					t.Start();
				}

				DateTime t1 = DateTime.Now;
				Scheduler.Run();
				DateTime t2 = DateTime.Now;

				Console.WriteLine();
				Console.WriteLine("TOTAL {0} MB/s", (Client.s_totalBlocksSent / 1000.0 / 1000.0 * s_blockSize) / ((TimeSpan)(t2 - t1)).TotalSeconds);
			}
		}

		static void ServerPrintRun()
		{
			while (true)
			{
				DateTime t1 = DateTime.Now;
				Server.s_totalBytesReceived = 0;

				MicroThread.CurrentThread.Sleep(1000);

				DateTime t2 = DateTime.Now;

				TimeSpan ts = t2 - t1;

				Console.WriteLine("{1}\t{2}/{3}/{4}\t{0}MB/s", Server.s_totalBytesReceived / 1000.0 / 1000.0 / ts.TotalSeconds,
					Server.s_connectedSockets,
					Scheduler.RunningThreadCount, Scheduler.WaitingThreadCount, Scheduler.SleepingThreadCount);
			}
		}

		static void ClientPrintRun()
		{
			while (true)
			{
				DateTime t1 = DateTime.Now;
				long lastTotalBlocks = Client.s_totalBlocksSent;

				MicroThread.CurrentThread.Sleep(1000);

				long blockDiff = Client.s_totalBlocksSent - lastTotalBlocks;
				DateTime t2 = DateTime.Now;

				TimeSpan ts = t2 - t1;

				Console.WriteLine("{1}/{2}\t{3}/{4}/{5}\t{0}MB/s", blockDiff * s_blockSize / 1000.0 / 1000.0 / ts.TotalSeconds,
					Client.s_connectingSockets, Client.s_connectedSockets,
					Scheduler.RunningThreadCount, Scheduler.WaitingThreadCount, Scheduler.SleepingThreadCount);

				if (Scheduler.ThreadCount == 1)
					break;
			}
		}
	}
}

