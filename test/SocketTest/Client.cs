using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Mono.MicroThreads;

namespace SocketTest
{
	class Client
	{
		MicroSocket m_socket;
		long m_blocksToSend;
		int m_blockSize;
		public static long s_totalBlocksSent = 0;

		public static int s_connectingSockets = 0;
		public static int s_connectedSockets = 0;

		public Client(long blocksToSend, int blockSize)
		{
			m_blocksToSend = blocksToSend;
			m_blockSize = blockSize;
		}

		public void Run()
		{
			m_socket = new MicroSocket();

			//Console.Write("-");

			s_connectingSockets++;

			IPAddress baal = IPAddress.Parse("127.0.0.1");
			IPAddress torturer = IPAddress.Parse("192.168.1.1");

			if (m_socket.Connect(baal, 12345) == false)
			{
				Console.WriteLine("Connection failed");
				return;
			}

			s_connectingSockets--;
			s_connectedSockets++;

			/*
			while (s_connectedSockets < 500)
				MicroThread.CurrentThread.Sleep(1000);
			*/

			//Console.Write(".");

			long sentBlocks = 0;

			byte[] buf = new byte[m_blockSize];
			while (true)
			{
				m_socket.Send(buf, buf.Length);

				sentBlocks++;
				s_totalBlocksSent++;

				if (sentBlocks >= m_blocksToSend)
					break;
			}

			//Console.Write("x");
			s_connectedSockets--;

			m_socket.Shutdown();
			m_socket.Close();
		}
	}
}
