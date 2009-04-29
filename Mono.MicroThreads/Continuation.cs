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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Mono.MicroThreads
{
	public class Continuation : IDisposable
	{
#if MONO
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static IntPtr alloc_continuation();
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void free_continuation(IntPtr handle);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void mark_continuation_frame(IntPtr handle, int skip);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static int store_continuation(IntPtr handle, int data);
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void restore_continuation(IntPtr handle, int data);
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static int get_continuation_stack_size(IntPtr handle);
#elif CLR
		[DllImport("cont.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr alloc_continuation();

		[DllImport("cont.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void free_continuation(IntPtr handle);

		[DllImport("cont.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void mark_continuation_frame(IntPtr handle, int skip);

		[DllImport("cont.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int store_continuation(IntPtr handle, int val);

		[DllImport("cont.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int restore_continuation(IntPtr handle, int val);
#else
		#error NO PLATFORM
#endif

		IntPtr m_handle = IntPtr.Zero;

#if MT_DEBUG
		bool m_debug = false;
		static int s_numberCounter = 0;
		int m_number = s_numberCounter++;
#endif

		public Continuation()
		{
			m_handle = alloc_continuation();
			Print("Continuation()");
		}

		~Continuation()
		{
			Print("~Continuation()");

			if (m_handle != IntPtr.Zero)
			{
				free_continuation(m_handle);
				m_handle = IntPtr.Zero;
			}
		}

		public void Dispose()
		{
			Print("Dispose()");

			if (m_handle != IntPtr.Zero)
			{
				free_continuation(m_handle);
				m_handle = IntPtr.Zero;
			}

			GC.SuppressFinalize(this);
		}

		public IntPtr Handle
		{
			get { return m_handle; }
		}

		[MethodImplAttribute(MethodImplOptions.NoInlining)]
		public void Mark()
		{
			Print("Mark()");
			// skip 1 frame, ie. this function
			mark_continuation_frame(m_handle, 1);
		}

		public int Store(int data)
		{
			Print("Store({0})", data);
			int res = store_continuation(m_handle, data);
			Print("Store({0}) = {1}", data, res);
			return res;
		}

		public void Restore(int data)
		{
			Print("Restore({0})", data);
			restore_continuation(m_handle, data);
			Print("Restore() exit (NEVER REACHED)");
		}

		public int StackSize
		{
			get { return get_continuation_stack_size(m_handle); }
		}

		[Conditional("MT_DEBUG")]
		void Print(string msg, params object[] args)
		{
#if MT_DEBUG
			if(m_debug)
				Console.WriteLine("CO" + m_number.ToString() + ": " + msg, args);
#endif
		}
	}
}
