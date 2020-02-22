using Newtonsoft.Json;
using System;
using System.IO;

namespace LambdaBiz.AWS.Service
{
    class Program
    {
        internal class AWS
        {
            public string Region { get; set; }
            public string AccessKeyID { get; set; }
            public string SecretAccessKey { get; set; }
            public string LambdaRole { get; set; }
        }
        static void Main(string[] args)
        {
            var contents = File.ReadAllText("D://Work//AWSWorkflowProvider.json");

            var aws = JsonConvert.DeserializeObject<AWS>(contents);
            try
            {

                while (true)
                {

                    var orch = new AWSRESTService(aws.AccessKeyID, aws.SecretAccessKey, aws.Region);
                    orch.Run("RESTSequence1").Wait();

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
