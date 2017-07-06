﻿using Cogs.Dto;
using Cogs.Model;
using Cogs.Publishers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Cogs.Tests
{
    public class JsonSchemaTests
    {
        [Fact]
        public async System.Threading.Tasks.Task asyncJsonSchemaTestAsync()
        {
            string path = @"C:\Users\clement\Documents\GitHub\cogs\cogsburger";

            string subdir = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            string outputPath = Path.Combine(Path.GetTempPath(), subdir);

            var directoryReader = new CogsDirectoryReader();
            var cogsDtoModel = directoryReader.Load(path);

            var modelBuilder = new CogsModelBuilder();
            var cogsModel = modelBuilder.Build(cogsDtoModel);

            var jsonPublisher = new JsonPublisher();
            jsonPublisher.TargetDirectory = outputPath;
            jsonPublisher.Publish(cogsModel);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            };
            var schemaData = File.ReadAllText(Path.Combine(outputPath, "jsonSchema" + ".json"));
            var schema = await JsonSchema4.FromJsonAsync(schemaData);

            var jsondata1 = File.ReadAllText(@"C:\Users\clement\Documents\GitHub\cogs\Cogs.Tests.Console\testing1_reference_reusable.json");
            var jsondata2 = File.ReadAllText(@"C:\Users\clement\Documents\GitHub\cogs\Cogs.Tests.Console\testing2_reference_Object.json");
            var jsondata3 = File.ReadAllText(@"C:\Users\clement\Documents\GitHub\cogs\Cogs.Tests.Console\test3_SimpleType.json");
            var jsondata4 = File.ReadAllText(@"C:\Users\clement\Documents\GitHub\cogs\Cogs.Tests.Console\test4_invalid_json.json");


            var validate1 = schema.Validate(jsondata1);
            var validate2 = schema.Validate(jsondata2);
            var validate3 = schema.Validate(jsondata3);
            var validate4 = schema.Validate(jsondata4);

            Assert.Empty(validate1);
            Assert.Empty(validate2);
            Assert.Empty(validate3);
            Assert.NotEmpty(validate4);
        }
    }
}
