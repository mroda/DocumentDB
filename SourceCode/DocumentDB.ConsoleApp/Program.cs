using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentDB.ConsoleApp.Model;

namespace DocumentDB.ConsoleApp
{
    public class Program
    {
        public static Queue<Template> Queue { get; set; }

        public static void Main(string[] args)
        {
            Queue = new Queue<Template>();

            Thread th = new Thread(new ThreadStart(Run));
            
            //th.Start();

            try
            {
                var templates = Service.GithubService.GetARMTemplatesAsync().Result;
                Service.DocumentDBService.SaveToDocumentDB(templates);
             }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: {0}, Message: {1}", ex.Message, baseEx.Message);
            }

            Console.ReadKey();
        }

        public static void Run()
        {
            bool isCompleted = false;
            while (!isCompleted)
            {
                if (Queue.Count > 0)
                {
                    var template = Queue.Dequeue();
                    Service.DocumentDBService.SaveTemplate(template).Wait();
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
