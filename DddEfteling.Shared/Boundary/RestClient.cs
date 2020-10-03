﻿using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DddEfteling.Shared.Boundary
{
    public abstract class RestClient
    {
        public static readonly HttpClient client = new HttpClient();
        private Uri baseUri;

        protected void setBaseUri(string baseUri)
        {
            this.baseUri = new Uri(baseUri);
        }

        public async Task<Stream> GetResource(string path)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("Application/Json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Efteling");

            Uri targetUri = new Uri(baseUri, path);

            var streamTask = client.GetStreamAsync(targetUri.AbsoluteUri);

            return await streamTask;
        }

        public async Task<Stream> GetResource(string path, Dictionary<string, string> urlParams)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("Application/Json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Efteling");

            Uri targetUri = new Uri(baseUri, path);

            var streamTask = client.GetStreamAsync(QueryHelpers.AddQueryString(targetUri.AbsoluteUri, urlParams));

            return await streamTask;
        }

    }
}
