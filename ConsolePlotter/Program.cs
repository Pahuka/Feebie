using System.Collections.Concurrent;
using ConsolePlotter;
using ConsolePlotter.Channels;

var cancellationToken = new CancellationTokenSource();
var taskCollection = new BlockingCollection<Task>();
var settings = new Settings();
settings.Init();
var producer = new ChannelProducer(settings.MaxCopyThreads, cancellationToken.Token, settings);
var consumer = new ChannelConsumer(producer.Channel.Reader, cancellationToken.Token, taskCollection);
var consumer2 = new ChannelConsumer(producer.Channel.Reader, cancellationToken.Token, taskCollection);

Logger.WriteLog("Для запуска процесса нажмите любую клавишу\nДля отмены - \"с\"");
Console.ReadKey();

Task.Run(() =>
{
	if (Console.ReadKey(true).KeyChar == 'c') cancellationToken.Cancel();
});

Task.WaitAll(producer.Run(), consumer.Run(), consumer2.Run());

foreach (var task in taskCollection.GetConsumingEnumerable()) 
	await task;

Logger.WriteLog("Все задачи завершены", ConsoleColor.Green);
Console.ReadKey(true);