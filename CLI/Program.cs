﻿using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Nebula.Parser;
using McMaster.Extensions.CommandLineUtils;
using Nebula.Services;
using System.Linq;
using Nebula.Generators;
using Microsoft.Extensions.Configuration;

namespace Nebula
{
    public static class NebulaConfig
    {
        public static string TemplateManifestRepo { get; set; }
    }
    
    [Command(Name = "nebula", Description = "REST API Client Library Generator"),
        Subcommand("new", typeof(NewProject)), 
        Subcommand("build", typeof(BuildProject)), 
        Subcommand("template", typeof(TemplateOptions)),
        Subcommand("generate", typeof(GenerateOptions))
    ]
    class Nebula
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            
            NebulaConfig.TemplateManifestRepo = configuration.GetSection("TemplateManifest").Value;
            
            CommandLineApplication.Execute<Nebula>(args);
        }

        private int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify a subcommand.");
            app.ShowHelp();
            return 1;
        }

        [Command("new", Description = "Create a new project")]
        private class NewProject
        {
            [Required(ErrorMessage = "You must specify the project name")]
            [Argument(0, Description = "The name for the new project")]
            public string Name { get; }
            private int OnExecute(IConsole console)
            {
                console.WriteLine("Creating project: " + Name);
                var ps = new ProjectService();
                ps.CreateProject(Name, Environment.CurrentDirectory);
                return 0;
            }
        }

        [Command("build", Description = "Build the client libraries")]
        private class BuildProject
        {
            private int OnExecute(IConsole console)
            {
                var ps = new ProjectService();
                try
                {
                    var project = ps.LoadProject(Environment.CurrentDirectory);
                    console.WriteLine($"Building {project.Name}...");
                    ps.BuildProject(project);
                    console.WriteLine("Build completed successfully.");
                    return 0;
                }
                catch (Exception e)
                {
                    console.Error.WriteLine(e.Message);
                    console.Error.WriteLine(e.StackTrace);
                    return 1;
                }
            }
        }

        [Command("generate", Description = "Tools for generating entities"),
            Subcommand("entity", typeof(GenerateEntityOption))]
        private class GenerateOptions
        {
            private int OnExecute(IConsole console)
            {
                return 0;
            }

            [Command("entity", Description = "Generate entities from provided JSON")]
            private class GenerateEntityOption
            {
                [Required(ErrorMessage = "You must specify the source data")]
                [Argument(0, Description = "JSON content from which to generate entities")]
                public string Data { get; }
                private int OnExecute(IConsole console)
                {
                    var ps = new ProjectService();
                    try
                    {
                        var project = ps.LoadProject(Environment.CurrentDirectory);
                        var generator = new EntityGenerator(project, Data);
                        generator.GenerateEntityFromJSON();
                        return 0;
                    }
                    catch (Exception e)
                    {
                        console.Error.WriteLine(e.Message);
                        console.Error.WriteLine(e.StackTrace);
                        return 1;
                    }
                }
            }
        }

        [Command("template", Description = "Commands to manage library templates"),
            Subcommand("update", typeof(TemplateUpdateOption)),
            Subcommand("list", typeof(TemplateListOption)),
            Subcommand("add", typeof(TemplateAddOption)),
            Subcommand("remove", typeof(TemplateRemoveOption))
        ]
        private class TemplateOptions
        {
            private int OnExecute(IConsole console)
            {
                return 0;
            }

            [Command("update", Description = "Update template manifest")]
            private class TemplateUpdateOption
            {
                private int OnExecute(IConsole console)
                {
                    console.WriteLine("Updating templates");
                    try
                    {
                        var ps = new ProjectService();
                        var project = ps.LoadProject(Environment.CurrentDirectory);
                        var ts = new TemplateService(project, NebulaConfig.TemplateManifestRepo);
                        
                        ts.GetOrUpdateManifest();
                        return 0;
                    }
                    catch (Exception e)
                    {
                        console.Error.WriteLine(e.Message);
                        return 1;
                    }
                    
                }
            }

            [Command("list", Description = "Get list of available templates")]
            private class TemplateListOption
            {
                private int OnExecute(IConsole console)
                {
                    try
                    {
                        var ps = new ProjectService();
                        var project = ps.LoadProject(Environment.CurrentDirectory);
                        var ts = new TemplateService(project, NebulaConfig.TemplateManifestRepo);
                        
                        var templates = ts.GetTemplates();
                        foreach (var t in templates)
                        {
                            console.WriteLine($"{t.Name}\t{t.Language}\t{t.Framework}\t{t.Version}");
                        }
                        return 0;
                    }
                    catch (Exception e)
                    {
                        console.Error.WriteLine(e.Message);
                        return 1;
                    }
                }
            }

            [Command("add", Description = "Adds the specified template to the project")]
            private class TemplateAddOption
            {
                [Required(ErrorMessage = "You must specify the template name")]
                [Argument(0, Description = "The name of the template to add")]
                public string Name { get; }
                private int OnExecute(IConsole console)
                {
                    try
                    {
                        var ps = new ProjectService();
                        var project = ps.LoadProject(Environment.CurrentDirectory);
                        var ts = new TemplateService(project, NebulaConfig.TemplateManifestRepo);
                        
                        var template = ts.GetTemplates().FirstOrDefault(t => t.Name == Name);
                        if (template != null)
                        {
                            if (ts.AddTemplateToProject(template))
                            {
                                ps.SaveProject(project);
                                return 0;
                            }

                            throw new Exception("Template is already added to this project.");
                        }

                        throw new Exception("Could not find template named: " + Name);
                        
                    }
                    catch (Exception e)
                    {
                        console.Error.WriteLine(e.Message);
                        return 1;
                    }
                }
            }

            [Command("remove", Description = "Removes the specified template from the project")]
            private class TemplateRemoveOption
            {
                [Required(ErrorMessage = "You must specify the template name")]
                [Argument(0, Description = "The name of the template to remove")]
                public string Name { get; }
                private int OnExecute(IConsole console)
                {
                    try
                    {
                        var ps = new ProjectService();
                        var project = ps.LoadProject(Environment.CurrentDirectory);
                        var ts = new TemplateService(project, NebulaConfig.TemplateManifestRepo);
                        
                        ts.RemoveTemplateFromProject(Name);
                        ps.SaveProject(project);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        console.Error.WriteLine(e.Message);
                        return 1;
                    }
                }
            }
        }
    }
}
