// Copyright (c) 2019 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Sharpmake;
using Sharpmake.Generators.JsonCompilationDatabase;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompileDataBaseCommand
{
    public class BaseProject : Project
    {
        protected BaseProject()
        {
            IsFileNameToLower = false;
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";
        }
    }

    public class BaseLibProject : BaseProject
    {
        protected BaseLibProject(string name)
        {
            Name = name + "ProjectName";
            AddTargets(new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug, OutputType.Lib));
            SourceRootPath = "[project.SharpmakeCsPath]/" + name;
        }

        [Configure]
        public void ConfigureLib(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.Lib;
            conf.IncludePaths.Add("[project.SourceRootPath]");

            conf.Options.Add(Options.Vc.Librarian.TreatLibWarningAsErrors.Enable);
        }
    }

    [Generate]
    public class LibHelloProject : BaseLibProject
    {
        public LibHelloProject() : base("LibHello")
        { }
    }

    [Generate]
    public class LibGoodbyeProject : BaseLibProject
    {
        public LibGoodbyeProject() : base("LibGoodbye")
        { }
    }

    [Generate]
    public class ExeProject : BaseProject
    {
        public ExeProject()
        {
            Name = "ExeProjectName";
            AddTargets(new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug));
            SourceRootPath = "[project.SharpmakeCsPath]/src";
        }

        [Configure]
        public void Configure(Configuration conf, Target target)
        {
            conf.Options.Add(Options.Vc.Linker.TreatLinkerWarningAsErrors.Enable);

            conf.AddPrivateDependency<LibHelloProject>(target);
            conf.AddPrivateDependency<LibGoodbyeProject>(target);
        }
    }

    [Generate]
    public class MainSolution : Solution
    {
        public MainSolution()
        {
            Name = "CompileCommandDatabaseSolution";
            AddTargets(new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug));

            IsFileNameToLower = false;
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<ExeProject>(target);
        }
    }

    public static class main
    {
        [Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            // Generally you should only generate either for projets or solution but this is a sample so we do both ;)
            arguments.Builder.EventPostProjectLink += GenerateProjectDatabase;
            arguments.Builder.EventPostSolutionLink += GenerateSolutionDatabase;

            arguments.Generate<MainSolution>();
        }

        private static void GenerateProjectDatabase(Project project)
        {
            var outdir = Path.GetDirectoryName(project.ProjectFilesMapping.First().Key);

            GenerateDatabase(outdir, project.Configurations, CompileCommandFormat.Arguments);
        }

        private static void GenerateSolutionDatabase(Solution solution)
        {
            var outdir = Path.GetDirectoryName(solution.SolutionFilesMapping.First().Key);

            var configs = solution.Configurations.SelectMany(c => c.IncludedProjectInfos.Select(pi => pi.Configuration));

            GenerateDatabase(outdir, configs, CompileCommandFormat.Command);
        }

        private static void GenerateDatabase(string outdir, IEnumerable<Project.Configuration> configs, CompileCommandFormat format)
        {
            var builder = Builder.Instance;

            if (builder == null)
            {
                System.Console.Error.WriteLine("CompilationDatabase: No builder instance.");
                return;
            }

            var generator = new JsonCompilationDatabase();

            generator.Generate(builder, outdir, configs, format, new List<string>(), new List<string>());
        }
    }
}
