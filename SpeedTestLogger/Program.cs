using System;
using System.Threading.Tasks;
using SpeedTestLogger.Models;

namespace SpeedTestLogger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello SpeedTestLogger!");

            var config = new LoggerConfiguration();
            var runner = new SpeedTestRunner(config.LoggerLocation);
            var testData = runner.RunSpeedTest();
            var results = new TestResult
            {
                SessionId = new Guid(),
                User = config.UserId,
                Device = config.LoggerId,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Data = testData
            };

            var success = false;
            using (var client = new SpeedTestApiClient(config.ApiUrl))
            {
                success = await client.PublishTestResult(results);
            }

            Console.WriteLine($"SpeedTest {(success == true ? "complete" : "failed")}!");
        }
    }
}