using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winder
{
	public class DirectoryViewModel
	{
		public DirectoryViewModel(string directoryPath) {
			DirectoryInfo = new DirectoryInfoExtended(directoryPath);
			Children = new ObservableCollection<FileSystemInfoExtended>(DirectoryInfo.GetChildren());
		}

		public DirectoryInfoExtended DirectoryInfo { get; }

		public ObservableCollection<FileSystemInfoExtended> Children { get; }
	}
}