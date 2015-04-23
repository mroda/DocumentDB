using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using DocumentDB.ConsoleApp.Model;

namespace DocumentDB.ConsoleApp.Service
{
    public class DocumentDBService
    {
        private static readonly string documentDBEndpointUrl = ConfigurationManager.AppSettings["documentDBEndpointUrl"];
        private static readonly string documentDBAuthorizationKey = ConfigurationManager.AppSettings["documentDBAuthorizationKey"];
        private static readonly string databaseId = ConfigurationManager.AppSettings["databaseId"];
        private static readonly string collectionId = ConfigurationManager.AppSettings["collectionId"];

        private static DocumentClient client;
        public DocumentDBService() { }
        
        private static async Task<Database> GetOrCreateDatabaseAsync(string id)
        {
            Database database = client.CreateDatabaseQuery().Where(db => db.Id == id).ToArray().FirstOrDefault();
            if (database == null)
            {
                database = await client.CreateDatabaseAsync(new Database { Id = id });
            }

            return database;
        }

        private static async Task<DocumentCollection> GetOrCreateCollectionAsync(string dbLink, string id)
        {
            DocumentCollection collection = client.CreateDocumentCollectionQuery(dbLink).Where(c => c.Id == id).ToArray().FirstOrDefault();
            if (collection == null)
            {
                collection = await client.CreateDocumentCollectionAsync(dbLink, new DocumentCollection { Id = id });
            }

            return collection;
        }


        internal static void SaveToDocumentDB(List<Template> templates)
        {
            templates.ForEach( async t => await SaveTemplate(t));
        }

        public static async Task SaveTemplate(Template template)
        {
            try
            {
                using (client = new DocumentClient(new Uri(documentDBEndpointUrl), documentDBAuthorizationKey))
                {
                    Database database = await GetOrCreateDatabaseAsync(databaseId);
                    DocumentCollection collection = await GetOrCreateCollectionAsync(database.CollectionsLink, collectionId);

                    dynamic doc = client.CreateDocumentQuery<Document>(collection.DocumentsLink)
                        .Where(d => d.Id == template.Id).AsEnumerable()
                        .FirstOrDefault();

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("Saving Template '{0}'... ", template.Id);

                    if (doc == null)
                    {
                        //Save a new document 
                        await client.CreateDocumentAsync(collection.DocumentsLink, template);
                    }
                    else
                    {
                        //Update exist document
                        await client.ReplaceDocumentAsync(doc.SelfLink, template);
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done!");
                }
            }
            catch (Exception ex)
            {
                var baseError = ex.GetBaseException();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error!");
                Console.WriteLine("{0}. Message: {1}", ex.Message, baseError.Message);
            }
        }
    }
}
