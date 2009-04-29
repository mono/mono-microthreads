using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Mono.MicroThreads;

namespace WindowsApplication1
{
	static class Program
	{
		public static List<Dot> s_dotList = new List<Dot>();

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MyForm f = new MyForm();
			f.Show();

			new MicroThread(WindowRun).Start();

			for (int i = 0; i < 200; i++)
			{
				Dot d = new Dot(i, 0, Color.Red, f);
				MicroThread t = new MicroThread(d.Run);
				t.Start();
			}

			Scheduler.Run();

			//			Application.Run(f);
		}

		static void WindowRun()
		{
			while(true)
			{
				Application.DoEvents();
				MicroThread.CurrentThread.Yield();
			}
		}

	}

	public class MyForm : Form
	{
		public int m_mouseX, m_mouseY;
		public bool[,] m_area;
		public Bitmap m_bitmap;

		public MyForm()
		{
			//this.DoubleBuffered = true;

			int w = 200; int h = 200;
			m_area = new bool[w, h];
			for (int x = 0; x < w; x++)
				for (int y = 0; y < h; y++)
					m_area[x, y] = false;

			m_bitmap = new Bitmap(200, 200);

			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Name = "Form1";
			this.Text = "Form1";
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//base.OnPaint(e);

			//e.Graphics.DrawImage(m_bitmap, 0, 0);
		}

		private void Form1_MouseMove(object sender, MouseEventArgs e)
		{
			m_mouseX = e.X / 2;
			m_mouseY = e.Y / 2;
		}
	}

	public class Dot
	{
		Color m_color;
		int m_x, m_y;
		MyForm m_form;
		Pen m_pen;

		public Dot(int x, int y, Color color, MyForm form)
		{
			m_x = x;
			m_y = y;
			m_color = color;
			m_form = form;
			m_pen = new Pen(m_color);
			m_form.m_area[x, y] = true;
		}

		public void Run()
		{
			while(true)
			{
			int dx = m_form.m_mouseX - m_x;
			int dy = m_form.m_mouseY - m_y;
			int angle;

			if (dx == 0 && dy == 0)
			{
				MicroThread.CurrentThread.Yield();
				continue;
			}

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (dx >= 0)
					angle = 0;
				else
					angle = 180;
			}
			else
			{
				if (dy >= 0)
					angle = 90;
				else
					angle = 270;
			}

			if (TryAngle(AngleAdd(angle, 0)) == false)
				if (TryAngle(AngleAdd(angle, 90)) == false)
					if (TryAngle(AngleAdd(angle, -90)) == false)
						TryAngle(AngleAdd(angle, 180));

			MicroThread.CurrentThread.Yield();

			}
		}

		bool TryAngle(int angle)
		{
			int x = m_x + (int)Math.Cos(Math.PI * angle / 180);
			int y = m_y + (int)Math.Sin(Math.PI * angle / 180);

			if (x < 0 || x >= m_form.m_area.GetLength(0))
				return false;

			if (y < 0 || y >= m_form.m_area.GetLength(1))
				return false;

			if (m_form.m_area[x,y] == true)
				return false;

			Graphics g = m_form.CreateGraphics();

			g.DrawRectangle(Pens.White, m_x*2, m_y*2, 1, 1);
			m_form.m_area[m_x, m_y] = false;
			m_x = x;
			m_y = y;
			g.DrawRectangle(m_pen, m_x * 2, m_y * 2, 1, 1);
			m_form.m_area[m_x, m_y] = true;

			g.Dispose();

			return true;
		}

		int AngleAdd(int angle, int add)
		{
			angle += add;
			if(angle > 360)
				angle -= 360;

			if(angle < 0)
				angle += 360;

			return angle;
		}


	}
}
