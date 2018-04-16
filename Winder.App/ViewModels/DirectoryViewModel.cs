using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Winder.App.Properties;
using Winder.App.WindowsUtilities;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public class DirectoryViewModel : FileSystemItemViewModel
	{
		public DirectoryViewModel(NormalizedPath path) : base(path) {
			_icon = new Lazy<ImageSource>(() => FileSystemImages.GetIcon(path, true));
			Info = new DirectoryInfo(path);
			Children = new ObservableCollection<FileSystemItemViewModel>();
		}

		/// <summary>
		/// Should be called by <see cref="FileSystemManager"/> to update the known child view models
		/// and notify any watchers that the collection might have changed.
		/// </summary>
		/// <param name="update"></param>
		public void UpdateCollection(Action<ObservableCollection<FileSystemItemViewModel>> update) {
			update(Children);
		}

		public ObservableCollection<FileSystemItemViewModel> Children { get; }

		/// <summary>
		/// Should be called by <see cref="FileSystemManager"/> to update the known <see cref="DirectoryInfo"/>
		/// and notify any watchers that properties derived from the new info might have changed.
		/// </summary>
		public void UpdateInfo(DirectoryInfo newInfo) {
			Info = newInfo;
		}

		public DirectoryInfo Info { get; private set; }
		public override FileSystemInfo FileSystemInfo => Info;

		public override string DisplayName => Info.Name;
	}
}