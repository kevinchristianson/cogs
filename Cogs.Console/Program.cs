﻿// Copyright (c) 2017 Colectica. All rights reserved
// See the LICENSE file in the project root for more information.
using Cogs.Common;
using Cogs.Dto;
using Cogs.Model;
using Cogs.Publishers;
using Cogs.Publishers.Csharp;
using Cogs.Validation;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Cogs.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine(cogsLogo);

            var app = new CommandLineApplication
            {
                Name = "Cogs"
            };
            app.HelpOption("-?|-h|--help");


            app.Command("validate", (command) =>
            {

                command.Description = "Validate a on disk COGS data model directory";
                command.HelpOption("-?|-h|--help");

                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");                                

                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;

                    // read cogs directory and validate the contents
                    var directoryReader = new CogsDirectoryReader();                    
                    var cogsDtoModel = directoryReader.Load(location);
                    HandleErrors(directoryReader.Errors);
                    HandleErrors(DtoValidation.Validate(cogsDtoModel));

                    // this could find gaps in the validation, but idealy return nothing
                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);
                    HandleErrors(modelBuilder.Errors);

                    return 0;
                });

            });

            app.Command("publish-xsd", (command) =>
            {

                command.Description = "Publish an XML schema from a COGS data model";
                command.HelpOption("-?|-h|--help");

                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the xsd schema is generated.");

                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);

                var namespaceUri = command.Option("-n|--namespace",
                                           "URI of the target XML namespace",
                                           CommandOptionType.SingleValue);

                var namespaceUriPrefix = command.Option("-p|--namespacePrefix",
                                           "Namespace prefix to use for the target XML namespace",
                                           CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();

                    // read cogs directory and validate the contents
                    var directoryReader = new CogsDirectoryReader();                    
                    var cogsDtoModel = directoryReader.Load(location);
                    HandleErrors(directoryReader.Errors);
                    HandleErrors(DtoValidation.Validate(cogsDtoModel));

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);
                    HandleErrors(modelBuilder.Errors);

                    var targetNamespace = namespaceUri.Value() ?? cogsModel.Settings.NamespaceUrl;
                    var prefix = namespaceUriPrefix.Value() ?? cogsModel.Settings.NamespacePrefix;
                    try
                    {
                        XmlConvert.VerifyName(prefix);
                    }
                    catch(XmlException xmlEx)
                    {
                        CogsError xmlPrefixError = new CogsError(ErrorLevel.Error, "Invalid xml prefix string", xmlEx);
                        HandleErrors(new List<CogsError>() { xmlPrefixError });
                    }

                    XmlSchemaPublisher publisher = new XmlSchemaPublisher
                    {
                        CogsLocation = location,
                        TargetDirectory = target,
                        Overwrite = overwrite,
                        TargetNamespace = targetNamespace,
                        TargetNamespacePrefix = prefix
                    };

                    publisher.Publish(cogsModel);
                    HandleErrors(publisher.Errors);


                    return 0;
                });

            });


            app.Command("publish-uml", (command) =>
            {

                command.Description = "Publish an UML schema from a COGS data model";
                command.HelpOption("-?|-h|--help");

                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the UML schema is generated.");

                var dotOption = command.Option("-l|--location", "Directory where the dot.exe file is located. " +
                                            "Only used if not using normative and not necessary for windows.", CommandOptionType.SingleValue);
                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);
                var normativeOption = command.Option("-n|--normative",
                                           "Output a normative xmi file (2.4.2) instead of xmi 2.5.1. Note: cannot contain a graph element",
                                           CommandOptionType.NoValue);

                command.OnExecute(() =>
                {
                    var dot = dotOption.Value() ?? null;
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();
                    bool normative = normativeOption.HasValue();

                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);

                    UmlSchemaPublisher publisher = new UmlSchemaPublisher
                    {
                        DotLocation = dot,
                        TargetDirectory = target,
                        Overwrite = overwrite,
                        Normative = normative
                    };
                    publisher.Publish(cogsModel);


                    return 0;
                });

            });

            app.Command("publish-dot", (command) =>
            {

                command.Description = "Publish a dot schema from a COGS data model";
                command.HelpOption("-?|-h|--help");


                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the dot schema is generated.");

                var dotOption = command.Option("-l|--location", "Directory where the dot.exe file is located. " +
                                            "Only used if not using normative and not necessary for windows.", CommandOptionType.SingleValue);
                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);
                var outputFormat = command.Option("-f|--format", "Specifies format for output file. Defaults to svg", CommandOptionType.SingleValue);
                var outputAll = command.Option("-a|--all",
                                           "generate one graph containing all objects. Cannot be used with -s",
                                           CommandOptionType.NoValue);
                var outputSingle = command.Option("-s|--single",
                                           "generate a graph for every single item (incoming links and outgoing links). Cannot be used with -a",
                                           CommandOptionType.NoValue);
                var inheritanceArgument = command.Option("-i|--inheritance",
                                            "allow inheritance in the graph(s)", CommandOptionType.NoValue);
                var reusableArgument = command.Option("-c|--composite", "show composite types inside item types", CommandOptionType.NoValue);

                command.OnExecute(() =>
                {
                    var dot = dotOption.Value() ?? null;
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();
                    string format = outputFormat.Value() ?? "svg";
                    bool all = outputAll.HasValue();
                    bool single = outputSingle.HasValue();
                    if (all && single) throw new ArgumentException();
                    string output = "topic";
                    if (all) output = "all";
                    else if (single) output = "single";
                    bool reusables = reusableArgument.HasValue();
                    bool inheritance = inheritanceArgument.HasValue();

                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);

                    DotSchemaPublisher publisher = new DotSchemaPublisher
                    {
                        DotLocation = dot,
                        TargetDirectory = target,
                        Overwrite = overwrite,
                        Format = format,
                        Output = output,
                        Inheritance = inheritance,
                        ShowReusables = reusables
                    };
                    publisher.Publish(cogsModel);
                    HandleErrors(publisher.Errors);
                    return 0;
                });

            });

            app.Command("publish-cs", (command) =>
            {

                command.Description = "Publish a c# class structure from a COGS data model";
                command.HelpOption("-?|-h|--help");


                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the c# schema is generated.");

                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);
                var namespaceUri = command.Option("-n|--namespace",
                                           "URI of the target XML namespace",
                                           CommandOptionType.SingleValue);

                var namespaceUriPrefix = command.Option("-p|--prefix",
                                           "Namespace prefix to use for the target XML namespace",
                                           CommandOptionType.SingleValue);
                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();

                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);

                    var targetNamespace = namespaceUri.Value() ?? cogsModel.Settings.NamespaceUrl;
                    var prefix = namespaceUriPrefix.Value() ?? cogsModel.Settings.NamespacePrefix;
                    try
                    {
                        XmlConvert.VerifyName(prefix);
                    }
                    catch (XmlException xmlEx)
                    {
                        CogsError xmlPrefixError = new CogsError(ErrorLevel.Error, "Invalid xml prefix string", xmlEx);
                        HandleErrors(new List<CogsError>() { xmlPrefixError });
                    }

                    CsSchemaPublisher publisher = new CsSchemaPublisher
                    {
                        TargetDirectory = target,
                        TargetNamespace = targetNamespace,
                        TargetNamespacePrefix = prefix,
                        Overwrite = overwrite
                    };
                    publisher.Publish(cogsModel);

                    return 0;
                });

            });


            app.Command("publish-sphinx", (command) =>
            {

                command.Description = "Publish a Sphinx documentation website from a COGS data model";
                command.HelpOption("-?|-h|--help");

                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the sphinx documentation is generated.");

                var dotOption = command.Option("-l|--location", "Directory where the dot.exe file is located. " +
                                            "Only used if not using normative and not necessary for windows.", CommandOptionType.SingleValue);
                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);



                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    var dot = dotOption.Value() ?? null;
                    bool overwrite = overwriteOption.HasValue();

                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);

                    SphinxPublisher publisher = new SphinxPublisher
                    {
                        TargetDirectory = target,
                        Overwrite = overwrite,
                        DotLocation = dot
                    };

                    publisher.Publish(cogsModel);
                    return 0;
                });

            });

            app.Command("publish-json", (command) =>
            {

                command.Description = "Publish a JSON schema from a COGS data model";
                command.HelpOption("-?|-h|--help");

                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the json schema is generated.");

                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);
                var additionalprop = command.Option("-a|--allowAdditionalProperties",
                                            "Allow additional Properties to be added when enabled", CommandOptionType.NoValue);



                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();
                    bool addprop = additionalprop.HasValue();


                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);

                    JsonPublisher publisher = new JsonPublisher
                    {
                        CogsLocation = location,
                        TargetDirectory = target,
                        Overwrite = overwrite,
                        AdditionalProp = addprop
                    };

                    publisher.Publish(cogsModel);


                    return 0;
                });

            });

            app.Command("publish-GraphQL", (command) =>
            {

                command.Description = "Publish a GraphQL schema from a COGS data model";
                command.HelpOption("-?|-h|--help");

                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the json schema is generated.");

                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);



                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();


                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);

                    GraphQLPublisher publisher = new GraphQLPublisher
                    {
                        CogsLocation = location,
                        TargetDirectory = target,
                        Overwrite = overwrite
                    };

                    publisher.Publish(cogsModel);

                    return 0;
                });

            });

            app.Command("publish-owl", (command) =>
            {

                command.Description = "Publish an OWL/RDF schema from a COGS data model";
                command.HelpOption("-?|-h|--help");

                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the owl schema is generated.");

                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);

                var namespaceUri = command.Option("-n|--namespace",
                                           "URI of the target Owl namespace",
                                           CommandOptionType.SingleValue);

                var namespaceUriPrefix = command.Option("-p|--namespacePrefix",
                                           "Namespace prefix to use for the target Owl namespace",
                                           CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();

                    // read cogs directory and validate the contents
                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);
                    HandleErrors(directoryReader.Errors);
                    HandleErrors(DtoValidation.Validate(cogsDtoModel));

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);
                    HandleErrors(modelBuilder.Errors);

                    var targetNamespace = namespaceUri.Value() ?? cogsModel.Settings.NamespaceUrl;
                    var prefix = namespaceUriPrefix.Value() ?? cogsModel.Settings.NamespacePrefix;

                    try
                    {
                        XmlConvert.VerifyName(prefix);
                    }
                    catch (XmlException xmlEx)
                    {
                        CogsError xmlPrefixError = new CogsError(ErrorLevel.Error, "Invalid xml prefix string", xmlEx);
                        HandleErrors(new List<CogsError>() { xmlPrefixError });
                    }

                    OwlPublisher publisher = new OwlPublisher
                    {
                        CogsLocation = location,
                        TargetDirectory = target,
                        Overwrite = overwrite,
                        TargetNamespace = targetNamespace,
                        TargetNamespacePrefix = prefix
                    };

                    publisher.Publish(cogsModel);
                    //HandleErrors(publisher.Errors);


                    return 0;
                });

            });

            app.Command("cogs-new", (command) =>
            {
                command.Description = "create a model skeleton";
                command.HelpOption("-?|-h|--help");
                
                var locationArgument = command.Argument("[cogsLocation]", "Directory where the COGS datamodel is located.");
                var targetArgument = command.Argument("[targetLocation]", "Directory where the model skelton is generated.");

                var overwriteOption = command.Option("-o|--overwrite",
                                           "If the target directory exists, delete and overwrite the location",
                                           CommandOptionType.NoValue);



                command.OnExecute(() =>
                {
                    var location = locationArgument.Value ?? Environment.CurrentDirectory;
                    var target = targetArgument.Value ?? Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool overwrite = overwriteOption.HasValue();

                    var directoryReader = new CogsDirectoryReader();
                    var cogsDtoModel = directoryReader.Load(location);

                    var modelBuilder = new CogsModelBuilder();
                    var cogsModel = modelBuilder.Build(cogsDtoModel);

                    ModelInitializer cogsmodel = new ModelInitializer
                    {
                        Dir = location,
                        Overwrite = overwrite
                    };

                    cogsmodel.Create();


                    return 0;
                });

            });

            app.OnExecute(() =>
            {
                System.Console.WriteLine("Cogs");
                return 0;
            });


            var result = app.Execute(args);
            Environment.Exit(result);
        }

        private static void HandleErrors(List<CogsError> errors)
        {
            foreach(var error in errors)
            {
                System.Console.Error.Write(Enum.GetName(typeof(ErrorLevel), error.Level) + ": ");
                if(error.Level == ErrorLevel.Message)
                {
                    System.Console.WriteLine(error.Message);
                }
                else
                {
                    System.Console.Error.WriteLine(error.Message);
                }
                             
            }
            if(errors.Any(x => x.Level == ErrorLevel.Error))
            {
                Environment.Exit(100);
            }
        }

        private static string cogsLogo = 
@"  ______   ______     _______      _______.
 /      | /  __  \   /  _____|    /       |
|  ,----'|  |  |  | |  |  __     |   (----`
|  |     |  |  |  | |  | |_ |     \   \    
|  `----.|  `--'  | |  |__| | .----)   |   
 \______| \______/   \______| |_______/";
    }
}
