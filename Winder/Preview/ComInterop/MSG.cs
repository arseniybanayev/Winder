using System;
using System.Runtime.InteropServices;

namespace Winder.Preview.ComInterop
{
	[StructLayout(LayoutKind.Sequential)]
	public struct MSG
	{
		private IntPtr hwnd;
		public int message;
		private IntPtr wParam;
		private IntPtr lParam;
		public int time;
		public int pt_x;
		public int pt_y;
	}
}