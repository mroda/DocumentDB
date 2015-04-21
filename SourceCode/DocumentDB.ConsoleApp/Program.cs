using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDB.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var templates = Service.GithubService.GetTemplatesFoldersAsync().Result;
                Service.DocumentDBService.SaveToDocumentDB(templates).Wait();
            }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: {0}, Message: {1}", ex.Message, baseEx.Message);
            }

            Console.ReadKey();
        }
    }
}
