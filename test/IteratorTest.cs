/**
 * An iterator implemented with continuations
 * Note that this breaks easily, as we exit from the method
 * in which we have called Mark(). If it is called via a different
 * call path, the stack is formed differently and we have a crash.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Mono.MicroThreads;
using Mono.Tasklets;

namespace IteratorTest
{
	class IntRange
	{
		long m_min;
		long m_max;
		Continuation m_cont;

		public IntRange(long min, long max)
		{
			m_min = min;
			m_max = max;
		}

		public long GetNext()
		{
			if (m_cont == null)
			{
				m_cont = new Continuation();
				m_cont.Mark();
				for (long i = m_min; i < m_max; i++)
				{
					if (m_cont.Store(0) == 0)
						return i;
				}
				return 0;
			}
			else
			{
				m_cont.Restore(1);
				throw new Exception("not reached");
			}
		}
	}


	class IteratorTest
	{
		static void Main(string[] args)
		{
			long min = 1, max = 1000000;
			long tot = 0;
			DateTime t1 = DateTime.Now;

			foreach (int i in Range(min, max))
			{
				//Console.WriteLine("i={0}", i);
				tot++;
			}

			DateTime t2 = DateTime.Now;

			TimeSpan ts = t2 - t1;

			Console.WriteLine("Iteration with C# yield: {0}ms", ts.TotalMilliseconds);


			tot = 0;

			t1 = DateTime.Now;

			IntRange r = new IntRange(min, max);
			long x;

			while((x = r.GetNext()) != 0)
			{
				//Console.WriteLine("x={0}", x);
				tot++;
			}

			t2 = DateTime.Now;

			ts = t2 - t1;

			Console.WriteLine("Iteration with continuations: {0}ms", ts.TotalMilliseconds);
		}

		public static IEnumerable<long> Range(long min, long max)
		{
			for (long i = min; i < max; i++)
			{
				yield return i;
			}
		}
	}
}
