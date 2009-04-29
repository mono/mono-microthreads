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

using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.MicroThreads
{
	public class Channel<T>
	{
		Queue<MicroThread> m_receivers = new Queue<MicroThread>();
		Queue<MicroThread> m_senders = new Queue<MicroThread>();
		Queue<T> m_dataQueue = new Queue<T>();

		public Channel()
		{
		}

		public void Send(T data)
		{
			MicroThread t = MicroThread.CurrentThread;

			m_dataQueue.Enqueue(data);

			if (m_receivers.Count == 0)
			{
				m_senders.Enqueue(t);
				t.Wait();
			}
			else
			{
				MicroThread receiver = m_receivers.Dequeue();
				receiver.WakeUp();
				t.Yield();
			}
		}

		public T Receive()
		{
			MicroThread t = MicroThread.CurrentThread;

			if (m_senders.Count == 0)
			{
				m_receivers.Enqueue(t);
				t.Wait();
			}
			else
			{
				MicroThread sender = m_senders.Dequeue();
				sender.WakeUp();
			}

			return m_dataQueue.Dequeue();
		}
	}
}
