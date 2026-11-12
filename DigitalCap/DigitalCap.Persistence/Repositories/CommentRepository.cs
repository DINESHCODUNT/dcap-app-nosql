using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Transactions;

namespace DigitalCap.Persistence.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        protected IDbConnection Connection => _unitOfWork.Connection;
        protected IDbTransaction Transaction => _unitOfWork.Transaction;

        public CommentRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<IEnumerable<CommentModel>>GetCommentsByProjectIdAsync(int projectId)
        {
            var result = await Connection.QueryAsync<CommentModel>(
                sql: "[CAP].[Read_Comments_By_Project_Id]",
                param: new { ProjectId = projectId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            var comments = result.Select(c =>
            {
                c.UserId ??= c.CreatedBy;
                c.CreatedBy ??= c.UserId;
                return c;
            }).ToList();

            return comments;
        }

        public async Task<IEnumerable<CommentModel>>GetCommentsByProjectTaskIdAsync(int projectTaskId)
        {
            var result = await Connection.QueryAsync<CommentModel>(
                sql: "[CAP].[Read_Comments_By_Task_Id]",
                param: new { ProjectTaskId = projectTaskId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
            var comments = result.Select(c =>
               {
                c.UserId ??= c.CreatedBy;
                c.CreatedBy ??= c.UserId;
                return c;
                }).ToList();

             return comments;
        }

        public async Task<IEnumerable<CommentModel>> CreateCommentAsync(int projectId,int? projectTaskId,
            string userId,string comment)
        {
            var result = await Connection.QueryAsync<CommentModel>(
                        "[CAP].[Create_Comment]",
            new
                {
                    ProjectId = projectId,
                    ProjectTaskId = projectTaskId,
                    CreatedBy = userId,
                    Comment = comment
                },
            commandType: CommandType.StoredProcedure,
            transaction: Transaction
            );
            this.Commit();
            if (projectTaskId.HasValue)
                return await GetCommentsByProjectTaskIdAsync(projectTaskId.Value);

            return await GetCommentsByProjectIdAsync(projectId);
            
        }

       

        public async Task<IEnumerable<CommentModel>> UpdateCommentAsync(int commentId,int projectId,int? projectTaskId,
            string comment,string user)
        {
           
            //  Execute update SP
            await Connection.ExecuteAsync(
                "[CAP].[Update_Comment]",
                new
                {
                    Id = commentId,
                    ProjectId = projectId,
                    Comment = comment,
                    UpdatedBy = user
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            // Re-fetch updated comments list
            IEnumerable<CommentModel> comments;

            if (projectTaskId.HasValue)
            {
                comments = await Connection.QueryAsync<CommentModel>(
                    "[CAP].[Read_Comments_By_Task_Id]",
                    new { ProjectTaskId = projectTaskId.Value },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
            }
            else
            {
                comments = await Connection.QueryAsync<CommentModel>(
                    "[CAP].[Read_Comments_By_Project_Id]",
                    new { ProjectId = projectId },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
            }
                var commentList = comments.Select(c =>
                {
                    c.UserId ??= c.CreatedBy;
                    c.CreatedBy ??= c.UserId;
                    return c;
                }).ToList();
            
            return commentList;

        }


        public async Task<IEnumerable<CommentModel>>DeleteCommentAsync(int commentId,int projectId,string comment,string user)
        {
            var result = await Connection.QueryAsync<CommentModel>(
                sql: "[CAP].[Delete_Comment]",
                param: new
                {
                    Id = commentId,
                    ProjectId = projectId,
                    Comment = comment,
                    DeletedBy = user
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            var commentList = result.Select(c =>
            {
                c.UserId ??= c.CreatedBy;
                c.CreatedBy ??= c.UserId;
                return c;
            }).ToList();

            return commentList;
        }
    }
}

