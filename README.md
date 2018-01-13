# vs-project-converter
This tool will convert legacy Visual Studio projects and solutions to the format expected in Visual Studio 2017 in order to migrate
to .NET Core.

My goal was to make the magic button for this great post (seriously, read it):
http://www.natemcmaster.com/blog/2017/03/09/vs2015-to-vs2017-upgrade/

This post describes the changes, https://docs.microsoft.com/en-us/dotnet/core/tools/csproj, however most of the conversion was written based on my own trial and error with projects in which I have experience.

# Important Warning
This tool _is not perfect_ and I am sure there are missing pieces. It _will_ perform potentially destructive operations on your project files. Backup your files!

# Usage
To use, clone the repo and build the application.

To build
```powershell
dotnet build -f netcoreapp2.0
```

To run, a filename is required. This should be either a Visual Studio Solution (sln) or a Visual Studio Project (csproj) file. Pass the file name like so:

```powershell
dotnet run convert C:\testProject.csproj
```

```powershell
dotnet run convert C:\testSolution.sln
```

Preview mode will output the project files with `.converted` appended to the end of the filename in the location of the project file.
Convert will overwrite the project files passed in.

```powershell
dotnet run preview C:\testSolution.sln
```

## Notes
This was primarily run against solutions and projects from VS2015 which were using VS2017 build tools. This is provided as is; I am sure there are things I missed in my testing.

More importantly, this will *NOT* do the work of upgrading a solution to .NET Core. But what it does do, is enable your app to more easily be migrated
to .NET Core. Below are common issues I have experienced which are not handled by this (yet?):
* Duplicate assembly attributes
  * Due to assembly attributes being include as part of the project, rather than being specified in AssemblyInfo.cs files as was done previously.
* "Detected package downgrade"
  * This appears to be due to transitive package references. For exmaple, the below would generate this error because of project references and package references which use different versions.
    ```
    solution
        - project1.csproj (references Common.Logging 3.3.1)
        - project2.csproj (references project1 AND Common.Logging 3.4.0)
    ```