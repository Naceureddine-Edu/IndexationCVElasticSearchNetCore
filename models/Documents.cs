using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;
using indexationCv.models;
using Microsoft.AspNetCore.Http;
using System.IO;


namespace indexationCv.models
{
    public class Document
    {
        public Guid Id { get; set; }
        /// <summary>
        /// FileData Base64 encoded
        /// </summary>
        public string Content { get; set; }
        public string Path {get; set;}
        public Attachment Attachment { get; set; }
    }
}