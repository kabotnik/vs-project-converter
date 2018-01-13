using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace vs_project_converter
{
    public class Vs2015ToVs2017ProjectConverter : IConverter
    {
        private readonly string _projectName;
        private readonly string _saveLocation;
        private readonly string _filePath;

        public Vs2015ToVs2017ProjectConverter(FileInfo fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");

            _filePath = fileInfo.FullName;
            _projectName = fileInfo.Name.Substring(0,fileInfo.Name.LastIndexOf('.'));
            _saveLocation = fileInfo.FullName; 
        }

        public void ConvertAndSave(bool overwrite = false)
        {
            // http://www.natemcmaster.com/blog/2017/03/09/vs2015-to-vs2017-upgrade/#approach-two-manually-convert
            var xmlFile = File.ReadAllText(_filePath);
            xmlFile = xmlFile.Replace(Constants.Attributes.Namespace, string.Empty);
            var document = XDocument.Parse(xmlFile);
            document.Declaration = null;

            // Change the project attributes
            var project = document.Element(Constants.Elements.Project);

            ModifyProjectAttributes(project);
            RemoveImports(project);
            RemoveChooses(project);
            ModifyTargetEnvironment(project);      
            RemoveCompiles(project);
            RemoveEmbeddedResources(project);
            RemoveContent(project);
            RemoveNones(project);
            RemovePropertyGroupElements(project);
            ModifyPackageReferences(project);
            ModifyProjectReferences(project);
            ModifySystemReferences(project);

            // Remove empty elements when done
            RemoveEmptyElements(project);

            // Do this to prevent the XML declaration line from getting saved
            var xmlString = document.ToString();
            // xmlString = xmlString.Replace(Constants.Attributes.Namespace, string.Empty);

            if (overwrite)
            {
                File.WriteAllText($"{_saveLocation}", xmlString);
            }
            else
            {
                File.WriteAllText($"{_saveLocation}.converted", xmlString);
            }            
        }

        private void ModifyProjectAttributes(XElement element)
        {
            element.RemoveAttributes();
            element.Attributes("xmlns").Remove();

            if (IsWebApplication(element))
            {
                element.SetAttributeValue(Constants.Attributes.Sdk, Constants.CoreWebApplication);
            }
            else
            {
                element.SetAttributeValue(Constants.Attributes.Sdk, Constants.CoreStandardProject);
            }
        }

        private void RemoveImports(XElement element)
        {
            // Remove the imports that aren't needed (that are known about so far)
            var imports = element.Elements(Constants.Elements.Import);
            if (!imports.Any()) return;

            var importsToRemove = new List<XElement>();
            foreach (var import in imports)
            {
                var importAttr = import.Attribute(Constants.Attributes.Project);
                if (importAttr != null && Constants.Imports.Any(i=>importAttr.Value.Contains(i)))
                {
                    importsToRemove.Add(import);
                }
            }
            RemoveNodes(importsToRemove);
        }

        private void RemoveChooses(XElement element)
        {
            // Remove choose elements, if they exist
            var chooses = element.Elements(Constants.Elements.Choose);
            if (!chooses.Any()) return;

            var choosesToRemove = new List<XElement>();
            foreach (var choose in chooses)
            {
                choosesToRemove.Add(choose);
            }
            RemoveNodes(choosesToRemove);
        }

        private void ModifyTargetEnvironment(XElement element)
        {
            var targetFrameworkVersion = element.Descendants(Constants.Elements.TargetFrameworkVersion).SingleOrDefault();
            if (targetFrameworkVersion == null) return;

            var version = targetFrameworkVersion.Value;
            var targetFramework = new XElement(Constants.Elements.TargetFramework);
            if (Constants.TargetFrameworks.ContainsKey(version))
            {
                targetFramework.SetValue(Constants.TargetFrameworks[version]);
            }
            targetFrameworkVersion.ReplaceWith(targetFramework);
        }

        private void RemoveCompiles(XElement element)
        {
            var compiles = element.Descendants(Constants.Elements.Compile);
            if (!compiles.Any()) return;

            var compilesToRemove = new List<XElement>();
            foreach (var compile in compiles)
            {
                if (compile.Parent.Name == Constants.Elements.ItemGroup)
                {
                    compilesToRemove.Add(compile);
                }
            }
            var itemGroupsToRemove = compilesToRemove.Select(c => c.Parent).Distinct().ToList();
            RemoveNodes(itemGroupsToRemove);
        }

        private void RemoveEmbeddedResources(XElement element)
        {
            var embeddedResources = element.Descendants(Constants.Elements.EmbeddedResource);
            if (!embeddedResources.Any()) return;

            var embeddedResourcesToRemove = new List<XElement>();
            foreach (var embeddedResource in embeddedResources)
            {
                if (embeddedResource.Parent.Name == Constants.Elements.ItemGroup)
                {
                    embeddedResourcesToRemove.Add(embeddedResource);
                }
            }
            var itemGroupsToRemove = embeddedResourcesToRemove.Select(c => c.Parent).Distinct().ToList();
            RemoveNodes(itemGroupsToRemove);
        }

        private void RemoveContent(XElement element)
        {
            var contents = element.Descendants(Constants.Elements.Content);
            if (!contents.Any()) return;

            var contentsToRemove = new List<XElement>();
            foreach (var content in contents)
            {
                if (content.Parent.Name == Constants.Elements.ItemGroup)
                {
                    contentsToRemove.Add(content);
                }
            }
            RemoveNodes(contentsToRemove);
        }

        private void RemoveNones(XElement element)
        {
            var nones = element.Descendants(Constants.Elements.None);
            if (!nones.Any()) return;

            var nonesToRemove = new List<XElement>();
            foreach (var none in nones)
            {
                if (none.Parent.Name == Constants.Elements.ItemGroup)
                {
                    nonesToRemove.Add(none);
                }
            }
            RemoveNodes(nonesToRemove);
        }

        private void RemovePropertyGroupElements(XElement element)
        {
            var itemsToRemove = new List<XElement>();

            foreach (var item in Constants.Elements.PropertyGroupElements)
            {
                var pgElements = element.Descendants(item);
                foreach (var pgElement in pgElements)
                {
                    if (pgElement.Name != Constants.Elements.RootNamespace
                        && pgElement.Name != Constants.Elements.AssemblyName )
                    {
                        itemsToRemove.Add(pgElement);
                        continue;
                    }

                    if (IsSameAsProject(pgElement.Value))
                    {
                        itemsToRemove.Add(pgElement);
                    }
                }
            }

            RemoveNodes(itemsToRemove);
        }

        private void ModifyPackageReferences(XElement element)
        {
            var packageReferences = element.Descendants(Constants.Elements.Reference)
                                            .Where(e => e.HasElements)
                                            .ToList();
            if (!packageReferences.Any()) return;

            XElement parent = null;
            var versionDictionary = new Dictionary<string,string>();
            var packageDictionary = new Dictionary<string,XElement>();
            var packageNames = new List<string>();
            var referencesToRemove = new List<XElement>();
            foreach (var package in packageReferences)
            {
                if (parent == null) parent = package.Parent;

                var assembly = GetPackageName(package);
                var version = GetPackageVersion(package);
                if (!string.IsNullOrEmpty(assembly) && !string.IsNullOrEmpty(version))
                {
                    if (!versionDictionary.ContainsKey(assembly) && !packageDictionary.ContainsKey(assembly))
                    {
                        versionDictionary.Add(assembly,version);
                        packageDictionary.Add(assembly,package);
                    }
                    else
                    {
                        referencesToRemove.Add(package);
                    }
                }
            }

            foreach (var package in packageDictionary)
            {
                var newPackage = new XElement(Constants.Elements.PackageReference);
                var packageToReplace = package.Value;
                var packageName = package.Key;
                var packageVersion = versionDictionary[packageName];

                newPackage.SetAttributeValue(Constants.Attributes.Include, package.Key);
                newPackage.SetAttributeValue(Constants.Attributes.Version, packageVersion);

                packageToReplace.ReplaceWith(newPackage);
            }
            RemoveNodes(referencesToRemove);

            // Lets reorder them by PackageReference and Reference
            Reorder(parent);            
        }

        private void ModifyProjectReferences(XElement element)
        {
            var projectReferences = element.Descendants(Constants.Elements.ProjectReference)
                                            .ToList();
            if (!projectReferences.Any()) return;

            XElement parent = null;
            foreach (var projectReference in projectReferences)
            {
                if (parent == null) parent = projectReference.Parent;

                var existingInclude = projectReference.Attribute(Constants.Attributes.Include);
                if (existingInclude == null) continue;

                var newReference = new XElement(projectReference.Name);
                newReference.SetAttributeValue(Constants.Attributes.Include, existingInclude.Value);
                projectReference.ReplaceWith(newReference);
            }
        }

        private void ModifySystemReferences(XElement element)
        {
            var systemRefernces = element.Descendants(Constants.Elements.Reference)
                                            .ToList();
            if (!systemRefernces.Any()) return;

            XElement parent = null;
            foreach (var sysRef in systemRefernces)
            {
                if (parent == null) parent = sysRef.Parent;

                var includeAttr = sysRef.Attribute(Constants.Attributes.Include);
                if (includeAttr == null) continue;

                var includeValue = includeAttr.Value;
                var includeTrimmed = includeValue.Replace(" ", string.Empty);
                var includeChunks = includeTrimmed.Split(',');
                if (includeChunks.Length == 1) continue;

                sysRef.SetAttributeValue(Constants.Attributes.Include, includeChunks[0]);
            }

            Reorder(parent);
        }

        private void RemoveEmptyElements(XElement element)
        {
            foreach(XElement child in element.Descendants().Reverse())
            {
                if(!child.HasElements && string.IsNullOrEmpty(child.Value) && !child.HasAttributes) child.Remove();
            }
        }

        private void RemoveXmlDeclaration(XDocument document)
        {
            var node = document.Nodes().Where(n => n.NodeType == XmlNodeType.XmlDeclaration).SingleOrDefault();
            if (node == null) return;

            node.Remove();
        }

        private void Reorder(XElement element)
        {
            var ordered = new XElement(element.Name,
                from child in element.Elements()
                orderby child.Name.ToString()
                select child);
            element.ReplaceWith(ordered);
        }

        private void RemoveNodes(List<XElement> nodesToRemove)
        {
            foreach (var element in nodesToRemove)
            {
                element.Remove();
            }
        }

        private void RemoveRootNamespace(XElement element)
        {
            var exEl = new XElement(element.Name.LocalName);
            exEl.Value = element.Value;

            foreach (var attr in element.Attributes())
            {
                exEl.Add(attr);
            }
        }

        private bool IsSameAsProject(string value)
        {
            return value == _projectName;
        }

        private string GetPackageVersion(XElement element)
        {
            var hintPath = element.Element(Constants.Elements.HintPath);
            if (hintPath == null) return string.Empty;

            var hintPathSplit = hintPath.Value.Split('\\');
            var index = Array.IndexOf(hintPathSplit, "lib");
            var packageIndex = index - 1;

            var package = hintPathSplit[packageIndex];
            var packageSplit = package.Split('.');
            packageSplit = packageSplit.Reverse().ToArray();

            var version = new List<string>();
            foreach (var str in packageSplit)
            {
                int n;
                if (int.TryParse(str, out n))
                {
                    version.Add(str);
                }
                else
                {
                    break;
                }
            }

            var finalVersion = version.ToArray().Reverse().ToArray();
            return string.Join(".", finalVersion);
        }

        private string GetPackageName(XElement element)
        {
            var hintPath = element.Element(Constants.Elements.HintPath);
            if (hintPath == null) return string.Empty;

            var hintPathSplit = hintPath.Value.Split('\\');
            var index = Array.IndexOf(hintPathSplit, "lib");
            var packageIndex = index - 1;

            var package = hintPathSplit[packageIndex];
            var packageSplit = package.Split('.');

            var packageName = new List<string>();
            foreach (var str in packageSplit)
            {
                int n;
                if (!int.TryParse(str, out n))
                {
                    packageName.Add(str);
                }
                else
                {
                    break;
                }
            }

            return string.Join(".", packageName);
        }

        private bool IsWebApplication(XElement element)
        {
            var webAppTarget = element.Elements(Constants.Elements.Import)
                                    .Where(el => el.Attribute(Constants.Attributes.Project).Value.Contains(Constants.WebApplicationTarget))
                                    .SingleOrDefault();
            return webAppTarget != null;
        }
    }    
}