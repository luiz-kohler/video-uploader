using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.Minio;

namespace Tests.Integration.Setup
{
    public static class MinioTestConfiguration
    {
        private const string UserName = "minioadmin";
        private const string Password = "minioadmin";
        private const string BucketName = "video-uploader-tests";
        private const string Image = "quay.io/minio/minio:latest";
        private const int InternalPort = 9000;
        private const int UiPort = 9001;
        private static string Endpoint { get; set; } = null!;
        private static int MappedPort { get; set; }

        private static Dictionary<string, string> GenerateEnvVariables(IContainer container)
        {
            return new Dictionary<string, string>
            {
                ["MinIO:Endpoint"] = Endpoint,
                ["MinIO:AccessKey"] = UserName,
                ["MinIO:SecretKey"] = Password,
                ["MinIO:BucketName"] = BucketName,
                ["MinIO:WithSSL"] = "false"
            };
        }

        public static void UpdateEnvVariables(IContainer container)
        {
            MappedPort = container.GetMappedPublicPort(InternalPort);
            Endpoint = $"localhost:{MappedPort}";

            Dictionary<string, string> variables = GenerateEnvVariables(container);

            foreach (KeyValuePair<string, string> variable in variables)
            {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
        }

        public static IContainer BuildConfiguredMinIOContainer()
        {
            return new MinioBuilder()
                .WithImage(Image)
                .WithCommand("--console-address", $":{UiPort}")
                .WithPortBinding(InternalPort, true)
                .WithEnvironment("MINIO_ROOT_USER", UserName)
                .WithEnvironment("MINIO_ROOT_PASSWORD", Password)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*API.*"))
                .Build();
        }
    }
}
