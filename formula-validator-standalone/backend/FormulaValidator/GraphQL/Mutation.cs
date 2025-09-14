using FormulaValidator.Models;
using FormulaValidator.Services;

namespace FormulaValidator.GraphQL
{
    public class Mutation
    {
        private readonly IFormulaValidationService _validationService;

        public Mutation(IFormulaValidationService validationService)
        {
            _validationService = validationService;
        }

        public ValidationResult ValidateFormula(ValidationRequest request)
        {
            return _validationService.ValidateFormula(request);
        }
    }
}