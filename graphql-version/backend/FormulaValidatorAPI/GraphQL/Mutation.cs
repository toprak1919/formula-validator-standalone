using FormulaValidatorAPI.Models;
using FormulaValidatorAPI.Services;

namespace FormulaValidatorAPI.GraphQL
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