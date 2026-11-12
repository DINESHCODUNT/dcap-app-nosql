using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class CreateAccountResponse
    {
        public IReadOnlyList<ClientModel> Clients { get; set; }
    }
}