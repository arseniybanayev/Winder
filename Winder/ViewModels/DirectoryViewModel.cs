using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Winder.ViewModels
{
	public class DirectoryViewModel : FileSystemItemViewModel
	{
		private Lazy<ObservableCollection<FileSystemItemViewModel>> _children;
		public ObservableCollection<FileSystemItemViewModel> Children => _children.Value;

		public DirectoryViewModel(DirectoryInfo directoryInfo) : base(directoryInfo)
		{
			_children = new Lazy<ObservableCollection<FileSystemItemViewModel>>(() =>
				new ObservableCollection<FileSystemItemViewModel>(Source.GetFileSystemInfos().Select(FileSystemItemViewModel.Create)));
		}

		public DirectoryInfo Source => (DirectoryInfo)SourceUntyped;

		public override bool IsDirectory => true;
	}
}