using FormulaValidator.Models;
using FormulaValidator.Services;

namespace FormulaValidator.GraphQL
{
    public class Query
    {
        private readonly IFormulaValidationService _validationService;
        private readonly IConstantRepository _constantRepository;

        public Query(IFormulaValidationService validationService, IConstantRepository constantRepository)
        {
            _validationService = validationService;
            _constantRepository = constantRepository;
        }

        public string GetHello() => "Formula Validator API with mXparser!";

        public ValidationResult ValidateFormula(ValidationRequest request)
        {
            return _validationService.ValidateFormula(request);
        }

        public IEnumerable<Constant> GetConstants()
        {
            return _constantRepository.GetAll();
        }
    }
}
