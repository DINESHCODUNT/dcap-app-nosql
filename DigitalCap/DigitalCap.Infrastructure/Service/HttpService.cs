using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class HttpService : IHttpService
    {
        private string _accessToken;
        private readonly string _username;
        private readonly string _password;
        private readonly string _signinUrl;
        private readonly string _endpointUrl;
        private readonly string _clientId;
        private readonly string _subscriptionKey;
        private readonly IHttpClientFactory _httpClientFactory;
        public HttpService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _username = config["FreedomUser"];
            _password = config["FreedomPw"];
            _signinUrl = config["FreedomSigninUrl"];
            _endpointUrl = config["FreedomEndpointUrl"];
            _clientId = config["FreedomClientId"];
            _subscriptionKey = config["FreedomSubscriptionKey"];
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<TResult>> GetDataAsync<TContainer, TResult>(string clientUrl) where TContainer : IJsonContainer<TResult>
        {

            var options = new RestClientOptions(clientUrl)
            {
                Timeout = Timeout.InfiniteTimeSpan,

            };
            var client = new RestClient(options);


            var request = new RestRequest();

            request.AddHeader("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.AddHeader("Authorization", "Bearer " + AccessToken);

            RestResponse response = client.GetAsync(request).Result;

            try
            {
                var wrapperObject = JsonConvert.DeserializeObject<TContainer>(response.Content);

                var data = wrapperObject.Data;
                return data;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string AccessToken
        {
            get
            {
                if (_accessToken == null)
                {
                    _accessToken = GetAccessToken();
                }
                return _accessToken;
            }
        }


        #region Private Methods

        private string GetAccessToken()
        {
            var options = new RestClientOptions(_signinUrl)
            {
                Timeout = Timeout.InfiniteTimeSpan,

            };
            var restClient = new RestClient(options);
            var restRequest = new RestRequest();

            restRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            restRequest.AddHeader("apikey", "mkst4dga");
            restRequest.AddHeader("Cookie", "x-ms-cpim-sso:absauth.onmicrosoft.com_0=m1.R5eOB6qIbFW0o/RL.Z4MYMDd8w9fgDSoFseAwMw==.0.DmrTvSD25lkYPZiILcDjJa4+Ueq9K+4w4TBp4wG/FBeYPUxY5ZLAXQY7/tsbvQ4F4qbO7H3PD2ip8IxPK0aFoiw9nG2ji8Q3WXRRluQeiQulA/4cBUM7DdMKlT8/XgLAvS+FEbZOsai9osy2yd3vR42y7rvkirkgr1dKyoSqTUbO7nxBm9ikZAgWVNbyS+Ley1Ywh2Cw0raS4sgRsR0RhYrVXAffK40WUnHrkm87YxDy");
            restRequest.AddParameter("username", _username);
            restRequest.AddParameter("password", _password);
            restRequest.AddParameter("grant_type", "password");
            restRequest.AddParameter("client_id", _clientId);
            restRequest.AddParameter("response_type", "token");
            restRequest.AddParameter("scope", $"openid {_clientId}");
            RestResponse response = restClient.PostAsync(restRequest).Result;
            var jObject = JObject.Parse(response.Content);
            return jObject.GetValue("access_token").ToString();
        }

        #endregion
    }
}
