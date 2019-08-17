using System.Collections.Generic;
using System.IO;
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

			// 1. File preview image
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
			var previewImage = new Image();
			previewImage.HorizontalAlignment = HorizontalAlignment.Stretch;
			previewImage.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
			previewImage.Source = FileSystemImages.GetThumbnail(file.Path, false);
			SetRow(previewImage, 0);
			Children.Add(previewImage);

			// 2. File details
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
			var fileDetailsStackPanel = new StackPanel {
				Orientation = Orientation.Vertical
			};
			SetRow(fileDetailsStackPanel, 1);
			Children.Add(fileDetailsStackPanel);

			// File details: description
			// filename.jpef
			//     JPEG Image - 91 KB
			// 
			//        Tags  Add Tags...
			//     Created  Sunday, July 15, 2018 at 2:58 PM
			//    Modified  Sunday, July 15, 2018 at 2:58 PM
			// Last opened  Sunday, July 15, 2018 at 2:58 PM
			//  Dimensions  600 x 400

			var descriptiveDetails = new[] {
				file.DisplayName, // TODO: Truncate the file display name just like in the other places
				$"{Path.GetExtension(file.Path.Value)} - {file.FileSizeString}",
				""
			};
			foreach (var desc in descriptiveDetails) {
				fileDetailsStackPanel.Children.Add(new Label {
					HorizontalAlignment = HorizontalAlignment.Center,
					Content = desc
				});
			}

			var properties = new Dictionary<string, string> {
				{"Tags", "Add Tags..."},
				{"Created", file.FileSystemInfo.CreationTime.ToString("f")},
				{"Modified", file.FileSystemInfo.LastWriteTime.ToString("f")},
				{"Last opened", file.FileSystemInfo.LastAccessTime.ToString("f")}
				// TODO: Add more file type-specific properties (like Dimensions: 600x400)
			};

			var propertiesGrid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			propertiesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6, GridUnitType.Star) });
			propertiesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) });
			fileDetailsStackPanel.Children.Add(propertiesGrid);
			var rowIndex = 0;
			foreach (var kv in properties) {
				propertiesGrid.RowDefinitions.Add(new RowDefinition());
				var label = new Label {
					HorizontalAlignment = HorizontalAlignment.Right,
					Content = kv.Key
				};
				SetColumn(label, 0);
				SetRow(label, rowIndex);
				propertiesGrid.Children.Add(label);
				label = new Label {
					HorizontalAlignment = HorizontalAlignment.Left,
					Content = kv.Value
				};
				SetColumn(label, 1);
				SetRow(label, rowIndex);
				propertiesGrid.Children.Add(label);
				rowIndex++;
			}
		}
	}
}