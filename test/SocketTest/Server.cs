using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Mono.MicroThreads;

namespace SocketTest
{
	public class Server
	{
		public static int s_bufferSize;

		public static long s_totalBytesReceived = 0;
		public static int s_connectedSockets = 0;

		public static void ListenRun()
		{
			MicroSocket listenSocket = new MicroSocket();

			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 12345);
			listenSocket.Bind(ep);
			listenSocket.Listen(10);

			while (true)
			{
				MicroSocket socket = listenSocket.Accept();

				//Console.WriteLine("Accepted a new socket");
				//Console.Write(".");

				s_connectedSockets++;

				Server server = new Server(socket);
				MicroThread t = new MicroThread(server.SocketRun);
				t.Start();
			}
		}

		MicroSocket m_socket;

		public Server(MicroSocket s)
		{
			m_socket = s;
		}

		public void SocketRun()
		{
			byte[] buf = new byte[s_bufferSize];
			while (true)
			{
				int len = m_socket.Receive(buf);

				if (len == 0)
				{
					break;
				}

				s_totalBytesReceived += len;
			}

			//Console.Write("x");

			s_connectedSockets--;

			m_socket.Shutdown();
			m_socket.Close();
		}
	}
}
