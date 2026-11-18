using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosGradeRepository : CosmosRepositoryBase<ProjectGradeDocument>, IGradeRepository
    {
        private readonly ISequenceService _sequenceService;

        public CosmosGradeRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            ILogger<CosmosGradeRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_ProjectGrades, logger)
        {
            _sequenceService = sequenceService;
        }

        public void Commit() { }

        public async Task<ProjectGrades> GetProjectGrades(int projectId)
        {
            try
            {
                var pk = new PartitionKey((double)projectId);
                var query = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsDeleted = false")
                    .WithParameter("@projectId", projectId);

                var items = await QueryAsync(query, pk);
                if (items.Count == 0)
                {
                    return null!;
                }

                // QueryFirstOrDefault with no ORDER BY: take the lowest Id
                var doc = items.OrderBy(x => x.Id).First();

                return new ProjectGrades
                {
                    Id = doc.Id,
                    ProjectId = doc.ProjectId,
                    CAPCertificateNumber = doc.CAPCertificateNumber,
                    CAPCertificateIssuanceDate = doc.CAPCertificateIssuanceDate,
                    ClassReportNumber = doc.ClassReportNumber,
                    ClassReportDate = doc.ClassReportDate,
                    StructuralReportNumber = doc.StructuralReportNumber,
                    StructuralReportDate = doc.StructuralReportDate,
                    FinalGrade = doc.FinalGrade.HasValue ? (FinalGrades)doc.FinalGrade.Value : null,
                    StructuralGrade = doc.StructuralGrade.HasValue ? (Grades)doc.StructuralGrade.Value : null,
                    FatigueGrade = doc.FatigueGrade.HasValue ? (Grades)doc.FatigueGrade.Value : null,
                    RenewalGrade = doc.RenewalGrade.HasValue ? (Grades)doc.RenewalGrade.Value : null,
                    MaterialGrade = doc.MaterialGrade.HasValue ? (Grades)doc.MaterialGrade.Value : null,
                    GaugingGrade = doc.GaugingGrade.HasValue ? (Grades)doc.GaugingGrade.Value : null,
                    HullGirderStrength = doc.HullGirderStrength.HasValue ? (Grades)doc.HullGirderStrength.Value : null,
                    CAP1CertificateIssuanceDate = doc.CAP1CertificateIssuanceDate,
                    CAP1FinalGrade = doc.CAP1FinalGrade!
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving project grades for project ID {projectId}: {ex.Message}", ex);
            }
        }

        public async Task<int> CreateProjectGrades(int projectId)
        {
            try
            {
                var newId = await _sequenceService.GetNextAsync(CosmosContainers.CAP_ProjectGrades);

                var doc = new ProjectGradeDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    ProjectId = projectId,
                    IsDeleted = false
                };

                var pk = new PartitionKey((double)projectId);
                await CreateAsync(doc, pk);

                // SQL SP CAP.Create_Project_Grades sets @Output = scope_identity() without SELECT,
                // so QueryFirstOrDefaultAsync<int> in SQL repository always returns 0. Keep identical behavior.
                return 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creating project grades for project ID {projectId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateProjectGrades(ProjectGrades projectGrades)
        {
            if (projectGrades == null)
            {
                throw new ArgumentNullException(nameof(projectGrades));
            }

            try
            {
                var idString = projectGrades.Id.ToString();
                var findQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", idString);

                var items = await QueryAsync(findQuery);
                if (items.Count == 0)
                {
                    return;
                }

                var existingDoc = items[0];
                var cutCAP1 = projectGrades.CAP1FinalGrade != null && projectGrades.CAP1FinalGrade.Length > 4000
                    ? projectGrades.CAP1FinalGrade[..4000]
                    : projectGrades.CAP1FinalGrade;

                var updatedDoc = new ProjectGradeDocument
                {
                    id = existingDoc.id,
                    Id = projectGrades.Id,
                    ProjectId = projectGrades.ProjectId,
                    CAPCertificateNumber = projectGrades.CAPCertificateNumber,
                    CAPCertificateIssuanceDate = projectGrades.CAPCertificateIssuanceDate?.Date,
                    ClassReportNumber = projectGrades.ClassReportNumber,
                    ClassReportDate = projectGrades.ClassReportDate?.Date,
                    StructuralReportNumber = projectGrades.StructuralReportNumber,
                    StructuralReportDate = projectGrades.StructuralReportDate?.Date,
                    FinalGrade = projectGrades.FinalGrade.HasValue ? (int)projectGrades.FinalGrade.Value : null,
                    StructuralGrade = projectGrades.StructuralGrade.HasValue ? (int)projectGrades.StructuralGrade.Value : null,
                    FatigueGrade = projectGrades.FatigueGrade.HasValue ? (int)projectGrades.FatigueGrade.Value : null,
                    RenewalGrade = projectGrades.RenewalGrade.HasValue ? (int)projectGrades.RenewalGrade.Value : null,
                    MaterialGrade = projectGrades.MaterialGrade.HasValue ? (int)projectGrades.MaterialGrade.Value : null,
                    GaugingGrade = projectGrades.GaugingGrade.HasValue ? (int)projectGrades.GaugingGrade.Value : null,
                    HullGirderStrength = projectGrades.HullGirderStrength.HasValue ? (int)projectGrades.HullGirderStrength.Value : null,
                    CAP1CertificateIssuanceDate = projectGrades.CAP1CertificateIssuanceDate?.Date,
                    CAP1FinalGrade = cutCAP1,
                    IsDeleted = existingDoc.IsDeleted
                };

                if (projectGrades.ProjectId == existingDoc.ProjectId)
                {
                    await ReplaceAsync(updatedDoc, updatedDoc.id, new PartitionKey((double)projectGrades.ProjectId));
                }
                else
                {
                    // Partition key changed: create in new partition, delete from old
                    await CreateAsync(updatedDoc, new PartitionKey((double)projectGrades.ProjectId));
                    try
                    {
                        await DeleteAsync(existingDoc.id, new PartitionKey((double)existingDoc.ProjectId));
                    }
                    catch
                    {
                        try
                        {
                            await DeleteAsync(updatedDoc.id, new PartitionKey((double)updatedDoc.ProjectId));
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating project grades for project ID {projectGrades.ProjectId}: {ex.Message}", ex);
            }
        }
    }
}
