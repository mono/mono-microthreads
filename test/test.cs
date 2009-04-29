/**
 * An iterator implemented with continuations
 */

using System;
using System.Collections.Generic;
using System.Text;
using Mono.MicroThreads;

namespace IteratorTest
{
	class IntRange
	{
		Continuation m_cont;

		public int GetNext(int a, int b, int c, int d, int e)
		{
			if (m_cont == null)
			{
				m_cont = new Continuation();
				m_cont.Mark();
				return m_cont.Store(0);
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
			IntRange r = new IntRange();

			long num = 9;

			num = r.GetNext(1, 2, 3, 4, 5);
			Console.WriteLine("first {0}", num);
			if(num == 1) {
				Console.WriteLine("err");
				return;
			}
			num = r.GetNext(6, 7, 8, 9, 10);
			Console.WriteLine("second {0}", num);
		}

	}
}
