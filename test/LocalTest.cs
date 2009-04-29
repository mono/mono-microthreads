/**
 * Tries to test if the correct amount of stack is restored
 */

using System;
using System.Collections.Generic;
using System.Text;
using Mono.MicroThreads;
using Mono.Tasklets;

namespace LocalTest
{
	class LocalTest
	{
		static Continuation con = new Continuation();

		static void Main(string[] args)
		{
			int a = 01;
			int b = 11;
			int f = 51;

			Func1(ref a, b, f);
		}

		static void Func1(ref int a, int b, int f)
		{
			con.Mark();

			int c = 21;
			int d = 31;

			int ret = Func2(ref a, b, ref c, d, ref f);
			
			Func3(ret);
		}

		static int Func2(ref int a, int b, ref int c, int d, ref int f)
		{
			int ret;
			int e = 41;
			
			ret = con.Store(0);

			Console.WriteLine("loop {0}: Changed: a({1}) Unchanged: b({2}), c({3}), d({4}), e({5}), f({6})", ret, a, b, c, d, e, f);

			a++;
			b++;
			c++;
			d++;
			e++;
			f++;

			return ret;
		}

		static void Func3(int ret)
		{
			if (ret < 4)
			{
				con.Restore(ret + 1);
			}
		}
	}
}
