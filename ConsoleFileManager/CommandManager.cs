using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace ConsoleFileManager
{
	public enum CommandResult
	{
		Complite,
		Error,
		CommandNotFound,
		SourceFileNotFound,
		SourceDirectoryNotFound,
		NotEnoughArguments,
	}
	public static class CommandManager
	{
		public static CommandResult CommandExecute(string command, FileMan fm)
		{
			var args = command.Split(" ", StringSplitOptions.RemoveEmptyEntries);
			switch (args.FirstOrDefault())
			{
				case "copy_file":
					if (args.Length > 1)
					{
						if (File.Exists(args[1]))
						{
							if (args.Length > 2)
								return FileCopy(args[1], args[2]);
							else
								return FileCopy(args[1], Path.Combine(fm.CurrentDirectory.FullName, Path.GetFileName(args[1])));
						}
						else
						{
							MessageBox.ErrorQuery($"Exception", $"The file '{args[1]}' does not exist", "OK");
							return CommandResult.SourceFileNotFound;
						}
					}
					else
					{
						MessageBox.ErrorQuery($"Exception", $"Not enough arguments", "OK");
						return CommandResult.NotEnoughArguments;
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
						return CommandResult.NotEnoughArguments;
					}

				default:
					return CommandResult.CommandNotFound;
			}
		}

		private static CommandResult FileCopy(string sourceFileName, string destFileName)
		{
			try
			{
				if (File.Exists(destFileName))
				{
					if (MessageBox.Query($"Warning", $"File {destFileName} exists{Environment.NewLine}Override file?", "Yes", "No") == 0)
					{
						File.Copy(sourceFileName, destFileName, true);
					}
					else
					{
						destFileName = Path.Combine(Path.GetDirectoryName(destFileName), $"{Path.GetFileNameWithoutExtension(destFileName)}_Copy.{Path.GetExtension(destFileName)}");
						if (File.Exists(destFileName))
						{
							int copy_index = 0;
							do
							{
								copy_index++;
								destFileName = Path.Combine(Path.GetDirectoryName(destFileName), $"{Path.GetFileNameWithoutExtension(destFileName)}_Copy_{copy_index}.{Path.GetExtension(destFileName)}");

							} while (File.Exists(destFileName));
						}
						File.Copy(sourceFileName, destFileName);
					}
					return CommandResult.Complite;
				}
				else
				{
					if (!Directory.Exists(Path.GetDirectoryName(destFileName)))
						Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
					File.Copy(sourceFileName, destFileName);
					return CommandResult.Complite;
				}
			}
			catch (Exception ex)
			{
				MessageBox.ErrorQuery($"Exception", $"{ex.Message}", "OK");
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
						FileCopy(file.FullName, Path.Combine(newDir.FullName, file.Name));
				}
				else
				{
					MessageBox.ErrorQuery($"Exception", $"The directory '{copyDir.FullName}' does not exist", "OK");
					return CommandResult.SourceDirectoryNotFound;
				}
				return CommandResult.Complite;
			}
			catch (Exception ex)
			{
				MessageBox.ErrorQuery($"Exception", $"{ex.Message}", "OK");
				return CommandResult.Error;
			}
		}
	}
}
