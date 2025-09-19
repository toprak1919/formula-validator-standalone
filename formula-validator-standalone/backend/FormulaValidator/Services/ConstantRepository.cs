using System.Collections.Generic;
using FormulaValidator.Models;
using Microsoft.Extensions.Configuration;

namespace FormulaValidator.Services
{
    public interface IConstantRepository
    {
        IReadOnlyCollection<Constant> GetAll();
    }

    public sealed class ConfigConstantRepository : IConstantRepository
    {
        private readonly IReadOnlyCollection<Constant> _constants;

        public ConfigConstantRepository(IConfiguration configuration)
        {
            var items = configuration.GetSection("PredefinedConstants").Get<List<ConstantConfig>>() ?? new();

            var list = new List<Constant>(items.Count);
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                var id = NormalizeId(item.Id);
                list.Add(new Constant
                {
                    Id = id,
                    Name = item.Name ?? id,
                    Value = item.Value
                });
            }

            _constants = list;
        }

        public IReadOnlyCollection<Constant> GetAll() => _constants;

        private static string NormalizeId(string id)
        {
            var trimmed = id.Trim();
            return trimmed.StartsWith("#", StringComparison.Ordinal) ? trimmed : "#" + trimmed;
        }

        private sealed class ConstantConfig
        {
            public string Id { get; set; } = string.Empty;
            public string? Name { get; set; }
            public double Value { get; set; }
        }
    }
}
