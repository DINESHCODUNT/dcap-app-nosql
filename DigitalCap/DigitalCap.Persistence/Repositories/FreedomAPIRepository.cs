using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.VesselModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class FreedomAPIRepository : IFreedomAPIRepository
    {
        private readonly string _endpointUrl;
        private readonly IHttpService _httpService;
        private class VesselObject : IJsonContainer<Vessel>
        {
            public Vessel[] Vessel { get; set; }
            public IEnumerable<Vessel> Data { get => Vessel; }
        }
        private class VesselsObject : IJsonContainer<Vessel>
        {
            public Vessel[] Vessels { get; set; }

            public IEnumerable<Vessel> Data { get => Vessels; }
        }
        private class SurveysObject : IJsonContainer<VesselSurvey>
        {
            public VesselSurvey[] Surveys { get; set; }

            public IEnumerable<VesselSurvey> Data
            {
                get => Surveys;
            }
        }
        public async Task<Vessel> GetVessel(string classNumber)
        {
            try
            {
                var vessel = await GetData<VesselObject, Vessel>(null, classNumber);
                if (vessel != null)
                {
                    return vessel.FirstOrDefault();
                }
                else return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private async Task<IEnumerable<U>> GetData<T, U>(string query, string classNum = null)
          where U : CachedData
          where T : IJsonContainer<U>
        {
            string clientUrl = string.Empty;

            if (classNum == null)
            {
                clientUrl = $"{_endpointUrl}/vessel/{query}";
            }
            else
            {
                clientUrl = $"{_endpointUrl}/vessel/{classNum}";

            }

            if (!string.IsNullOrEmpty(query) && classNum != null)
            {
                clientUrl = $"{clientUrl}/{query}";
            }

            var data = _httpService.GetDataAsync<T, U>(clientUrl).Result;
            return data;
        }

        public async Task<IEnumerable<VesselSurvey>> GetSurveys(string classNum)

        {
            return await GetData<SurveysObject, VesselSurvey>("survey-audit", classNum);
        }

        #region Private Methods
        public async Task<IEnumerable<Certificate>> GetCertificates(string classNum)
        {
            IEnumerable<Certificate> certificates = await GetData<CertificatesObject, Certificate>("certificates", classNum);
            if (certificates != null)
            {
                return certificates.Select(certificate =>
                {
                    var updateCertificate = certificate;
                    updateCertificate.ClassNumber = classNum;
                    return updateCertificate;
                });
            }
            else
            {
                return null;
            }
        }

        #endregion

        public class CertificatesObject : IJsonContainer<Certificate>
        {
            [JsonProperty("certificates")]
            public Certificate[] Certificates { get; set; }

            public IEnumerable<Certificate> Data { get => Certificates; }
        }

        private string _accessToken;
        private readonly string _username;
        private readonly string _password;
        private readonly string _signinUrl;
        private readonly string _clientId;
        private readonly string _subscriptionKey;


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

        public FreedomAPIRepository(IConfiguration config, IHttpService httpService)
        {
            _username = config["FreedomUser"];
            _password = config["FreedomPw"];
            _signinUrl = config["FreedomSigninUrl"];
            _endpointUrl = config["FreedomEndPointUrl"];
            _clientId = config["FreedomClientId"];
            _subscriptionKey = config["FreedomSubscriptionKey"];
            _httpService = httpService;


        }

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
            RestResponse response = restClient.ExecutePostAsync(restRequest).Result;
            var jObject = JObject.Parse(response.Content);
            return jObject.GetValue("access_token").ToString();
        }
    }
}
