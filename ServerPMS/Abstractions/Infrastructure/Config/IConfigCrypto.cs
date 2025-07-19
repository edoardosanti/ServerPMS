
namespace ServerPMS.Abstractions.Infrastructure.Config
{
    public interface IConfigCrypto
    {
        //properties
        string EnvFilePath { get; set; }

        //methods
        void EncryptToFile(string msg, string outputPath);
        string DecryptFromFile(string inputPath);

    }
}
