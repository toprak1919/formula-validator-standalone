namespace FormulaValidator.Models
{
    public class ValidationRequest
    {
        public string Formula { get; set; } = string.Empty;
        public List<MeasuredValue> MeasuredValues { get; set; } = new();
        public List<Constant> Constants { get; set; } = new();
    }

    public class MeasuredValue
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double? Value { get; set; }
        public List<double>? Values { get; set; }
        // Optional unit (e.g., "meter", "au"). If null/empty, value is treated as unitless
        public string? Unit { get; set; }
    }

    public class Constant
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public double? Result { get; set; }
        public string? EvaluatedFormula { get; set; }
        public string Source { get; set; } = "Backend";
    }
}
