/*
 * Copyright (c) 2009 Tomi Valkeinen
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */
//#define MT_SOCKET_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Mono.MicroThreads
{
	internal static class MicroSocketManager
	{
		static List<MicroSocket> s_socketList = new List<MicroSocket>();
		static Dictionary<Socket, MicroSocket> s_socketMap = new Dictionary<Socket, MicroSocket>();

		internal static void AddSocket(MicroSocket s)
		{
			s_socketList.Add(s);
			s_socketMap[s.Socket] = s;
		}

		internal static void RemoveSocket(MicroSocket s)
		{
			s_socketList.Remove(s);
			s_socketMap.Remove(s.Socket);
		}

		internal static void Tick(int maxWaitMs)
		{
			if (s_socketList.Count == 0)
			{
				if (maxWaitMs > 0)
				{
					//Console.WriteLine("sleep {0}", maxWaitMs);
					System.Threading.Thread.Sleep(maxWaitMs);
				}
				return;
			}

			bool doSelect = false;

			List<Socket> readList = new List<Socket>(s_socketList.Count);
			List<Socket> writeList = new List<Socket>(s_socketList.Count);
			List<Socket> errorList = new List<Socket>(s_socketList.Count);

			foreach (MicroSocket s in s_socketList)
			{
				//Console.WriteLine("State {0}", s.State);

				if (s.m_readingThread != null)
				{
					readList.Add(s.Socket);
					doSelect = true;
				}

				if (s.m_writingThread != null)
				{
					writeList.Add(s.Socket);
					doSelect = true;
				}

				if (s.m_readingThread != null || s.m_writingThread != null)
				{
					errorList.Add(s.Socket);
				}

				s.m_selectStatus = 0;
			}

			if (doSelect == false)
			{
				if(maxWaitMs > 0)
				{
					Console.WriteLine("sleep");
					System.Threading.Thread.Sleep(maxWaitMs);
				}
				return;
			}
			/*
			if(maxWaitMs > 0)
				Console.WriteLine("select timeout {0}", maxWaitMs);
			*/
			Socket.Select(readList, writeList, errorList, maxWaitMs * 1000);

			foreach (Socket s in readList)
			{
				//Console.WriteLine("Read event");

				MicroSocket ms = s_socketMap[s];

				ms.m_selectStatus |= MicroSocketSelectStatus.Read;

				if (ms.m_readingThread != null)
				{
					ms.m_readingThread.WakeUp();
				}
			}

			foreach (Socket s in writeList)
			{
				//Console.WriteLine("Write event");

				MicroSocket ms = s_socketMap[s];

				ms.m_selectStatus |= MicroSocketSelectStatus.Write;
			
				if (ms.m_writingThread != null)
				{
					ms.m_writingThread.WakeUp();
				}
			}

			foreach (Socket s in errorList)
			{
				Console.WriteLine("ERROR event");

				MicroSocket ms = s_socketMap[s];
				ms.m_selectStatus |= MicroSocketSelectStatus.Error;

				if (ms.m_readingThread != null)
				{
					ms.m_readingThread.WakeUp();
				}

				if (ms.m_writingThread != null)
				{
					ms.m_writingThread.WakeUp();
				}
			}

		}
	}

	[Flags]
	internal enum MicroSocketSelectStatus
	{
		Read = 1,
		Write = 2,
		Error = 4
	}

	public class MicroSocket
	{
		Socket m_socket;
		internal MicroThread m_readingThread;
		internal MicroThread m_writingThread;

		internal MicroSocketSelectStatus m_selectStatus = 0;

		CriticalSection m_readCS = new CriticalSection();
		CriticalSection m_writeCS = new CriticalSection();

		public MicroSocket()
		{
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_socket.Blocking = false;
			MicroSocketManager.AddSocket(this);
		}

		public MicroSocket(Socket socket)
		{
			m_socket = socket;
			m_socket.Blocking = false;
			MicroSocketManager.AddSocket(this);
		}

		public Socket Socket
		{
			get { return m_socket; }
		}

		public void Bind(EndPoint localEP)
		{
			m_socket.Bind(localEP);
		}

		public void Listen(int backlog)
		{
			m_socket.Listen(backlog);
		}

		public MicroSocket Accept()
		{
			Print("Begin accepting");

			m_readCS.Enter();
			m_readingThread = MicroThread.CurrentThread;
			m_readingThread.Wait();
			m_readingThread = null;
			m_readCS.Exit();

			Print("Accept returned");

			Socket newSocket = m_socket.Accept();
			MicroSocket s = new MicroSocket(newSocket);
			return s;
		}

		public bool Connect(IPAddress address, int port)
		{
			Print("Begin connecting to {0}:{1}", address, port);

			try
			{
				m_socket.Connect(new IPEndPoint(address, port));
			}
			catch (SocketException)
			{
				// ignore blocking connect exception. shouldn't there be some other way to do this...
				//Console.WriteLine("exc cont");
			}

			m_writeCS.Enter();
			m_writingThread = MicroThread.CurrentThread;
			m_writingThread.Wait();
			m_writingThread = null;
			m_writeCS.Exit();

			//Console.WriteLine("STATE {0}", m_waitState);

			if ((m_selectStatus & MicroSocketSelectStatus.Error) != 0)
			{
				Console.WriteLine("Connect failed");
				return false;
			}
			else if ((m_selectStatus & MicroSocketSelectStatus.Write) != 0)
			{
				//Console.WriteLine("Connected!");
				return true;
			}
			else
			{
				throw new Exception("illegal state");
			}
		}

		public int Receive(byte[] buffer)
		{
			SocketError error;

			int received = Receive(buffer, 0, buffer.Length, SocketFlags.None, out error);

			if (error != SocketError.Success)
			{
				throw new SocketException((int)error);
			}

			return received;
		}

		public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError error)
		{
			while (true)
			{
				int received = m_socket.Receive(buffer, offset, size, socketFlags, out error);

				if (error == SocketError.Success)
				{
					return received;
				}

				if (error != SocketError.WouldBlock)
				{
					return received;
				}

				m_readCS.Enter();
				m_readingThread = MicroThread.CurrentThread;
				m_readingThread.Wait();
				m_readingThread = null;
				m_readCS.Exit();
			}
		}


		public int Send(byte[] buffer, int len)
		{
			SocketError error;

			int sent = Send(buffer, 0, len, SocketFlags.None, out error);

			if (error != SocketError.Success)
			{
				throw new SocketException((int)error);
			}

			return sent;
		}

		public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError error)
		{
			int sent = 0;

			while (true)
			{
				sent += m_socket.Send(buffer, offset + sent, size - sent, socketFlags, out error);

				if (error == SocketError.WouldBlock)
				{
					error = SocketError.Success;
				}

				if (error != SocketError.Success)
				{
					return sent;
				}

				if (sent < size)
				{
					m_writeCS.Enter();
					m_writingThread = MicroThread.CurrentThread;
					m_writingThread.Wait();
					m_writingThread = null;
					m_writeCS.Exit();
				}
				else
				{
					return sent;
				}
			}
		}

		public void Shutdown()
		{
			m_socket.Shutdown(SocketShutdown.Both);
		}

		public void Close()
		{
			m_socket.Close();
			MicroSocketManager.RemoveSocket(this);
		}

		[Conditional("MT_SOCKET_DEBUG")]
		static void Print(string msg, params object[] args)
		{
			Console.WriteLine("Socket: " + msg, args);
		}
	}
}
