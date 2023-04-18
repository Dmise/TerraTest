using System.Reflection.Metadata;
using Terra;

var documents = new List<Document>();
for (int i = 0; i < 6; i++)
{
    var document = new Document();
    documents.Add(document);
}

var cts = new CancellationTokenSource();
var queue = new DocumentQueue<string>(10, new Progress<string>(), cts.Token);
foreach (var document in documents)
{
    queue.Enqueue(document);
}

while (true)
{

}