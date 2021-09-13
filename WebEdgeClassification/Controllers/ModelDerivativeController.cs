using Autodesk.Forge;
using Autodesk.Forge.Model;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebEdgeClassification.Controllers
{
    [ApiController]
    public class ModelDerivativeController : ControllerBase
    {
        /// <summary>
        /// Start the translation job for a give bucketKey/objectName
        /// </summary>
        /// <param name="objModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/forge/modelderivative/jobs")]
        public async Task<dynamic> TranslateObject([FromBody] TranslateObjectModel objModel)
        {
            dynamic oauth = await OAuthController.GetInternalAsync();

            // prepare the payload
            List<JobPayloadItem> outputs = new List<JobPayloadItem>()
            {
            new JobPayloadItem(
                JobPayloadItem.TypeEnum.Svf,
                new List<JobPayloadItem.ViewsEnum>()
                {
                JobPayloadItem.ViewsEnum._2d,
                JobPayloadItem.ViewsEnum._3d
                })
            };
            JobPayloadInput jobPayloadInput;

            if (objModel.ObjectType == "zipfile3D")
            {
                jobPayloadInput = new JobPayloadInput(objModel.ObjectName, true, "MyWallShelf.iam");
            }
            else if (objModel.ObjectType == "zipfile2D")
            {
                jobPayloadInput = new JobPayloadInput(objModel.ObjectName, true, "WallShelfDrawings.idw");
            }
            else
            {
                jobPayloadInput = new JobPayloadInput(objModel.ObjectName);
            }
            // start the translation
            JobPayload job = new JobPayload(jobPayloadInput, new JobPayloadOutput(outputs));

            DerivativesApi derivative = new DerivativesApi();
            derivative.Configuration.AccessToken = oauth.access_token;
            dynamic jobPosted = await derivative.TranslateAsync(job);
            return jobPosted;
        }

        /// <summary>
        /// Model for TranslateObject method
        /// </summary>
        public class TranslateObjectModel
        {
            public string BucketKey { get; set; }
            public string ObjectName { get; set; }
            public string ObjectType { get; set; }
        }
    }
}
