﻿using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.NET.Sdk.Publish.Tasks.Tests
{
    public class AppSettingsTransformTests
    {
        [Fact]
        public void GenerateDefaultAppSettingsJsonFile_CreatesCorrectDefaultFile()
        {
            // Act 
            string resultFile = AppSettingsTransform.GenerateDefaultAppSettingsJsonFile();

            // Assert
            Assert.True(File.Exists(resultFile));
            JToken defaultConnectionString = JObject.Parse(File.ReadAllText(resultFile))["ConnectionStrings"]["DefaultConnection"];
            Assert.Equal(defaultConnectionString.ToString(), string.Empty);
        }


        [Theory]
        [InlineData("DefaultConnection", @"Server=(localdb)\mssqllocaldb;Database=defaultDB;Trusted_Connection=True;MultipleActiveResultSets=true")]
        [InlineData("EmptyConnection", @"")]
        [InlineData("", @"SomeConnectionStringValue")]
        public void AppSettingsTransform_UpdatesSingleConnectionString(string connectionName, string connectionString)
        {
            // Arrange
            ITaskItem[] taskItemArray = new ITaskItem[1];
            TaskItem connectionstringTaskItem = new TaskItem(connectionName);
            connectionstringTaskItem.SetMetadata("Value", connectionString);
            taskItemArray[0] = connectionstringTaskItem;

            string appsettingsFile = AppSettingsTransform.GenerateDefaultAppSettingsJsonFile();

            // Act 
            AppSettingsTransform.UpdateDestinationConnectionStringEntries(appsettingsFile, taskItemArray);

            // Assert
            JToken connectionStringValue = JObject.Parse(File.ReadAllText(appsettingsFile))["ConnectionStrings"][connectionName];
            Assert.Equal(connectionStringValue.ToString(), connectionString);

            if (File.Exists(appsettingsFile))
            {
                File.Delete(appsettingsFile);
            }
        }

        private static readonly ITaskItem DefaultConnectionTaskItem = new TaskItem("DefaultConnection", new Dictionary<string, string>() { { "Value", @"Server=(localdb)\mssqllocaldb; Database=defaultDB;Trusted_Connection=True;MultipleActiveResultSets=true" } });
        private static readonly ITaskItem CarConnectionTaskItem = new TaskItem("CarConnection", new Dictionary<string, string>() { { "Value", @"Server=(localdb)\mssqllocaldb; Database=CarDB;Trusted_Connection=True;MultipleActiveResultSets=true" } });
        private static readonly ITaskItem PersonConnectionTaskItem = new TaskItem("PersonConnection", new Dictionary<string, string>() { { "Value", @"Server=(localdb)\mssqllocaldb; Database=PersonDb;Trusted_Connection=True;MultipleActiveResultSets=true" } });

        private static readonly List<object[]> testData = new List<object[]>
        {
            new object[] {new ITaskItem[] { DefaultConnectionTaskItem } },
            new object[] {new ITaskItem[] { DefaultConnectionTaskItem, CarConnectionTaskItem, PersonConnectionTaskItem } }
        };

        public static IEnumerable<object[]> ConnectionStringsData
        {
            get { return testData; }
        }

        [Theory]
        [MemberData(nameof(ConnectionStringsData), MemberType=typeof(AppSettingsTransformTests))]
        public void AppSettingsTransform_UpdatesMultipleConnectionStrings(ITaskItem[] values)
        {
            // Arrange
            string destinationAppSettingsFile = AppSettingsTransform.GenerateDefaultAppSettingsJsonFile();

            //Act
            AppSettingsTransform.UpdateDestinationConnectionStringEntries(destinationAppSettingsFile, values);

            // Assert
            foreach (var eachValue in values)
            {
                JToken connectionStringValue = JObject.Parse(File.ReadAllText(destinationAppSettingsFile))["ConnectionStrings"][eachValue.ItemSpec];
                Assert.Equal(connectionStringValue.ToString(), eachValue.GetMetadata("Value"));
            }

            if (File.Exists(destinationAppSettingsFile))
            {
                File.Delete(destinationAppSettingsFile);
            }
        }
    }
}
