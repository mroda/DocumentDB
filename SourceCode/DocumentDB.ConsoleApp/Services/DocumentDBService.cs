using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentDB.ConsoleApp.Model;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace DocumentDB.ConsoleApp.Services
{
    public class DocumentDBService
    {
        private static readonly string DocumentDBEndpointUrl = ConfigurationManager.AppSettings["documentDBEndpointUrl"];
        private static readonly string DocumentDBAuthorizationKey = ConfigurationManager.AppSettings["documentDBAuthorizationKey"];
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["databaseId"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["collectionId"];

        private static DocumentClient client;

        public DocumentDBService()
        {
        }

        private static async Task<Database> GetOrCreateDatabaseAsync(string id)
        {
            Database database = client.CreateDatabaseQuery().Where(db => db.Id == id).ToArray().FirstOrDefault();
            if (database == null)
            {
                database = await client.CreateDatabaseAsync(new Database { Id = id });
            }

            return database;
        }

        private static async Task<DocumentCollection> GetOrCreateCollectionAsync(string collectionLink, string id)
        {
            DocumentCollection collection = client.CreateDocumentCollectionQuery(collectionLink).Where(c => c.Id == id).ToArray().FirstOrDefault();
            if (collection == null)
            {
                collection = await client.CreateDocumentCollectionAsync(collectionLink, new DocumentCollection { Id = id });
            }

            return collection;
        }

        public static async Task SaveTemplate(Template template, DocumentCollection collection, Database database)
        {
            try
            {
                dynamic doc = client.CreateDocumentQuery<Document>(collection.DocumentsLink)
                    .Where(d => d.Id == template.Id).AsEnumerable()
                    .FirstOrDefault();

                Console.ForegroundColor = ConsoleColor.Gray;

                if (doc == null)
                {
                    ////Save a new document 
                    Console.Write("Saving Template '{0}'... ", template.Id);
                    await client.CreateDocumentAsync(collection.DocumentsLink, template);
                }
                else
                {
                    ////Update exist document
                    Console.Write("Updating Template '{0}'... ", template.Id);
                    await client.ReplaceDocumentAsync(doc.SelfLink, template);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Done!");

            }
            catch (Exception ex)
            {
                var baseError = ex.GetBaseException();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error!");
                Console.WriteLine("{0}. Message: {1}", ex.Message, baseError.Message);
            }
        }

        public static async Task DeleteTemplate(string id)
        {
            using (client = new DocumentClient(new Uri(DocumentDBEndpointUrl), DocumentDBAuthorizationKey))
            {
                Database database = await GetOrCreateDatabaseAsync(DatabaseId);
                DocumentCollection collection = await GetOrCreateCollectionAsync(database.CollectionsLink, CollectionId);

                dynamic doc = client.CreateDocumentQuery<Document>(collection.DocumentsLink)
                           .Where(d => d.Id == id).AsEnumerable()
                           .FirstOrDefault();

                if (doc != null)
                {
                    var docDeleted = await client.DeleteDocumentAsync(doc.SelfLink);
                }
            }
        }

        public static async Task ListTemplates()
        {
            using (client = new DocumentClient(new Uri(DocumentDBEndpointUrl), DocumentDBAuthorizationKey))
            {
                Database database = await GetOrCreateDatabaseAsync(DatabaseId);
                DocumentCollection collection = await GetOrCreateCollectionAsync(database.CollectionsLink, CollectionId);

                dynamic documents = client.CreateDocumentQuery<Document>(collection.DocumentsLink)
                           .ToList();

                foreach (var item in documents)
                {
                    var template = (Template)item;
                    Console.WriteLine(template.Id);
                    Console.WriteLine(template.Author);

                    ////get parameters in azuredeploy.json
                    var scriptFileEntity = template.ScriptFiles.FirstOrDefault(sf => sf.FileName.Equals("azuredeploy.json", StringComparison.InvariantCultureIgnoreCase));
                    var azureDeploy = Newtonsoft.Json.Linq.JObject.Parse(scriptFileEntity.FileContent.ToString());

                    var parameters = Newtonsoft.Json.Linq.JObject.Parse(azureDeploy.GetValue("parameters").ToString());
                    foreach (var prop in parameters.Properties())
                    {
                        var name = prop.Name;
                        var value = prop.Value;
                    }
                    
                    Console.WriteLine("version " + azureDeploy.GetValue("contentVersion").ToString());
                }

            }
        }


        internal static async Task SaveTemplatesAsync()
        {
            using (client = new DocumentClient(new Uri(DocumentDBEndpointUrl), DocumentDBAuthorizationKey))
            {
                Database database = await GetOrCreateDatabaseAsync(DatabaseId);
                DocumentCollection collection = await GetOrCreateCollectionAsync(database.CollectionsLink, CollectionId);

                while (Program.QueueTemplates.Count>0)
                {
                    await SaveTemplate(Program.QueueTemplates.Dequeue(), collection, database);
                }
            }
        }
    }
}
