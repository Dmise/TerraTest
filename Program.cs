using System.Reflection.Metadata;
using Terra;

#region test arena


#endregion

var reporter = new ProgressReporter();
var documents = new List<Document>();

for (int i = 0; i < 100; i++)
{
    var document = new Document();
    documents.Add(document); 
}

var cts = new CancellationTokenSource();
var queue = new DocumentQueue<string>(10, new Progress<string>(reporter.ReportProgress), cts.Token);

foreach (var document in documents)
{
    queue.Enqueue(document);
}

while (true)
{
    Console.WriteLine($"Докуменов к отправке: {queue.DocumentsInQueue.Count}");
    Task.Delay(1000).Wait();
}