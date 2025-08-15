using FormulaValidatorAPI.Models;
using FormulaValidatorAPI.Services;

namespace FormulaValidatorAPI.GraphQL
{
    public class Query
    {
        private readonly IFormulaValidationService _validationService;

        public Query(IFormulaValidationService validationService)
        {
            _validationService = validationService;
        }

        public string GetHello() => "Hello from GraphQL!";

        public ValidationResult ValidateFormula(ValidationRequest request)
        {
            return _validationService.ValidateFormula(request);
        }
    }
}