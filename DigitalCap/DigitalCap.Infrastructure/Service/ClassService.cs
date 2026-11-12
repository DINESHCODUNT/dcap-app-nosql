using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepository;

        public ClassService(IClassRepository classRepository)
        {
            _classRepository = classRepository ?? throw new ArgumentNullException(nameof(classRepository));
        }

        public async Task<IEnumerable<ClassModel>> Search()
        {
            return await _classRepository.Search();
        }

        public async Task<string[]> MapFromName(string className1, string className2)
        {
            var results = await Search();

            var classAbbreviations =
                from classification in results
                where classification.Name == className1 || classification.Name == className2
                select classification.Abbreviation;

            return classAbbreviations.ToArray();
        }
    }
}
