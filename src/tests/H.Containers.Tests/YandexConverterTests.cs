/*using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    
    [TestClass]
    public class YandexConverterTests
    {
        public const string FolderId = "$FolderId$";
        public const string OAuthToken = "$OAuthToken$";

        public static IConverter CreateConverter() => new YandexConverter
        {
            OAuthToken = OAuthToken,
            FolderId = FolderId,
            Lang = "ru-RU",
            SampleRateHertz = 8000,
        };
        
        [TestMethod]
        public async Task StartStreamingRecognitionTest()
        {
            using var converter = CreateConverter();

            await BaseConvertersTests.StartStreamingRecognitionTest(converter, "проверка_проверка_8000.wav", "проверка");
        }

        [TestMethod]
        public async Task StartStreamingRecognitionTest_RealTime()
        {
            using var recorder = CreateRecorder();
            using var converter = CreateConverter();

            await BaseConvertersTests.StartStreamingRecognitionTest_RealTime(recorder, converter);
        }

        [TestMethod]
        public async Task ConvertTest()
        {
            using var converter = CreateConverter();

            await BaseConvertersTests.ConvertTest(converter, "проверка_проверка_8000.wav", "проверка проверка");
        }

        [TestMethod]
        public async Task ConvertTest_RealTime()
        {
            using var recorder = CreateRecorder();
            using var converter = CreateConverter();

            await BaseConvertersTests.ConvertTest_RealTime(recorder, converter);
        }
    }
}
*/