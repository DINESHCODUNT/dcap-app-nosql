using DigitalCap.Core.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IHttpService
    {
        Task<IEnumerable<TResult>> GetDataAsync<TContainer, TResult>(string url) where TContainer : IJsonContainer<TResult>;
    }
}
