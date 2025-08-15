using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using FormulaValidatorAPI.Models;
using FormulaValidatorAPI.Services;
using FormulaValidatorAPI.Tests.Models;

namespace FormulaValidatorAPI.Tests
{
    public class FormulaValidationServiceTests
    {
        private readonly IFormulaValidationService _validationService;
        private readonly ITestOutputHelper _output;
        private readonly TestCaseData _testCaseData;

        public FormulaValidationServiceTests(ITestOutputHelper output)
        {
            _validationService = new FormulaValidationService();
            _output = output;
            _testCaseData = LoadTestCases();
        }

        private TestCaseData LoadTestCases()
        {
            try
            {
                // Try to load from the centralized test-cases.json file
                var testCasesPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..", "..", "..", "..", "..",
                    "test-cases.json"
                );

                if (File.Exists(testCasesPath))
                {
                    var json = File.ReadAllText(testCasesPath);
                    var data = JsonConvert.DeserializeObject<TestCaseData>(json);
                    _output.WriteLine($"Loaded {data?.TestCases?.Count ?? 0} test cases from centralized file");
                    return data ?? new TestCaseData();
                }
                else
                {
                    _output.WriteLine($"Test cases file not found at: {testCasesPath}");
                    return GetFallbackTestCases();
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to load test cases: {ex.Message}");
                return GetFallbackTestCases();
            }
        }

        private TestCaseData GetFallbackTestCases()
        {
            return new TestCaseData
            {
                TestCases = new List<TestCase>
                {
                    new TestCase { Id = "basic-add", Formula = "1 + 1", ExpectError = false, Description = "Basic addition" },
                    new TestCase { Id = "missing-op", Formula = "1 2", ExpectError = true, Description = "Missing operator" }
                },
                TestData = new TestDataSet
                {
                    MeasuredValues = new Dictionary<string, TestVariable>
                    {
                        ["$measuredValue_1"] = new TestVariable { Id = "measuredValue_1", Name = "$measuredValue_1", Value = 10 }
                    },
                    Constants = new Dictionary<string, TestConstant>
                    {
                        ["#constantsPi"] = new TestConstant { Id = "constantsPi", Name = "#constantsPi", Value = 3.14159 }
                    }
                }
            };
        }

        [Fact]
        public void TestCasesLoaded()
        {
            Assert.NotNull(_testCaseData);
            Assert.NotEmpty(_testCaseData.TestCases);
            _output.WriteLine($"Test suite contains {_testCaseData.TestCases.Count} test cases");
        }

        [Theory]
        [MemberData(nameof(GetValidTestCases))]
        public void ValidFormulas_ShouldValidateSuccessfully(TestCase testCase)
        {
            // Arrange
            var request = CreateValidationRequest(testCase);

            // Act
            var result = _validationService.ValidateFormula(request);

            // Assert
            Assert.True(result.IsValid, $"Formula '{testCase.Formula}' should be valid but got error: {result.Error}");
            Assert.Null(result.Error);
            _output.WriteLine($"✓ {testCase.Id}: {testCase.Description} - Valid as expected");
        }

        [Theory]
        [MemberData(nameof(GetInvalidTestCases))]
        public void InvalidFormulas_ShouldFailValidation(TestCase testCase)
        {
            // Arrange
            var request = CreateValidationRequest(testCase);

            // Act
            var result = _validationService.ValidateFormula(request);

            // Assert
            Assert.False(result.IsValid, $"Formula '{testCase.Formula}' should be invalid but validated successfully");
            Assert.NotNull(result.Error);
            _output.WriteLine($"✓ {testCase.Id}: {testCase.Description} - Invalid as expected. Error: {result.Error}");
        }

        [Fact]
        public void RunAllTestCases_GenerateReport()
        {
            var results = new List<(TestCase testCase, ValidationResult result, bool passed)>();

            foreach (var testCase in _testCaseData.TestCases)
            {
                var request = CreateValidationRequest(testCase);
                var result = _validationService.ValidateFormula(request);
                
                bool passed = testCase.ExpectError 
                    ? !result.IsValid 
                    : result.IsValid;

                results.Add((testCase, result, passed));
            }

            // Generate report
            var totalTests = results.Count;
            var passedTests = results.Count(r => r.passed);
            var failedTests = totalTests - passedTests;
            var passRate = (double)passedTests / totalTests * 100;

            _output.WriteLine("\n========== Test Report ==========");
            _output.WriteLine($"Total Tests: {totalTests}");
            _output.WriteLine($"Passed: {passedTests}");
            _output.WriteLine($"Failed: {failedTests}");
            _output.WriteLine($"Pass Rate: {passRate:F1}%");

            if (failedTests > 0)
            {
                _output.WriteLine("\n========== Failed Tests ==========");
                foreach (var (testCase, result, passed) in results.Where(r => !r.passed))
                {
                    _output.WriteLine($"✗ {testCase.Id}: {testCase.Description}");
                    _output.WriteLine($"  Formula: {testCase.Formula}");
                    _output.WriteLine($"  Expected Error: {testCase.ExpectError}");
                    _output.WriteLine($"  Got Valid: {result.IsValid}");
                    _output.WriteLine($"  Error: {result.Error ?? "none"}");
                }
            }

            // Group by category
            var byCategory = results.GroupBy(r => r.testCase.Category);
            _output.WriteLine("\n========== Results by Category ==========");
            foreach (var category in byCategory)
            {
                var catPassed = category.Count(r => r.passed);
                var catTotal = category.Count();
                var catRate = (double)catPassed / catTotal * 100;
                _output.WriteLine($"{category.Key}: {catPassed}/{catTotal} ({catRate:F1}%)");
            }

            Assert.True(passedTests == totalTests, $"Not all tests passed. {failedTests} out of {totalTests} tests failed.");
        }

        private ValidationRequest CreateValidationRequest(TestCase testCase)
        {
            var request = new ValidationRequest
            {
                Formula = testCase.Formula,
                MeasuredValues = new List<MeasuredValue>(),
                Constants = new List<Constant>()
            };

            // Add all test data measured values
            foreach (var mv in _testCaseData.TestData.MeasuredValues.Values)
            {
                request.MeasuredValues.Add(new MeasuredValue
                {
                    Id = mv.Id,
                    Name = mv.Name,
                    Value = mv.Value
                });
            }

            // Add all test data constants
            foreach (var c in _testCaseData.TestData.Constants.Values)
            {
                request.Constants.Add(new Constant
                {
                    Id = c.Id,
                    Name = c.Name,
                    Value = c.Value
                });
            }

            return request;
        }

        public static IEnumerable<object[]> GetValidTestCases()
        {
            var tests = new FormulaValidationServiceTests(new TestOutputHelperStub());
            return tests._testCaseData.TestCases
                .Where(tc => !tc.ExpectError)
                .Select(tc => new object[] { tc });
        }

        public static IEnumerable<object[]> GetInvalidTestCases()
        {
            var tests = new FormulaValidationServiceTests(new TestOutputHelperStub());
            return tests._testCaseData.TestCases
                .Where(tc => tc.ExpectError)
                .Select(tc => new object[] { tc });
        }

        // Stub for static data methods
        private class TestOutputHelperStub : ITestOutputHelper
        {
            public void WriteLine(string message) { }
            public void WriteLine(string format, params object[] args) { }
        }
    }
}