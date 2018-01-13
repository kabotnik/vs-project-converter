using System;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

namespace vs_project_converter
{
    class Program
    {
        static readonly List<string> commands = new List<string>
        {
            "preview", "convert", "help"
        };

        static void Help()
        {
            Console.WriteLine("To run vs-project-converter");
            Console.WriteLine();
            Console.WriteLine("\tdotnet run <preview|convert|help> <solutionPath|projectPath>");
            Console.WriteLine();
            Console.WriteLine("\tpreview\t\tCopies the project files with the same filename");
            Console.WriteLine("\t\t\twith `.converted` appended to the end. For sln files");
            Console.WriteLine("\t\t\tit will use the referenced name and path");
            Console.WriteLine();
            Console.WriteLine("\tconvert\t\tOverwrites the project files with the same filename.");
            Console.WriteLine("\t\t\tFor sln files it will use the referenced name and path.");
            Console.WriteLine("\t\t\tThis is a destructive operation! Backup your files!");
            Console.WriteLine();
            Console.WriteLine("\tsolutionPath\tPath to a solution file");
            Console.WriteLine();
            Console.WriteLine("\tprojectPath\tPath to a csproj file");
            Console.WriteLine();
        }

        static void Run(bool overwriteOnSave, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"Provided file {filePath} does not exist");
            }

            var fileInfo = new FileInfo(filePath);
            IConverter converter = null;
            if (fileInfo.Extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                converter = new Vs2015ToVs2017SolutionConverter(fileInfo);
            }
            else if (fileInfo.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                converter = new Vs2015ToVs2017ProjectConverter(fileInfo);
            }

            converter.ConvertAndSave(overwriteOnSave);
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.Error.WriteLine($"Use `help` command for help.");
                    Environment.Exit(0);
                }

                var command = args[0];
                if (!commands.Any(c => c.Equals(command, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.Error.WriteLine($"Command {command} is invalid.");
                    Console.WriteLine($"Use `help` command for help.");
                    Environment.Exit(0);
                }

                if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    Help();
                    Environment.Exit(0);
                }

                if (args.Length < 2)
                {
                    Console.Error.WriteLine("Please provide a valid sln or csproj file");
                    Console.WriteLine($"Use `help` command for help.");
                    Environment.Exit(0);
                }

                var file = args[1];

                if (command.Equals("preview", StringComparison.OrdinalIgnoreCase)) Run(false, file);
                if (command.Equals("convert", StringComparison.OrdinalIgnoreCase)) Run(true, file);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Encountered error: {ex.Message}");
                Console.Error.WriteLine($"Use `help` command for help.");
            }

        }
    }
}
