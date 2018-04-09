using System;
using System.Runtime.InteropServices;

namespace Winder.Preview.ComInterop
{
	[StructLayout(LayoutKind.Sequential)]
	public struct MSG
	{
		private readonly IntPtr hwnd;
		public int message;
		private readonly IntPtr wParam;
		private readonly IntPtr lParam;
		public int time;
		public int pt_x;
		public int pt_y;
	}
}