﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DocumentDB.ConsoleApp.Model;
using Newtonsoft.Json.Linq;
using Octokit;

namespace DocumentDB.ConsoleApp.Services
{
    public static class GithubService
    {
        private static readonly string GitUserName = ConfigurationManager.AppSettings["githubUsername"];
        private static readonly string Password = ConfigurationManager.AppSettings["githubPassword"];
        private static readonly string RepoOwner = ConfigurationManager.AppSettings["githubRepoOwner"];
        private static readonly string RepoName = ConfigurationManager.AppSettings["githubRepoName"];

        private static GitHubClient githubClient = null;

        private static GitHubClient Client
        {
            get
            {
                if (githubClient == null)
                {
                    githubClient = new GitHubClient(new ProductHeaderValue("console-gitHub-documentDB"), new Uri("https://github.com/"))
                    {
                        Credentials = new Credentials(GitUserName, Password)
                    };
                }

                return githubClient;
            }
        }

        public static async Task<List<Template>> GetARMTemplatesAsync()
        {
            List<Template> templates = new List<Template>();
            var contents = await GithubService.Client.Repository.Content.GetContents(RepoOwner, RepoName, "/");
            foreach (RepositoryContent content in contents.Where(c => c.DownloadUrl == null))
            {
                var filesInFolder = await Client.Repository.Content.GetContents(RepoOwner, RepoName, content.Name);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("READING FOLDER '{0}'...", content.Name));


                var template = new Template();
                try
                {
                    CheckIsValidTemplate(filesInFolder);

                    var metadata = await GetMetadataJsonAsync(filesInFolder);
                    var scriptInTemplates = await GetScriptFilesAsync(filesInFolder);

                    template.Id = content.Name;
                    template.Link = content.GitUrl.AbsoluteUri;
                    template.Author = metadata.GetValue("githubUsername").ToString();
                    template.TemplateUpdated = metadata.GetValue("dateUpdated").ToString();
                    template.Title = metadata.GetValue("itemDisplayName") != null
                        ? metadata.GetValue("itemDisplayName").ToString()
                        : String.Empty;
                    template.Description = metadata.GetValue("description") != null
                       ? metadata.GetValue("description").ToString()
                       : string.Empty;

                    template.ScriptFiles = scriptInTemplates.ToArray();

                    template.ReadmeLink = GetReadmeLink(filesInFolder) ?? string.Empty;

                    template.GitHubPictureProfileLink = await GetProfilePictureLink(template.Author);
                    
                    //DocumentDB.ConsoleApp.Program.Queue.Enqueue(template);

                    templates.Add(template);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error!");
                    Console.WriteLine(ex.Message);
                }
            }

            return templates;
        }

        private static void CheckIsValidTemplate(IReadOnlyList<RepositoryContent> files)
        {
            var metadataFile = files.FirstOrDefault(c => c.Name.Equals("metadata.json", StringComparison.InvariantCultureIgnoreCase));
            var azuredeployFile = files.FirstOrDefault(c => c.Name.Equals("azuredeploy.json", StringComparison.InvariantCultureIgnoreCase));
            var azuredeployParametersFile = files.FirstOrDefault(c => c.Name.Equals("azuredeploy-parameters.json", StringComparison.InvariantCultureIgnoreCase));

            if (metadataFile == null || azuredeployFile == null || azuredeployParametersFile == null)
            {
                throw new Exception("Not contains the requiered files to be considered a valid ARM Template");
            }
        }

        private static async Task<JObject> GetMetadataJsonAsync(IReadOnlyList<RepositoryContent> files)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Read file 'metadata.json' from GitHub... ");

            var metadataJsonFile = files.FirstOrDefault(c => string.Equals(c.Name, "metadata.json"));
            if (metadataJsonFile == null)
            {
                throw new Exception(string.Format("This template no contains metadata.json and not will be load into documentDB"));
            }

            var content = await GetStringFileAsync(metadataJsonFile.DownloadUrl.AbsoluteUri);
            var result = JObject.Parse(content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");

            return result;
        }

        private static string GetReadmeLink(IReadOnlyList<RepositoryContent> files)
        {
            var readmeFile = files.FirstOrDefault(c => string.Equals(Path.GetExtension(c.Name), ".md"));
            if (readmeFile != null)
            {
                return readmeFile.DownloadUrl.AbsoluteUri;
            }
            else
            {
                return null;
            }

        }

        private static async Task<List<ScriptFile>> GetScriptFilesAsync(IReadOnlyList<RepositoryContent> files)
        {
            bool hasError;
            var scriptfiles = new List<ScriptFile>();
            foreach (var content in files.Where(c => c.DownloadUrl != null && Path.GetExtension(c.Name) == ".json" && !string.Equals(c.Name, "metadata.json")))
            {
                hasError = false;
               
                var file = new ScriptFile();
                try
                {
                    file.FileName = content.Name;
                    file.Link = content.DownloadUrl.AbsoluteUri;
                    file.FileContent = await GetContentFromJsonFileAsync(content.DownloadUrl.AbsoluteUri);

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(string.Format("Read file '{0}' from GitHub... ", content.Name));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("done!");
                }
                catch (Exception ex)
                {
                    var baseMessage = ex.GetBaseException();
                    hasError = true;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(string.Format("Read file '{0}' from GitHub... ", content.Name));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("error!");
                    Console.WriteLine("{0}, Message: ", ex.Message, baseMessage.Message);
                }

                if (!hasError)
                {
                    scriptfiles.Add(file);
                }
            }

            return scriptfiles;
        }

        private static async Task<string> GetProfilePictureLink(string userName)
        {
            var user = await githubClient.User.Get(userName);
            return user.AvatarUrl;
        }

        private static async Task<object> GetContentFromJsonFileAsync(string uri)
        {
            var content = await GetStringFileAsync(uri);
            return Newtonsoft.Json.JsonConvert.DeserializeObject(content);
        }

        private static async Task<string> GetStringFileAsync(string uri)
        {
            using (var httpClient = new HttpClient())
            {
                return await httpClient.GetStringAsync(uri);
            }
        }

        private static string Base64Encode(string text)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string text)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(text);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
