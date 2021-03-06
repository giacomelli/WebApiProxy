﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using WebApiProxy.Core.Models;
using WebApiProxy.Tasks.Templates;

namespace WebApiProxy.Tasks
{
    public class ProxyGenerationTask : ITask
    {
        private Configuration config;

        [Output]
        public string Filename { get; set; }

        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {


            try
            {

                config = Configuration.Load();

                config.Metadata = GetProxy();

                var template = new CSharpProxyTemplate(config);

                var source = template.TransformText();

                File.WriteAllText(Filename, source);
                File.WriteAllText(Configuration.CacheFile, source);
            }
            catch (InvalidOperationException ex)
            {
                TryReadFromCache(ex);
            }
            catch (Exception ex)
            {
                throw ex;
            }


            return true;
        }



        private Metadata GetProxy()
        {
            var url = string.Empty;

            try
            {
                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("X-Proxy-Type", "metadata");

                    var response = Task.Run(() => client.GetAsync(config.Endpoint)).Result;
                    response.EnsureSuccessStatusCode();

                    var metadata = response.Content.ReadAsAsync<Metadata>().Result;

                    return metadata;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(config.Endpoint, ex);
            }
        }

        private void TryReadFromCache(Exception ex)
        {

            if (!File.Exists(Configuration.CacheFile))
            {
                throw ex;
            }

            var source = File.ReadAllText(Configuration.CacheFile);
            File.WriteAllText(Filename, source);


        }


    }
}

