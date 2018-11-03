using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using SpeedTestLogger.Models;

namespace SpeedTestLogger
{
    class Program
    {
        private static LoggerConfiguration _loggerConfig;
        private static SubscriptionClient _subscriptionClient;

        static async Task Main(string[] args)
        {
            _loggerConfig = new LoggerConfiguration();

            var options = new MessageHandlerOptions(HandleException)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = true
            };
            _subscriptionClient = new SubscriptionClient(
                _loggerConfig.ServiceBus.ConnectionString,
                _loggerConfig.ServiceBus.TopicName,
                _loggerConfig.ServiceBus.SubscriptionName);
            _subscriptionClient.RegisterMessageHandler(HandleMessage, options);

            Console.ReadKey();

            await _subscriptionClient.CloseAsync();
        }

        static async Task HandleMessage(Message message, CancellationToken token)
        {
            var messageBody = Encoding.UTF8.GetString(message.Body);
            if (messageBody != "RUN_SPEEDTEST") return;

            Console.WriteLine("Running speedtest!");

            var runner = new SpeedTestRunner(_loggerConfig.LoggerLocation);
            var testData = runner.RunSpeedTest();
            var results = new TestResult
            {
                SessionId = new Guid(),
                User = _loggerConfig.UserId,
                Device = _loggerConfig.LoggerId,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Data = testData
            };

            var success = false;
            using (var client = new SpeedTestApiClient(_loggerConfig.ApiUrl))
            {
                success = await client.PublishTestResult(results);
            }

            Console.WriteLine($"SpeedTest {(success == true ? "complete" : "failed")}!");
        }

        static Task HandleException(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");

            return Task.CompletedTask;
        }
    }
}