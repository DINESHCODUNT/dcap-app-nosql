using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ExternalLoginResult
    {
        public bool Succeeded { get; set; }
        public string? ReturnUrl { get; set; }
        public string? Error {  get; set; }

        private ExternalLoginResult() { }

        public static ExternalLoginResult Ok(string? returnUrl = null)
        {
            return new ExternalLoginResult
            {
                Succeeded = true,
                ReturnUrl = returnUrl
            };
        }

        public static ExternalLoginResult Fail(string error)
        {
            return new ExternalLoginResult
            {
                Succeeded = false,
                Error = error
            };
        }
    }

}
