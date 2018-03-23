﻿using System.IO;

namespace Winder.ViewModels
{
	public class FileViewModel : FileSystemItemViewModel
	{
		public FileViewModel(FileInfo fileInfo) : base(fileInfo) {

		}

		public FileInfo Source => (FileInfo)SourceUntyped;

		public override bool IsDirectory => false;
	}
}