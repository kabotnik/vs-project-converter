using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace vs_project_converter
{
    public class Vs2015ToVs2017SolutionConverter : IConverter
    {
        private readonly string _projectName;
        private readonly string _saveLocation;
        private readonly string _filePath;
        private readonly string _solutionLocation;

        public Vs2015ToVs2017SolutionConverter(FileInfo fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");

            _filePath = fileInfo.FullName;
            _solutionLocation = fileInfo.Directory.ToString();
            _projectName = fileInfo.Name.Substring(0,fileInfo.Name.LastIndexOf('.'));
            _saveLocation = fileInfo.FullName;         }
        public void ConvertAndSave(bool overwrite = false)
        {
            var solutionFile = File.ReadAllLines(_filePath);
            var projectLines = solutionFile.Where(s => s.StartsWith("Project")).ToList();

            var projects = new List<string>();
            foreach (var projectLine in projectLines)
            {
                var line = projectLine.Replace(" ", string.Empty);
                var split = line.IndexOf('=');
                var argStr = line.Substring(split, line.Length - 1 - split);
                var args = argStr.Split(',');
                var project = args[1];
                if (!project.ToLower().Contains(".csproj")) continue;

                project = project.Substring(1, project.Length - 1 - 1);
                projects.Add($"{_solutionLocation}\\{project}");
            }

            foreach (var project in projects)
            {
                try
                {
                    var fileInfo = new FileInfo(project);
                    var projectConverter = new Vs2015ToVs2017ProjectConverter(fileInfo);
                    projectConverter.ConvertAndSave(overwrite);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Encountered error processing {project} in {_projectName}: {ex.Message}");
                }
            }
        }
    }
}