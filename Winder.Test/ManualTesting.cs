using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Winder.Test
{
	[TestClass]
	public class ManualTesting
	{
		[TestMethod]
		public void PlayWithDirectories() {
			var h = Preview.CreatePreviewHandler(@"C:\QuantEquity.txt", out var isInitialized);
		}
	}
}