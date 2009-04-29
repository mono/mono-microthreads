
using System;
using System.Collections.Generic;
using System.MicroThreads;


namespace Test
{
	class TestClass
	{
		static Channel<int> s_channel;

		static int res = 0;

		static void Main()
		{
//			DateTime time1 = DateTime.Now;
			int a3 = 0x3beef;
			int a2 = 0x2beef;
			int a1 = 0x1beef;

			Continuation c = new Continuation();

			int v1 = 111;
			int v2 = 222;

			tt(v1, v2, c, 1);

			a1++;
			a2++;
			a3++;

			tt(v1, v2, c, 2);

//			DateTime time2 = DateTime.Now;

	Console.WriteLine("{0:x}, {1:x}, {2:x}", a1, a2, a3);
//			Console.WriteLine("total {0}", res);
//			Console.WriteLine("time {0}ms", (time2 - time1).Milliseconds);
//			Console.WriteLine("END");
		}

		static void Print(string msg, params object[] args)
		{
			Console.WriteLine(MicroThread.CurrentThread.Name + ": " + msg, args);
		}

		static void tt(int v1, int v2, Continuation c, int state)
		{
			if(state == 1)
			{
				c.Mark();
			
				int val = tt1(c, 0);
			
				Console.WriteLine(v1);
				Console.WriteLine(v2);

				v1++;
				v2++;
			}
			else
			{
				tt2(c, 1);
			}
		}

		static int tt1(Continuation c, int val)
		{
			Console.WriteLine("before store, {0}", val);
			int ret = c.Store(0);
			Console.WriteLine("after store, {0}", val);

			Console.WriteLine("loop {0}", ret);

			return ret;
		}

		static void tt2(Continuation c, int val)
		{
			tt3(c, val);
		}
		static void tt3(Continuation c, int val)
		{
			if(val < 5)
			{
				Console.WriteLine("before resume, {0}", val);
				c.Resume(val + 1);
				Console.WriteLine("after resume");
			}
			else
			{
//				Console.WriteLine("endingi {0}", val);
			}
		}

	}
}

