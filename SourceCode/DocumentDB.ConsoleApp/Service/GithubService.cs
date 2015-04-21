using DocumentDB.ConsoleApp.Model;
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

        public static async Task<List<ARMTemplateFile>> GetTemplatesFoldersAsync()
        {
            List<ARMTemplateFile> templates = new List<ARMTemplateFile>();
            var contents = await GithubService.Client.Repository.Content.GetContents(repoOwner, repoName, "/");
            foreach (RepositoryContent content in contents.Where(c => c.DownloadUrl == null))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("GET FILES FROM TEMPLATE '{0}'", content.Name));
                var filesInFolder = await GetFilesAsync(content.Name);
                templates.AddRange(filesInFolder);
            }
            return templates;
        }

        private static async Task<List<ARMTemplateFile>> GetFilesAsync(string path)
        {
            bool hasError;
            var files = new List<ARMTemplateFile>();
            var contents = await Client.Repository.Content.GetContents(repoOwner, repoName, path);
            foreach (var item in contents.Where(c => c.DownloadUrl != null))
            {
                hasError = false;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(string.Format("Read file '{0}' from GitHub... ", item.Name));

                var file = new ARMTemplateFile();
                file.Id = item.Path;
                file.Folder = path;
                file.FileName = item.Name;
                if (Path.GetExtension(item.Name) == ".json")
                {
                    try
                    {
                        file.FileContent = await GetContentFromJsonFileAsync(item.DownloadUrl.AbsoluteUri);
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

                }
                else if (Path.GetExtension(item.Name) == ".txt" || Path.GetExtension(item.Name) == ".md")
                {
                    try
                    {
                        file.FileContent = await GetContentFromTextFileAsync(item.DownloadUrl.AbsoluteUri);
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
                }
                else
                {
                    //TODO Notify that we get an unexpected file
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("no implemented!");
                    hasError = true;//flag 
                }
                if (!hasError)
                    files.Add(file);


            }
            return files;
        }

        private static async Task<Object> GetContentFromJsonFileAsync(string uri)
        {
            var content = await GetStringFile(uri);
            return Newtonsoft.Json.JsonConvert.DeserializeObject(content);
        }

        private static async Task<Object> GetContentFromTextFileAsync(string uri)
        {
            var content = await GetStringFile(uri);
            var contentToJson = @"{
                'encode': 'Base64',
                'text':'" + Base64Encode(content) + @"',
            }";

            return Newtonsoft.Json.JsonConvert.DeserializeObject(contentToJson);
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
