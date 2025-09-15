using FormulaValidator.Models;
using FormulaValidator.Services;

namespace FormulaValidator.GraphQL
{
    public class Query
    {
        private readonly IFormulaValidationService _validationService;

        public Query(IFormulaValidationService validationService)
        {
            _validationService = validationService;
        }

        public string GetHello() => "Formula Validator API with NCalc + UnitsNet!";

        public ValidationResult ValidateFormula(ValidationRequest request)
        {
            return _validationService.ValidateFormula(request);
        }
    }
}
