using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Terminal.Gui;
using static Terminal.Gui.View;

namespace ConsoleFileManager
{
	public class FileMan
	{
		private string currentDirectory = Settings.Get("CurrentDirectory") ?? Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
		public DirectoryInfo CurrentDirectory
		{
			get
			{
				if (currentDirectory != null && Directory.Exists(currentDirectory))
					return new DirectoryInfo(currentDirectory);
				else
					return null;
			}
			set
			{
				if (value != null && value.Exists)
				{
					currentDirectory = value.FullName;
					Settings.Update("CurrentDirectory", value.FullName);
				}
				else
				{
					currentDirectory = null;
					Settings.Update("CurrentDirectory", "");
				}
			}
		}
		public string ShowCurrentDirectory =>
			CurrentDirectory != null ? CurrentDirectory.FullName : "My Computer";
		public FileInfo CurrentFile { get; set; }


		public List<DirectoryInfo> Directories
		{
			get
			{
				try
				{
					if (CurrentDirectory != null)
						return new List<DirectoryInfo>(CurrentDirectory?.GetDirectories());
				}
				catch (Exception ex)
				{
					if (CurrentDirectory != null)
					{
						if (MessageBox.ErrorQuery($"Exception", $"{ex.Message}{Environment.NewLine}Run as Admin?", "OK", "Cancel") == 0)
						{
							var appPath = Assembly.GetExecutingAssembly().Location;
							if (Path.GetExtension(appPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
							{
								//	dotnet myApp.dll --not work
								//RunAsAdmin("dotnet", appPath, Path.GetDirectoryName(appPath));
								//Environment.Exit(0);
								appPath = Path.Combine(Path.GetDirectoryName(appPath), $"{Path.GetFileNameWithoutExtension(appPath)}.exe");
							}

							if (File.Exists(appPath))
							{
								RunAsAdmin(appPath);
								Environment.Exit(0);
							}
						}

						CurrentDirectory = CurrentDirectory.Parent;
						return new List<DirectoryInfo>(CurrentDirectory?.GetDirectories());
					}
					else
						MessageBox.ErrorQuery($"Exception", $"{ex.Message}", "OK");
				}
				return DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => new DirectoryInfo(d.Name)).ToList();
			}
		}
		public List<FileInfo> Files
		{
			get
			{
				if (CurrentDirectory != null)
					return new List<FileInfo>(CurrentDirectory.GetFiles());
				return new List<FileInfo>();
			}
		}

		public List<FileSystemInfo> DirsAndFiles
		{
			get
			{
				var list = new List<FileSystemInfo>();
				if (CurrentDirectory != null)
				{
					list.Add(new DirectoryInfo(".."));

					foreach (var dir in Directories)
						list.Add(dir);

					foreach (var file in Files)
						list.Add(file);
				}
				else
					foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
						list.Add(new DirectoryInfo(drive.Name));

				return list;
			}
		}

		#region View Components
		Window win;
		ListView dirsList;

		Window winInfo;
		ListView fileInfo;

		TextField commandLine;
		#endregion
		public FileMan()
		{
			Application.Init();
			var top = Application.Top;

			win = new Window("File Manager")
			{
				Width = Dim.Fill(),
				Height = Dim.Percent(100) - Dim.Sized(9)
			};
			dirsList = new ListView(new FileSystemInfoDataSource(DirsAndFiles))
			{
				Width = Dim.Fill(),
				Height = Dim.Fill(),
			};
			
			dirsList.OpenSelectedItem += dirsList_OpenSelectedItem;
			dirsList.SelectedItemChanged += dirsList_SelectedItemChanged;
			win.Add(dirsList);
			top.Add(win);

			winInfo = new Window("Info")
			{
				Y = Pos.Bottom(win),
				Width = Dim.Fill(),
				Height = Dim.Sized(8),
			};
			fileInfo = new ListView()
			{
				Y = -1,
				Width = Dim.Fill(),
				Height = Dim.Fill(),
			};

			winInfo.Add(fileInfo);
			top.Add(winInfo);

			commandLine = new TextField("")
			{
				Y = Pos.Bottom(winInfo),
				Width = Dim.Fill(),
			};

			commandLine.KeyPress += commandLine_KeyPress;
			top.Add(commandLine);
		}


		private void dirsList_OpenSelectedItem(ListViewItemEventArgs eventArgs)
		{
			switch (eventArgs.Value)
			{
				case FileInfo file:
					if (file.Exists)
					{
						CurrentFile = file;
						Process.Start("explorer", CurrentFile.FullName);
					}
					break;

				case DirectoryInfo dir:
					if (dir.ToString().Equals(".."))
						CurrentDirectory = CurrentDirectory.Parent;
					else if (dir.Exists)
						CurrentDirectory = dir;

					dirsList.Source = new FileSystemInfoDataSource(DirsAndFiles);
					win.Title = ShowCurrentDirectory;
					fileInfo.SetSource(null);
					break;

				default:
					break;
			}
		}
		private void dirsList_SelectedItemChanged(ListViewItemEventArgs eventArgs)
		{
			fileInfo.SetSource(null);

			switch (eventArgs.Value)
			{
				case FileInfo file:
					if (file.Exists)
					{
						CurrentFile = file;
						fileInfo.SetSource(new string[]
						{
								"", $"File Name: {CurrentFile.Name}",
								$"File Size: {GetStringSize(CurrentFile.Length)}",
								$"Crate Time: {CurrentFile.CreationTime}",
								$"Atributes: {CurrentFile.Attributes}"
						});
					}
					break;

				case DirectoryInfo dir:
					if (dir.ToString().Equals(".."))
					{
					}
					else if (dir.Root.FullName.Equals(dir.FullName))
					{
						foreach (var drive in DriveInfo.GetDrives())
							if (drive.Name.Equals(dir.FullName))
								fileInfo.SetSource(new string[]
								{   "", $"Drive Name: {drive.Name}",
										$"Drive Format: {drive.DriveFormat}",
										$"Drive Type: {drive.DriveType}",
										$"Size: {GetStringSize(drive.AvailableFreeSpace)}/{GetStringSize(drive.TotalSize)}",
										$"Atributes: {dir.Attributes}"
								});
					}
					else if (dir.Exists)
						fileInfo.SetSource(new string[]
						{
								"", $"Directory Name: {dir.Name}",
								$"Crate Time: {dir.CreationTime}",
								$"Atributes: {dir.Attributes}"
						});
					break;

				default:
					break;
			}
		}


		List<string> commands = Settings.Get("Commands")?.Split(Environment.NewLine).ToList() ?? new List<string>();
		private void commandLine_KeyPress(KeyEventEventArgs eventArgs)
		{
			switch (eventArgs.KeyEvent.Key)
			{
				case Key.V | Key.CtrlMask:
					//commandLine.Text = $"{commandLine.Text}{Clipboard.Contents?.Copy()}";
					break;

				case Key.Enter:
					if(CommandManager.CommandExecute(commandLine.Text.ToString(), this) != CommandResult.Error)
					{

					}

					if (!commands.Contains($"{commandLine.Text}"))
					{
						commands.Add($"{commandLine.Text}");
						Settings.Update("Commands", string.Join(Environment.NewLine, commands));
					}

					eventArgs.Handled = true;
					break;

				case Key.CursorUp:
					if(commands.Contains($"{commandLine.Text}"))
					{
						int index = commands.IndexOf($"{commandLine.Text}");
						if (index > 1)
							commandLine.Text = commands[index - 1];
					}
					else if(commands.Count > 0)
							commandLine.Text = commands.Last();
					eventArgs.Handled = true;
					break;

				case Key.CursorDown:
					if (commands.Contains($"{commandLine.Text}"))
					{
						int index = commands.IndexOf($"{commandLine.Text}");
						if (commands.Count > index + 1)
							commandLine.Text = commands[index + 1];
					}
					else if (commands.Count > 0)
						commandLine.Text = commands.First();
					eventArgs.Handled = true;
					break;
			}
		}

		public void Run()
		{
			Application.Run();
		}

		static string GetStringSize(long size)
		{
			string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
			int index = 0;
			double curSize = size;

			while (size > 1024)
			{
				curSize = size / 1024d;
				size = size / 1024;
				index++;
			}

			return $"{curSize:0.00} {suffixes[index]}";
		}
		static void RunAsAdmin(string fileName, string arguments = "", string workDir = "")
		{
			ProcessStartInfo processInfo = new ProcessStartInfo();

			processInfo.FileName = fileName;
			processInfo.Arguments = arguments;
			processInfo.WorkingDirectory = workDir;
			processInfo.UseShellExecute = true;
			processInfo.Verb = "runas";
			
			Process.Start(processInfo);
		}
	}
}
