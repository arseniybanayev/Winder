using System.Drawing;
using System.Runtime.InteropServices;

namespace Winder.Preview.ComInterop
{
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT {
		public int left;
		public int top;
		public int right;
		public int bottom;
	}
}