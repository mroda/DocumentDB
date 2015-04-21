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
                Service.GithubService.GetTemplatesFoldersAsync().Wait();
                //Service.DocumentDBService.GetStart().Wait();
            }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", ex.Message, baseEx.Message);
            }

            Console.ReadKey();
        }
    }
}
