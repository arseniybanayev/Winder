﻿using System;
using System.Windows;
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
			previewImage.Source = FileSystemImages.GetThumbnail(file.Source);
			SetRow(previewImage, 0);
			Children.Add(previewImage);

			// File details
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
			var sizeLabel = new Label();
			sizeLabel.HorizontalAlignment = HorizontalAlignment.Center;
			sizeLabel.Content = $"Size: {FileSizeString}";
			SetRow(sizeLabel, 1);
			Children.Add(sizeLabel);
		}

		private static readonly string[] ByteSuffixes = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; // Longs run out around EB

		public string FileSizeString {
			get {
				var byteCount = _fileViewModel.Source.Length;
				if (byteCount == 0)
					return "0" + ByteSuffixes[0];
				var bytes = Math.Abs(byteCount);
				var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
				var num = Math.Round(bytes / Math.Pow(1024, place), 1);
				return $"{Math.Sign(byteCount) * num}{ByteSuffixes[place]}";
			}
		}
	}
}