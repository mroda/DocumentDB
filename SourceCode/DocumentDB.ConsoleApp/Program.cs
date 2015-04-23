using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentDB.ConsoleApp.Model;
using DocumentDB.ConsoleApp.Services;

namespace DocumentDB.ConsoleApp
{
    public class Program
    {
        private static bool isStoped = false;

        public static Queue<Template> QueueTemplates { get; set; }

        public static void Main(string[] args)
        {
            QueueTemplates = new Queue<Template>();

            LoadTemplatesToDocumentDB();
            Console.ReadKey();
        }

        public static void LoadTemplatesToDocumentDB()
        {
            while (!isStoped)
            {
                try
                {
                    GithubService.ReadARMTemplatesAsync().Wait();
                    DocumentDBService.SaveTemplatesAsync().Wait();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unexpected error: {0}", ex.Message);
                }
                finally
                {
                    ////Refreash templates each hour
                    Thread.Sleep(3600000);
                }
            }

        }

       
    }
}
