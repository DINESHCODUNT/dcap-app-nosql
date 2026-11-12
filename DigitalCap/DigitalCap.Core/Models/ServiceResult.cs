using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public string Message { get; set; }
        public string ErrorCode { get; set; }

        // Convenience property to get the first error message
        public string ErrorMessage => ErrorMessages?.FirstOrDefault();

        public string UserName { get; set; }
        public object Permissions { get; set; }
        public object Email { get; set; }

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T> { IsSuccess = true, Data = data };
        }

        public static ServiceResult<T> Failure(string errorMessage, string errorCode = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessages = new List<string> { errorMessage },
                ErrorCode = errorCode
            };
        }

        public static ServiceResult<T> Ok(T data = default)
           => new ServiceResult<T> { IsSuccess = true, Data = data };

        public static ServiceResult<T> Failure(List<string> errorMessages, string errorCode = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessages = errorMessages ?? new List<string>(),
                ErrorCode = errorCode
            };
        }

        public static ServiceResult<T> SuccessResult(T data, string message = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        // Method to add additional error messages
        public void AddError(string errorMessage)
        {
            ErrorMessages.Add(errorMessage);
            IsSuccess = false;
        }

        // Method to add multiple error messages
        public void AddErrors(IEnumerable<string> errorMessages)
        {
            ErrorMessages.AddRange(errorMessages);
            IsSuccess = false;
        }

        public static ServiceResult<List<Project>> Success(List<SelectListItem> selectListItems)
        {
            throw new NotImplementedException();
        }
    }
}
