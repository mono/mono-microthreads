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
	public class Scheduler
	{
		#region Public Static Interface
		static Scheduler s_scheduler = new Scheduler();

		public static int ThreadCount
		{
			get { return s_scheduler.m_scheduledThreadCount + s_scheduler.m_waitingThreadCount + s_scheduler.m_sleepingThreadCount; }
		}

		public static int RunningThreadCount
		{
			get { return s_scheduler.m_scheduledThreadCount; }
		}

		public static int WaitingThreadCount
		{
			get { return s_scheduler.m_waitingThreadCount; }
		}

		public static int SleepingThreadCount
		{
			get { return s_scheduler.m_sleepingThreadCount; }
		}

		public static void Run()
		{
			s_scheduler.RunInternal();
		}

		public static void Exit()
		{
			s_scheduler.m_exiting = true;
		}

		public static void Add(MicroThread thread)
		{
			s_scheduler.AddInternal(thread);
		}

		public static void Yield()
		{
			s_scheduler.YieldInternal();
		}

		public static void Wait()
		{
			s_scheduler.WaitInternal();
		}

		public static void WakeUp(MicroThread thread)
		{
			s_scheduler.WakeUpInternal(thread);
		}

		public static void Sleep(int milliseconds)
		{
			s_scheduler.SleepInternal(milliseconds);
		}

		#endregion

		internal static MicroThread CurrentThread
		{
			get { return s_scheduler.m_currentThread; }
		}


		// Scheduled threads are stored in a circular singly-linked list.
		// this points to the thread that will be ran next, is currently being ran, or was just finished
		MicroThread m_currentThread;
		// this points to the thread that was ran before the current one
		MicroThread m_previousThread;

		int m_scheduledThreadCount;
		int m_waitingThreadCount;

		MicroThread m_firstSleepingThread;
		int m_sleepingThreadCount;

		int m_threadsScheduledAfterManagers;

		Continuation m_continuation = new Continuation();

		bool m_exiting = false;
		bool m_running = false;

#if MT_DEBUG
		bool m_debug = true;
#endif

#if MT_TIMING
		Stopwatch m_stopWatch = new Stopwatch();
#endif

		void AddInternal(MicroThread thread)
		{
			if (m_currentThread == null)
			{
#if EXTRA_CHECKS
				if(m_previousThread != null || m_scheduledThreadCount != 0)
					throw new Exception();
#endif
				m_currentThread = thread;
				m_previousThread = thread;
				thread.m_next = thread;
			}
			else
			{
#if EXTRA_CHECKS
				if(m_previousThread == null || m_scheduledThreadCount == 0)
					throw new Exception();
#endif

				m_previousThread.m_next = thread;
				m_previousThread = thread;
				thread.m_next = m_currentThread;
			}

			m_scheduledThreadCount++;
		}

		void RemoveCurrentThread()
		{
			if (m_currentThread == m_previousThread)
			{
#if EXTRA_CHECKS
				if(m_scheduledThreadCount != 1)
					throw new Exception();					
#endif
				m_currentThread.m_next = null;
				m_currentThread = null;
				m_previousThread = null;
			}
			else
			{
				m_previousThread.m_next = m_currentThread.m_next;
				m_currentThread.m_next = null;
				m_currentThread = m_previousThread.m_next;
			}

			m_scheduledThreadCount--;
		}

		void RunInternal()
		{
			if (m_running == true)
			{
				throw new Exception("Scheduler already running");
			}

			if (m_scheduledThreadCount == 0)
			{
				return;
			}

			m_running = true;

			m_continuation.Mark();

			// status 1 = new thread to be started, m_currentThread has been set
			// status 2 = exiting
			int status = m_continuation.Store(1);

			if (status == 1)
			{
				// status 1 = new thread to be started, m_currentThread has been set

				if (m_currentThread.m_state != MicroThreadState.Starting)
				{
					throw new Exception(String.Format("illegal state {0}", m_currentThread.m_state));
				}

				try
				{
					Print("Starting new thread {0}", m_currentThread);
					m_currentThread.m_state = MicroThreadState.Running;

#if MT_TIMING
					m_stopWatch.Reset();
					m_stopWatch.Start();
#endif
					m_currentThread.Run();
#if MT_TIMING
					m_stopWatch.Stop();
					m_currentThread.m_ticks += m_stopWatch.ElapsedTicks;
#endif

					// When we are here the thread has finished

					Print("Thread {0} finished", m_currentThread);
				}
				catch (Exception e)
				{
					Console.WriteLine("Unhandled Exception in thread {0}", m_currentThread);
					Console.WriteLine("Thread terminated");
					Console.WriteLine(e.ToString());
				}

				m_currentThread.m_state = MicroThreadState.Stopped;
				m_currentThread.Dispose();

				RemoveCurrentThread();

				ScheduleNext();

				// Never reached
				throw new Exception();
			}
			else if (status == 2)
			{
				m_currentThread = null;
				m_previousThread = null;
				m_scheduledThreadCount = 0;
				m_waitingThreadCount = 0;

				Print("Scheduler exiting");
				return;
			}
			else
			{
				throw new Exception("Urrgh illegal restore status");
			}

			// never reached
			//throw new Exception();
		}

		void RunManagers()
		{
			Print("RunManagers()");

			m_threadsScheduledAfterManagers = 0;

			int nextWakeUp = ManageSleepers();
			if (nextWakeUp == -1)
				nextWakeUp = 500;

			if (m_scheduledThreadCount > 0)
				nextWakeUp = 0;

			Print("RunManagers(): nextWakeUp {0}", nextWakeUp);
			MicroSocketManager.Tick(nextWakeUp);
		}

		void ScheduleNext()
		{
			if (m_exiting == true)
			{
				Print("Exiting...");
				m_continuation.Restore(2);
			}

			if (m_currentThread != null && m_threadsScheduledAfterManagers > m_scheduledThreadCount)
			{
				Print("Enough threads have been run, calling RunManagers()");
				RunManagers();
			}

			while (m_currentThread == null)
			{
#if EXTRA_CHECKS
				if(m_scheduledThreadCount != 0 || m_previousThread != null)
					throw new Exception();
#endif
				Print("No threads running");

				if (m_waitingThreadCount == 0 && m_sleepingThreadCount == 0)
				{
					Print("No threads running, waiting or sleeping. Exiting...");
					m_continuation.Restore(2);
				}

				// TODO run managers when we have threads running
				RunManagers();
			}

			//Print("going to run {0}, state {1}", m_currentThread, m_currentThread.m_state);

			m_threadsScheduledAfterManagers++;

			if (m_currentThread.m_state == MicroThreadState.Starting)
			{
				m_continuation.Restore(1);
			}
			else if (m_currentThread.m_state == MicroThreadState.Scheduled)
			{
				Print("Resuming thread {0}", m_currentThread);
				m_currentThread.m_state = MicroThreadState.Running;
#if MT_TIMING
				m_stopWatch.Reset();
				m_stopWatch.Start();
#endif
				m_currentThread.m_continuation.Restore(1);

				// Execution never reaches this point
				throw new Exception();
			}
			else
			{
				throw new Exception(String.Format("Illegal thread state in scheduler: {0}", m_currentThread.m_state));
			}
		}

		// Yields the current thread and schedules next one
		void YieldInternal()
		{
			Print("Yield() on thread {0}", m_currentThread);

			if (m_currentThread.m_continuation.Store(0) == 0)
			{
#if MT_TIMING
				m_stopWatch.Stop();
				m_currentThread.m_ticks += m_stopWatch.ElapsedTicks;
#endif
				m_currentThread.m_state = MicroThreadState.Scheduled;

				m_previousThread = m_currentThread;
				m_currentThread = m_currentThread.m_next;

				ScheduleNext();
			}
			else
			{
				// We come here when the thread has resumed
				//Print("Yield() returned, resuming thread {0}", m_currentThread);

				if (m_currentThread.m_error != null)
					throw m_currentThread.m_error;
			}
		}

		void WaitInternal()
		{
			Print("Wait() on thread {0}", m_currentThread);

			if (m_currentThread.m_continuation.Store(0) == 0)
			{
#if MT_TIMING
				m_stopWatch.Stop();
				m_currentThread.m_ticks += m_stopWatch.ElapsedTicks;
#endif
				m_currentThread.m_state = MicroThreadState.Waiting;

				RemoveCurrentThread();

				m_waitingThreadCount++;

				ScheduleNext();
			}
			else
			{
				//Print("Wait() ended on thread {0}", m_currentThread);

				if (m_currentThread.m_error != null)
					throw m_currentThread.m_error;
			}
		}

		void WakeUpInternal(MicroThread thread)
		{
			Print("Waking up thread {0}", thread);

			m_waitingThreadCount--;

			thread.m_state = MicroThreadState.Scheduled;

			AddInternal(thread);
		}

		void SleepInternal(int milliseconds)
		{
			Print("Putting thread {0} to sleep for {1} ms", m_currentThread, milliseconds);

			if (m_currentThread.m_continuation.Store(0) == 0)
			{
#if MT_TIMING
				m_stopWatch.Stop();
				m_currentThread.m_ticks += m_stopWatch.ElapsedTicks;
#endif
				MicroThread thread = m_currentThread;

				RemoveCurrentThread();

				thread.m_state = MicroThreadState.Sleeping;


				DateTime wakeDateTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(milliseconds);
				long wakeTime = wakeDateTime.Ticks;
				thread.m_wakeTime = wakeTime;

				if (m_firstSleepingThread == null)
				{
					m_firstSleepingThread = thread;
				}
				else if (wakeTime <= m_firstSleepingThread.m_wakeTime)
				{
					thread.m_next = m_firstSleepingThread;
					m_firstSleepingThread = thread;
				}
				else
				{
					MicroThread t = m_firstSleepingThread;

					while (t.m_next != null && wakeTime >= t.m_next.m_wakeTime)
						t = t.m_next;

					thread.m_next = t.m_next;
					t.m_next = thread;
				}

				m_sleepingThreadCount++;

				ScheduleNext();
			}
			else
			{
				Print("Thread {0} woke up from sleep", m_currentThread);
			
				if (m_currentThread.m_error != null)
					throw m_currentThread.m_error;
			}
		}

		int ManageSleepers()
		{
			if (m_sleepingThreadCount == 0)
			{
#if EXTRA_CHECKS
				if(m_firstSleepingThread != null)
					throw new Exception();
#endif
				return -1;
			}

			long now = DateTime.UtcNow.Ticks;

			MicroThread t = m_firstSleepingThread;

			while(t != null && t.m_wakeTime < now)
			{
				MicroThread next = t.m_next;

				t.m_state = MicroThreadState.Scheduled;
				m_sleepingThreadCount--;

				AddInternal(t);

				t = next;
			}

			m_firstSleepingThread = t;

			if (m_firstSleepingThread == null)
				return -1;
			else
			{
				long wait = m_firstSleepingThread.m_wakeTime - now;
				return (int)TimeSpan.FromTicks(wait).TotalMilliseconds;
			}
		}

		[Conditional("MT_DEBUG")]
		void Print(string msg, params object[] args)
		{
#if MT_DEBUG
			if (m_debug)
				Console.WriteLine("SC: " + msg, args);
#endif
		}
	}
}
