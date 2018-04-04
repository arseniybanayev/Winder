using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Winder.Util;

namespace Winder.Test
{
	[TestClass]
	public class ManualTesting
	{
		[TestMethod]
		public void PlayWithDirectories() {
			var favorites = FileUtil.GetFavoritesFromUserLinks().ToList();
			
		}
	}
}