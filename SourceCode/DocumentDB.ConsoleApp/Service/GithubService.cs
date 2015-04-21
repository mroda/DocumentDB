using DocumentDB.ConsoleApp.Model;
using Octokit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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

        public static async Task GetTemplatesInRepo()
        {
            var contents = await GithubService.Client.Repository.Content.GetContents(repoOwner, repoName, "/");
            foreach (var item in contents)
            {

            }
        }

        public static async Task GetTemplatesFoldersAsync()
        {
            List<TemplateFolder> folders = new List<TemplateFolder>();
            var contents = await GithubService.Client.Repository.Content.GetContents(repoOwner, repoName, "/");
            foreach (RepositoryContent content in contents.Where(c => c.DownloadUrl == null))
            {
                var files = await GetFilesAsync(content.Name);
                var folder = new TemplateFolder();
                folder.Name = content.Name;
                folder.Files = files.ToArray();
                folders.Add(folder);
            }
        }

        private static async Task<List<Object>> GetFilesAsync(string path)
        {
            var files = new List<Object>();
            var contents = await Client.Repository.Content.GetContents(repoOwner, repoName, path);
            foreach (var item in contents.Where(c => c.DownloadUrl != null))
            {

            }
            return files;
        }
    }
}
