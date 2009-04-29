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
using System.Diagnostics;
using Mono.Tasklets;

namespace Mono.MicroThreads
{
	public delegate void MicroThreadStart();

	public enum MicroThreadState
	{
		Unstarted,    // Thread has not been started
		Starting,     // Start() has been called, but scheduler has not yet actually started the thread
		Running,      // Running at the moment
		Scheduled,    // Scheduled to be ran
		Waiting,      // Waiting for WakeUp
		Sleeping,     // Sleeping for a defined time
		Stopped       // Thread has finished
	}

	public class MicroThread : IDisposable
	{
		internal MicroThreadState m_state = MicroThreadState.Unstarted;
		internal Continuation m_continuation;
		internal MicroThread m_next;	// next microthread in scheduler's scheduled-list or sleeping-list
		internal long m_wakeTime;		// wake up time in DateTime.Ticks
		MicroThreadStart m_startDelegate;
		internal Exception m_error;

#if MT_DEBUG
		bool m_debug = true;
		static int s_numberCounter = 1;
		int m_number = s_numberCounter++;
#endif

#if MT_TIMING
		internal long m_ticks;

		public long UsedTicks
		{
			get { return m_ticks; }
		}
#endif

		public MicroThread(MicroThreadStart start)
		{
			m_startDelegate = start;
		}
		
		public void Dispose()
		{
			if (m_continuation != null)
			{
				m_continuation.Dispose();
				m_continuation = null;
			}

			m_startDelegate = null;
		}
		
		public void Start()
		{
			if(m_state != MicroThreadState.Unstarted)
			{
				throw new Exception("Thread has already been started");
			}

			m_continuation = new Continuation();

			m_state = MicroThreadState.Starting;
			Scheduler.Add(this);
		}

		public MicroThreadState State
		{
			get { return m_state; }
		}

		public static MicroThread CurrentThread
		{
			get { return Scheduler.CurrentThread; }
		}

		public void Yield()
		{
			if (m_state != MicroThreadState.Running)
			{
				throw new Exception(String.Format("Illegal thread state in Yield(): {0}", m_state));
			}

#if EXTRA_CHECKS
			if (CurrentThread != this)
			{
				throw new Exception("Trying to yield a non-current thread");
			}
#endif
			Scheduler.Yield();
		}

		public void WakeUp()
		{
			if (m_state == MicroThreadState.Scheduled)
			{
				// thread has already been woken up
				return;
			}

			if (m_state != MicroThreadState.Waiting)
			{
				throw new Exception(String.Format("Illegal thread state in WakeUp(): {0}", m_state));
			}

			Scheduler.WakeUp(this);
		}

		public void Wait()
		{
			if (m_state != MicroThreadState.Running)
			{
				throw new Exception(String.Format("Illegal thread state in Wait(): {0}", m_state));
			}

#if EXTRA_CHECKS
			if (CurrentThread != this)
			{
				throw new Exception("Trying to yield a non-current thread");
			}
#endif

			Scheduler.Wait();
		}

		public void Sleep(int milliseconds)
		{
			Scheduler.Sleep(milliseconds);
		}

		public void Interrupt()
		{
			m_error = new Exception("Thread interrupted");
		}

		public override string ToString()
		{
#if MT_DEBUG			
			return "MT" + m_number;
#else
			return base.ToString();
#endif
		}


		internal void Run()
		{
			m_continuation.Mark(); // Should this be in the scheduler?
			m_startDelegate();
		}
	}
}
