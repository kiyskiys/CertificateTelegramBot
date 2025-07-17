using CertificateTelegramBot_Main.Data;
using System.Text;
using Telegram.Bot.Types;

namespace CertificateTelegramBot.Services.CertificateGeneration
{
    internal class TestingCertificate
    {
        public static InputFileStream GenerateCertificateFile(Certificate certificate)
        {
            var fileName = $"Справка_{certificate.CertificateId}_{certificate.User.Surname}.txt";
            var fileContent = $"Это заглушка для справки {certificate.CertificateId}\nСтудент: {certificate.User.Surname}\nТип: {certificate.CertificateType}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            return InputFileStream.FromStream(stream, fileName);
        }

    }
}
