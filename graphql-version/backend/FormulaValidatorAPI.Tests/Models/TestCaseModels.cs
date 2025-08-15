using Newtonsoft.Json;
using System.Collections.Generic;

namespace FormulaValidatorAPI.Tests.Models
{
    public class TestCaseData
    {
        [JsonProperty("testCases")]
        public List<TestCase> TestCases { get; set; } = new();

        [JsonProperty("testData")]
        public TestDataSet TestData { get; set; } = new();

        [JsonProperty("categories")]
        public List<TestCategory> Categories { get; set; } = new();
    }

    public class TestCase
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("formula")]
        public string Formula { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("expectError")]
        public bool ExpectError { get; set; }

        [JsonProperty("expectedResult")]
        public object? ExpectedResult { get; set; }

        [JsonProperty("expectedError")]
        public string? ExpectedError { get; set; }

        [JsonProperty("requiredVariables")]
        public List<string>? RequiredVariables { get; set; }

        [JsonProperty("requiredConstants")]
        public List<string>? RequiredConstants { get; set; }

        [JsonProperty("note")]
        public string? Note { get; set; }
    }

    public class TestDataSet
    {
        [JsonProperty("measuredValues")]
        public Dictionary<string, TestVariable> MeasuredValues { get; set; } = new();

        [JsonProperty("constants")]
        public Dictionary<string, TestConstant> Constants { get; set; } = new();
    }

    public class TestVariable
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class TestConstant
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class TestCategory
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string? Description { get; set; }
    }
}