using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class GradeRepository : IGradeRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        
        public GradeRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        
        public void Commit()
        {
            _unitOfWork?.Commit();
        }
        
        public async Task<ProjectGrades> GetProjectGrades(int projectId)
        {
            // Capture connection at method start
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            // Ensure connection is open
            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            // Get transaction - ensure it exists
            var transaction = _unitOfWork?.Transaction;
            
            // If transaction is null or disposed, start a new one
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                
                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to start a new transaction for read operation.");
                }
            }

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<ProjectGrades>(
                    sql: "CAP.Read_Project_Grades",
                    param: new { ProjectId = projectId },
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction,
                    commandTimeout: 30);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving project grades for project ID {projectId}: {ex.Message}", ex);
            }
        }

        public async Task<int> CreateProjectGrades(int projectId)
        {
            // Capture connection and transaction
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            // Ensure connection is open
            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            // Get transaction - ensure it exists
            var transaction = _unitOfWork?.Transaction;
            
            // If transaction is null or disposed, start a new one
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                
                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to start a new transaction for write operation.");
                }
            }

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<int>(
                    sql: "CAP.Create_Project_Grades",
                    param: new { ProjectId = projectId },
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction,
                    commandTimeout: 30);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creating project grades for project ID {projectId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateProjectGrades(ProjectGrades projectGrades)
        {
            if (projectGrades == null)
                throw new ArgumentNullException(nameof(projectGrades));

            // Capture connection and transaction
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            // Ensure connection is open
            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            // Get transaction - ensure it exists
            var transaction = _unitOfWork?.Transaction;
            
            // If transaction is null or disposed, start a new one
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                
                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to start a new transaction for update operation.");
                }
            }

            try
            {
                await connection.ExecuteAsync(
                    sql: "CAP.Update_Project_Grades",
                    param: new
                    {
                        projectGrades.Id,
                        projectGrades.ProjectId,
                        projectGrades.CAPCertificateNumber,
                        projectGrades.CAPCertificateIssuanceDate,
                        projectGrades.ClassReportNumber,
                        projectGrades.ClassReportDate,
                        projectGrades.StructuralReportNumber,
                        projectGrades.StructuralReportDate,
                        projectGrades.FinalGrade,
                        projectGrades.StructuralGrade,
                        projectGrades.FatigueGrade,
                        projectGrades.RenewalGrade,
                        projectGrades.MaterialGrade,
                        projectGrades.GaugingGrade,
                        projectGrades.HullGirderStrength,
                        projectGrades.CAP1CertificateIssuanceDate,
                        projectGrades.CAP1FinalGrade
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction,
                    commandTimeout: 30
                );
                this.Commit();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating project grades for project ID {projectGrades.ProjectId}: {ex.Message}", ex);
            }
        }

    }
}
