using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace ConsoleFileManager
{
	public class FileSystemInfoDataSource : IListDataSource
	{
		public IList<FileSystemInfo> Source { get; set; } = new List<FileSystemInfo>();
		public FileSystemInfo SelectedItem { get; set; }

		public int Count => Source.Count;
		public int Length => Source.Count;


		public FileSystemInfoDataSource() { }
		public FileSystemInfoDataSource(IList<FileSystemInfo> source) => 
			Source = source;


		public bool IsMarked(int item)
		{
			return true;
		}

		public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
		{
			container.Move(col, line);
			var attr = driver.GetAttribute();
			switch (Source[item])
			{
				case FileInfo file:
					if(selected)
						driver.SetAttribute(new Attribute(Color.Green, Color.Gray));
					else
						driver.SetAttribute(new Attribute(Color.BrightGreen, Color.Blue));
					driver.AddStr($"{Source[item].Name}".PadRight(width));
					break;

				case DirectoryInfo dir:
					if (selected)
						driver.SetAttribute(new Attribute(Color.BrightBlue, Color.Gray));
					else
						driver.SetAttribute(new Attribute(Color.White, Color.Blue));
					if (dir.ToString().Equals(".."))
						driver.AddStr($"..".PadRight(width));
					else
						driver.AddStr($"{Source[item].Name}".PadRight(width));
					break;

				default:
					break;
			}
			driver.SetAttribute(attr);
		}

		public void SetMark(int item, bool value)
		{
			if(Count > item)
				SelectedItem = Source[item];
		}

		public IList ToList()
		{
			return Source.ToList();
		}
	}
}
