using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace ConsoleFileManager
{
	public enum CommandResult
	{
		Complite,
		Error = -1
	}
	public static class CommandManager
	{
		public static CommandResult CommandExecute(string command, FileMan fm)
		{
			//var args = command.Split(" ", StringSplitOptions.RemoveEmptyEntries);
			var args = GetCommandStrings(command).ToArray();

			if (args.Length <= 1)
			{
				MessageBox.ErrorQuery($"Exception", $"Not enough arguments", "OK");
				return CommandResult.Error;
			}

			switch (args.FirstOrDefault())
			{
				case "copy_file":
					if (File.Exists(args[1]))
					{
						if (args.Length > 2)
							return FileCopy(new FileInfo(args[1]), new FileInfo(args[2]));
						else
							return FileCopy(new FileInfo(args[1]), new FileInfo(Path.Combine(fm.CurrentDirectory.FullName, Path.GetFileName(args[1]))));
					}
					else
					{
						MessageBox.ErrorQuery($"Exception", $"The file '{args[1]}' does not exist", "OK");
						return CommandResult.Error;
					}

				case "copy_dir":
					if (args.Length > 1)
					{
						if (args.Length > 2)
							return DirectoryCopy(new DirectoryInfo(args[1]), new DirectoryInfo(args[2]));
						else
							return DirectoryCopy(new DirectoryInfo(args[1]), new DirectoryInfo(Path.Combine(fm.CurrentDirectory.FullName, Path.GetFileName(args[1]))));
					}
					else
					{
						MessageBox.ErrorQuery($"Exception", $"Not enough arguments", "OK");
						return CommandResult.Error;
					}

				case "del_file":
					return FileDelete(new FileInfo(args[1]));
				case "del_dir":
					return DirectoryDelete(new DirectoryInfo(args[1]));

				case "cd":
					var dirInfo = new DirectoryInfo(args[1]);
					if (dirInfo.Exists)
					{
						if(dirInfo.FullName == dirInfo.Root.FullName)
							fm.CurrentDirectory = null;
						else
							fm.CurrentDirectory = dirInfo;
						return CommandResult.Complite;
					}
					return CommandResult.Error;

				default:
					return CommandResult.Error;
			}
		}

		//	Парсинг введённой строки
		//	разбиение на состовные
		//	проверка строки взятых в кавычки
		static List<string> GetCommandStrings(string source)
		{
			var list = new List<string>();

			int lastIndex = 0;
			bool isSpacedString = false;
			for (int i = 0; i < source.Length; i++)
			{
				if (source[i] == '"')
				{
					if (isSpacedString)
					{
						var str = source.Substring(lastIndex, i - lastIndex);
						if (!string.IsNullOrWhiteSpace(str))
							list.Add(str);

						i++;
						lastIndex = i + 1;
						isSpacedString = false;
					}
					else
					{
						lastIndex = i + 1;
						isSpacedString = true;
					}
				}
				else if (!isSpacedString && source[i] == ' ')
				{
					var str = source.Substring(lastIndex, i - lastIndex);
					if(!string.IsNullOrWhiteSpace(str))
						list.Add(str);
					lastIndex = i + 1;
				}

				if(i == source.Length - 1 && lastIndex < i)
				{
					var str = source.Substring(lastIndex);
					if(!string.IsNullOrWhiteSpace(str))
						list.Add(str);
				}
			}

			return list;
		}


		private static CommandResult FileCopy(FileInfo sourceFileInfo, FileInfo destFileInfo)
		{
			try
			{
				if (destFileInfo.Exists)
				{
					if (MessageBox.Query($"Warning", $"File {destFileInfo.FullName} exists{Environment.NewLine}Override file?", "Yes", "No") == 0)
					{
						sourceFileInfo.CopyTo(destFileInfo.FullName, true);
					}
					else
					{
						//	Проверяем новое имя для копии файла, ищем свободное
						destFileInfo = new FileInfo(Path.Combine(destFileInfo.DirectoryName, $"{Path.GetFileNameWithoutExtension(destFileInfo.Name)}_Copy.{destFileInfo.Extension}"));
						if (destFileInfo.Exists)
						{
							int copy_index = 0;
							do
							{
								copy_index++;
								destFileInfo = new FileInfo(Path.Combine(destFileInfo.DirectoryName, $"{Path.GetFileNameWithoutExtension(destFileInfo.Name)}_Copy_{copy_index}.{destFileInfo.Extension}"));

							} while (destFileInfo.Exists);
						}
						sourceFileInfo.CopyTo(destFileInfo.FullName);
					}
					return CommandResult.Complite;
				}
				else
				{
					if (!Directory.Exists(destFileInfo.DirectoryName))
						Directory.CreateDirectory(destFileInfo.DirectoryName);
					sourceFileInfo.CopyTo(destFileInfo.FullName);
					return CommandResult.Complite;
				}
			}
			catch (Exception ex)
			{
				MessageBox.ErrorQuery($"Exception", $"{ex.Message}", "OK");
				File.AppendAllText("errors//random_name_exception.txt", $"{DateTime.Now:G}:\tFileCopy({sourceFileInfo.FullName}, {destFileInfo.FullName}), {ex.Message}");
				return CommandResult.Error;
			}
		}
		private static CommandResult DirectoryCopy(DirectoryInfo copyDir, DirectoryInfo targetDir)
		{
			try
			{
				if (copyDir.Exists)
				{
					var newDir = new DirectoryInfo(Path.Combine(targetDir.FullName, copyDir.Name));

					foreach(var dir in copyDir.GetDirectories())
						DirectoryCopy(dir, newDir);

					// Проверка после рекурсии копирования влоденных папок
					// чтобы сразу создавался окончательный путь
					if (!newDir.Exists)
						Directory.CreateDirectory(newDir.FullName);

					foreach(var file in copyDir.GetFiles())
						FileCopy(file, new FileInfo(Path.Combine(newDir.FullName, file.Name)));
				}
				else
				{
					MessageBox.ErrorQuery($"Exception", $"The directory '{copyDir.FullName}' does not exist", "OK");
					return CommandResult.Error;
				}
				return CommandResult.Complite;
			}
			catch (Exception ex)
			{
				MessageBox.ErrorQuery($"Exception", $"{ex.Message}", "OK");
				File.AppendAllText("errors//random_name_exception.txt", $"{DateTime.Now:G}:\tDirectoryCopy({copyDir.FullName}, {targetDir.FullName}), {ex.Message}");
				return CommandResult.Error;
			}
		}

		private static CommandResult FileDelete(FileInfo fileInfo)
		{
			try
			{
				if (fileInfo.Exists)
				{
					if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
					{
						MessageBox.ErrorQuery($"Exception", $"The file '{fileInfo.FullName}' is read only", "OK");
						return CommandResult.Error;
					}
					else
					{
						fileInfo.Delete();
						return CommandResult.Complite;
					}
				}
				else
				{
					MessageBox.ErrorQuery($"Exception", $"The file '{fileInfo.FullName}' does not exist", "OK");
					return CommandResult.Error;
				}
			}
			catch (Exception ex)
			{
				MessageBox.ErrorQuery($"Exception", $"{ex.Message}", "OK");
				File.AppendAllText("errors//random_name_exception.txt", $"{DateTime.Now:G}:\tFileDelete({fileInfo.FullName}), {ex.Message}");
				return CommandResult.Error;
			}
		}
		private static CommandResult DirectoryDelete(DirectoryInfo dirInfo)
		{
			try
			{
				if (dirInfo.Exists)
				{
					if (dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
					{
						MessageBox.ErrorQuery($"Exception", $"The directory '{dirInfo.FullName}' is read only", "OK");
						return CommandResult.Error;
					}
					else
					{
						dirInfo.Delete(true);
						return CommandResult.Complite;
					}
				}
				else
				{
					MessageBox.ErrorQuery($"Exception", $"The directory '{dirInfo.FullName}' does not exist", "OK");
					return CommandResult.Error;
				}
			}
			catch (Exception ex)
			{
				MessageBox.ErrorQuery($"Exception", $"{ex.Message}", "OK");
				File.AppendAllText("errors//random_name_exception.txt", $"{DateTime.Now:G}:\tDirectoryDelete({dirInfo.FullName}), {ex.Message}");
				return CommandResult.Error;
			}
		}
	}
}
