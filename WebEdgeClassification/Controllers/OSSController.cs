using Autodesk.Forge;
using Autodesk.Forge.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WebEdgeClassification.Controllers
{
    [ApiController]
    public class OSSController : ControllerBase
    {
        private IWebHostEnvironment _env;
        public OSSController(IWebHostEnvironment env) { _env = env; }
        public string ClientId { get { return OAuthController.GetAppSetting("FORGE_CLIENT_ID").ToLower(); } }
        // BucketName
        private string _bucketName;
        public string BucketName { get => _bucketName; }
        // server region for bucket, can be EMEA also
        private string _bucketRegion;
        public string BucketRegion { get => _bucketRegion; }
        /// <summary>
        /// Return list of buckets (id=#) or list of objects (id=bucketKey)
        /// </summary>
        [HttpGet]
        [Route("api/forge/oss/buckets")]
        public async Task<IList<TreeNode>> GetOSSAsync(string id, string bucketName)
        {
            if (bucketName == null) { bucketName = "all"; };
            IList<TreeNode> nodes = new List<TreeNode>();
            dynamic oauth = await OAuthController.GetInternalAsync();

            if (id == "#" || id == null) // root
            {
                // in this case, let's return all buckets
                BucketsApi appBuckets = new BucketsApi();
                appBuckets.Configuration.AccessToken = oauth.access_token;

                // to simplify, let's return only the first 100 buckets
                dynamic buckets = await appBuckets.GetBucketsAsync(_bucketRegion, 100);
                foreach (KeyValuePair<string, dynamic> bucket in new DynamicDictionaryItems(buckets.items))
                {
                    string bucketIdent = bucket.Value.bucketKey;
                    bucketIdent.ToLower();
                    if (bucketIdent.Contains(bucketName) || bucketName == "all")
                    {
                        nodes.Add(new TreeNode(
                        bucket.Value.bucketKey,
                        bucket.Value.bucketKey.Replace(ClientId + "-", string.Empty),
                        "bucket",
                        true));
                    }

                }
            }
            else
            {
                // as we have the id (bucketKey), let's return all 
                ObjectsApi objects = new ObjectsApi();
                objects.Configuration.AccessToken = oauth.access_token;
                var objectsList = await objects.GetObjectsAsync(id, 100);
                foreach (KeyValuePair<string, dynamic> objInfo in new DynamicDictionaryItems(objectsList.items))
                {
                    string fileName = objInfo.Value.objectKey;
                    fileName.ToLower();
                    string fileType = "object";
                    if (fileName.Contains("_input_"))
                    {
                        nodes.Add(new TreeNode(
                                            Base64Encode((string)objInfo.Value.objectId),
                                            objInfo.Value.objectKey,
                                            fileType,
                                            false));
                    }
                }
            }
            return nodes;
        }

        /// <summary>
        /// Model data for jsTree used on GetOSSAsync
        /// </summary>
        public class TreeNode
        {
            public TreeNode(string id, string text, string type, bool children)
            {
                this.id = id;
                this.text = text;
                this.type = type;
                this.children = children;
            }

            public string id { get; set; }
            public string text { get; set; }
            public string type { get; set; }
            public bool children { get; set; }
        }

        /// <summary>
        /// Create a new bucket 
        /// </summary>
        [HttpPost]
        [Route("api/forge/oss/buckets")]
        public async Task<dynamic> CreateBucket([FromBody] BucketModel bucket)
        {
            dynamic NewBucket = null;
            BucketsApi buckets = new BucketsApi();
            dynamic token = await OAuthController.GetInternalAsync();
            buckets.Configuration.AccessToken = token.access_token;
            _bucketName = bucket.bucketKey.ToLower();
            _bucketRegion = bucket.region.ToString();
            PostBucketsPayload bucketPayload = new PostBucketsPayload(string.Format("{0}-{1}", ClientId, _bucketName), null,
              PostBucketsPayload.PolicyKeyEnum.Transient);

            try
            {
                NewBucket = await buckets.CreateBucketAsync(bucketPayload, BucketRegion);
            }
            catch (Exception e)
            {
                if (e.Message == "Error calling CreateBucket: {\"reason\":\"Bucket already exists\"}")
                {
                    dynamic allBuckets = await buckets.GetBucketsAsync(BucketRegion, 100);
                    foreach (KeyValuePair<string, dynamic> actualBucket in new DynamicDictionaryItems(allBuckets.items))
                    {
                        string bucketName = actualBucket.Value.bucketKey;
                        if (bucketName.Contains(BucketName))
                        {
                            NewBucket = actualBucket;
                        }
                    }
                }
            }

            return NewBucket;
        }

        /// <summary>
        /// Delete bucket
        /// </summary>
        [HttpDelete]
        [Route("api/forge/oss/buckets")]
        public async Task DeleteBucket([FromBody] BucketModel bucketModel)
        {
            BucketsApi bucket = new BucketsApi();
            dynamic token = OAuthController.GetInternalAsync();
            bucket.Configuration.AccessToken = token.access_token;
            await bucket.DeleteBucketAsync(bucketModel.bucketKey);
        }

        /// <summary>
        /// Input model for CreateBucket method
        /// </summary>
        public class BucketModel
        {
            public string region { get; set; }
            public string bucketKey { get; set; }
        }

        /// <summary>
        /// Receive a file from the client and upload to the bucket
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/forge/oss/objects")]
        public async Task<dynamic> UploadObject([FromForm] UploadFile input)
        {
            // save the file on the server
            var fileSavePath = Path.Combine(_env.WebRootPath, Path.GetFileName(input.FileToUpload.FileName));
            using (var stream = new FileStream(fileSavePath, FileMode.Create))
                await input.FileToUpload.CopyToAsync(stream);


            // get the bucket...
            dynamic oauth = await OAuthController.GetInternalAsync();
            ObjectsApi objects = new ObjectsApi();
            objects.Configuration.AccessToken = oauth.access_token;

            // upload the file/object, which will create a new object
            dynamic uploadedObj;
            using (StreamReader streamReader = new StreamReader(fileSavePath))
            {
                uploadedObj = await objects.UploadObjectAsync(input.BucketKey,
                       Path.GetFileName(input.FileToUpload.FileName),
                       (int)streamReader.BaseStream.Length,
                       streamReader.BaseStream,
                       "application/octet-stream");
            }

            // cleanup
            System.IO.File.Delete(fileSavePath);

            return uploadedObj;
        }

        [HttpDelete]
        [Route("api/forge/oss/objects/delete")]
        public async Task DeleteObject([FromBody] ObjectModel objectModel)
        {
            dynamic oauth = await OAuthController.GetInternalAsync();
            ObjectsApi objectForDelete = new ObjectsApi();
            objectForDelete.Configuration.AccessToken = oauth.access_token;

            await objectForDelete.DeleteObjectAsync(objectModel.bucketKey, objectModel.objectKey);
        }

        [HttpGet]
        [Route("api/forge/oss/object")]
        public async Task<string> GetObject([FromBody] DownloadFile file)
        {
            dynamic oauth = OAuthController.GetInternalAsync();
            ObjectsApi forgeObject = new ObjectsApi();
            forgeObject.Configuration.AccessToken = oauth.access_token;
            string bucketKey = string.Format("{0}-{1}", ClientId, _bucketName).ToLower();
            try
            {
                string result = await forgeObject.GetObjectAsync(bucketKey, "edges.json"); //downloadFile.fileToDownload
                return result;
            }
            catch (Exception) { }
            return null;
        }
        /// <summary>
        /// Make signed Url for object in bucket
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/forge/objects/signed")]
        public async Task<dynamic> DownloadObject([FromBody] DownloadFile input)
        {
            // get the bucket
            dynamic oauth = await OAuthController.GetInternalAsync();
            ObjectsApi objects = new ObjectsApi();
            objects.Configuration.AccessToken = oauth.access_token;

            // collect information about file
            string bucketKey = input.bucketKey;
            string fileToDownload = input.fileToDownload;

            PostBucketsSigned postBucketsSigned = new PostBucketsSigned(20);
            try
            {
                dynamic result = await objects.CreateSignedResourceAsync(bucketKey, fileToDownload, postBucketsSigned);
                return result;
            }
            catch (Exception) { }

            return null;
        }

        /// <summary>
        /// Base64 enconde a string
        /// </summary>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public class DownloadFile
        {
            public string bucketKey { get; set; }
            public string fileToDownload { get; set; }
        }

        public class UploadFile
        {
            public string BucketKey { get; set; }
            public IFormFile FileToUpload { get; set; }
        }

        public class ObjectModel
        {
            public string bucketKey { get; set; }
            public string objectKey { get; set; }
        }
    }
}
