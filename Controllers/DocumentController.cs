using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Elasticsearch.Net;
using System.Text;
using indexationCv.models;
using Microsoft.AspNetCore.Http;
using System.IO;


namespace indexationCv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentsController : ControllerBase
    {

        private readonly ILogger<DocumentsController> _logger;
        // private readonly IElasticClient _client;

        public DocumentsController(ILogger<DocumentsController> logger)
        {
            _logger = logger;
            // _client = client;
        }

        [HttpPost]
        public async  Task<IActionResult> IndexFile(IFormFile file)
        {
                //Console.WriteLine("---- IM HERE " );
                var defaultIndex = "attachments";
                var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));

                var settings = new ConnectionSettings(pool)
                    .DefaultIndex(defaultIndex)
                    .DisableDirectStreaming()
                    .PrettyJson()
                    .OnRequestCompleted(callDetails =>
                    {
                        if (callDetails.RequestBodyInBytes != null)
                        {
                            Console.WriteLine(
                                $"{callDetails.HttpMethod} {callDetails.Uri} \n" +
                                $"{Encoding.UTF8.GetString(callDetails.RequestBodyInBytes)}");
                        }
                        else
                        {
                            Console.WriteLine($"{callDetails.HttpMethod} {callDetails.Uri}");
                        }


                        if (callDetails.ResponseBodyInBytes != null)
                        {
                            Console.WriteLine($"Status: {callDetails.HttpStatusCode}\n" +
                                    $"{Encoding.UTF8.GetString(callDetails.ResponseBodyInBytes)}\n" +
                                    $"{new string('-', 30)}\n");
                        }
                        else
                        {
                            Console.WriteLine($"Status: {callDetails.HttpStatusCode}\n" +
                                    $"{new string('-', 30)}\n");
                        }
                    });

                var client = new ElasticClient(settings);
            
            /*
            if (client.Indices.Exists(defaultIndex).Exists)
            {
                var deleteIndexResponse = client.Indices.Delete(defaultIndex);
            }

            */
                var createIndexResponse = client.Indices.Create(defaultIndex, c => c
                    .Settings(s => s
                        .Analysis(a => a
                            .Analyzers(ad => ad
                                .Custom("windows_path_hierarchy_analyzer", ca => ca
                                    .Tokenizer("windows_path_hierarchy_tokenizer")
                                )
                            )
                            .Tokenizers(t => t
                                .PathHierarchy("windows_path_hierarchy_tokenizer", ph => ph
                                    .Delimiter('\\')
                                )
                            )
                        )
                    )
                    .Map<Document>(mp => mp
                        .AutoMap()
                        .Properties(ps => ps
                            .Text(s => s
                                .Name(n => n.Path)
                                .Analyzer("windows_path_hierarchy_analyzer")
                            )
                            .Object<Attachment>(a => a
                                .Name(n => n.Attachment)
                                .AutoMap()
                            )
                        )
                    )
                );

            var putPipelineResponse = client.Ingest.PutPipeline("attachments", p => p
                .Description("Document attachment pipeline")
                .Processors(pr => pr
                .Attachment<Document>(a => a
                    .Field(f => f.Content)
                    .TargetField(f => f.Attachment)
                )
                .Remove<Document>(r => r
                    .Field(ff => ff
                    .Field(f => f.Content)
                    )
                )
                )
            );
            var filePath = Path.GetTempFileName();

            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }
            var base64File = Convert.ToBase64String(System.IO.File.ReadAllBytes(filePath));

            //var expectedGuid = Guid.NewGuid();

            var indexResponse = client.Index(new Document
            {
                Id = Guid.NewGuid(),
                Path = filePath,
                Content = base64File
            }, i => i
                .Pipeline("attachments")
                .Refresh(Refresh.WaitFor)
            );
            return Ok();
        }

        // Get All Documents : http://localhost:9200/attachments/_search
        // Get Document by Id : http://localhost:9200/attachments/_doc/{id}
        /* Get By FullText Search :
            url : http://localhost:9200/attachments/_search
            body : {
                "query": {
                    "query_string" : {
                        "query" : "hayrat"
                    }
                }
            }

            */
        /*
         Front - Interfaces :
            - Upload : Input upload + Api Upload [http://localhost:5000/documents] : POST
            - Get All
                - Affichage CV dans un tableau.
                        - Creer une class [Model] qui contient titre, path,content , author
                        - Consommation API : http://localhost:9200/attachments/_search
                                - Mettre tous dans un tableau pour l'affiche
                        - HTML : Titre et href<path>(pour telecharer le CV)
        */
    }
}
