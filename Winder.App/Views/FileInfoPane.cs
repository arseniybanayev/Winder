﻿using System.Windows;
using System.Windows.Controls;
using Winder.App.ViewModels;
using Winder.App.WindowsUtilities;

namespace Winder.App.Views
{
	public class FileInfoPane : Grid, IFileSystemPane
	{
		private readonly FileViewModel _fileViewModel;

		public FileInfoPane(FileViewModel file) {
			// Basic display and interactivity settings
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			_fileViewModel = file;

			// File preview image
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
			var previewImage = new Image();
			previewImage.HorizontalAlignment = HorizontalAlignment.Stretch;
			previewImage.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
			previewImage.Source = FileSystemImages.GetThumbnail(file.Path, false);
			SetRow(previewImage, 0);
			Children.Add(previewImage);

			// File details
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
			var sizeLabel = new Label();
			sizeLabel.HorizontalAlignment = HorizontalAlignment.Center;
			sizeLabel.Content = $"Size: {file.FileSize}";
			SetRow(sizeLabel, 1);
			Children.Add(sizeLabel);
		}
	}
}