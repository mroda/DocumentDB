using DocumentDB.ConsoleApp.Model;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDB.ConsoleApp.Service
{
    public static class GithubService
    {
        private static readonly string gitUserName = ConfigurationManager.AppSettings["githubUsername"];
        private static readonly string password = ConfigurationManager.AppSettings["githubPassword"];
        private static readonly string repoOwner = ConfigurationManager.AppSettings["githubRepoOwner"];
        private static readonly string repoName = ConfigurationManager.AppSettings["githubRepoName"];

        private static GitHubClient githubClient = null;
        private static GitHubClient Client
        {
            get
            {
                if (githubClient == null)
                {
                    githubClient = new GitHubClient(new ProductHeaderValue("console-gitHub-documentDB"), new Uri("https://github.com/"))
                    {
                        Credentials = new Credentials(gitUserName, password)
                    };
                }

                return githubClient;
            }
        }

        public static async Task<List<Template>> GetARMTemplatesAsync()
        {
            List<Template> templates = new List<Template>();
            var contents = await GithubService.Client.Repository.Content.GetContents(repoOwner, repoName, "/");
            foreach (RepositoryContent content in contents.Where(c => c.DownloadUrl == null))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("GET FILES FROM TEMPLATE '{0}'", content.Name));

                var template = new Template();

                try
                {
                    template.Id = content.Name;
                    template.Link = content.GitUrl.AbsoluteUri;

                    var metadata = await GetMetadataJsonAsync(content.Name);

                    template.Author = metadata.GetValue("githubUsername").ToString();
                    template.Description = metadata.GetValue("description").ToString();
                    template.TemplateUpdated = metadata.GetValue("dateUpdated").ToString();
                    template.Description = metadata.GetValue("description").ToString();
                    template.Title = metadata.GetValue("itemDisplayName").ToString();

                    var scriptInTemplates = await GetScriptFilesAsync(content.Name);
                    template.ScriptFiles = scriptInTemplates.ToArray();

                    template.ReadmeLink = await GetReadmeLinkAsync(content.Name);

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

        private static async Task<JObject> GetMetadataJsonAsync(string path)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Read file 'metadata.json' from GitHub... ");

            var contents = await Client.Repository.Content.GetContents(repoOwner, repoName, path);

            var metadataJsonFile = contents.FirstOrDefault(c => string.Equals(c.Name, "metadata.json"));
            if (metadataJsonFile == null)
            {
                throw new Exception(string.Format("This template no contains metadata.json and not will be load into documentDB"));
            }

            var content = await GetStringFile(metadataJsonFile.DownloadUrl.AbsoluteUri);
            var result = JObject.Parse(content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");

            return result;
        }

        private static async Task<string> GetReadmeLinkAsync(string path)
        {
            var contents = await Client.Repository.Content.GetContents(repoOwner, repoName, path);

            var readmeFile = contents.FirstOrDefault(c => string.Equals(Path.GetExtension(c.Name), ".md"));
            return readmeFile.DownloadUrl.AbsoluteUri;
        }

        private static async Task<List<ScriptFile>> GetScriptFilesAsync(string path)
        {
            bool hasError;
            var files = new List<ScriptFile>();
            var contents = await Client.Repository.Content.GetContents(repoOwner, repoName, path);
            foreach (var content in contents.Where(c => c.DownloadUrl != null && Path.GetExtension(c.Name) == ".json" && !string.Equals(c.Name, "metadata.json")))
            {
                hasError = false;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(string.Format("Read file '{0}' from GitHub... ", content.Name));

                var file = new ScriptFile();
                try
                {

                    file.fileName = content.Name;
                    file.Link = content.DownloadUrl.AbsoluteUri;
                    file.FileContent = await GetContentFromJsonFileAsync(content.DownloadUrl.AbsoluteUri);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("done!");
                }
                catch (Exception ex)
                {
                    var baseMessage = ex.GetBaseException();
                    hasError = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("error!");
                    Console.WriteLine("{0}, Message: ", ex.Message, baseMessage.Message);
                }

                if (!hasError)
                    files.Add(file);
            }
            return files;
        }

        private static async Task<string> GetProfilePictureLink(string userName)
        {
            var user = await githubClient.User.Get(userName);
            return user.AvatarUrl;
        }

        private static async Task<Object> GetContentFromJsonFileAsync(string uri)
        {
            var content = await GetStringFile(uri);
            return Newtonsoft.Json.JsonConvert.DeserializeObject(content);
        }

        private static async Task<string> GetStringFile(string uri)
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
