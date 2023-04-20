using System.Collections.Concurrent;
using System.Reflection.Metadata;


namespace Terra
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DocumentQueue<T> : IDocumentsQueue, IDisposable
    {
        // To detect redundant calls
        private bool _disposedValue;

        private ExternalSystemConnector _connector = new ExternalSystemConnector();        
        private ConcurrentQueue<Document> _docs = new ConcurrentQueue<Document>();  
        private ConcurrentBag<Document> _docBuffer = new ConcurrentBag<Document>();
        private IProgress<string> _progress;
        private Timer _timer;
        private CancellationToken _ct;
        private bool _process = false;

        public ConcurrentQueue<Document> DocumentsInQueue
        {
            get
            {
                return _docs;
            }
        }

        public DocumentQueue(uint timerTimeInSeconds, IProgress<string> reporter, CancellationToken ct) 
        {
            _timer = new Timer(
                callback: new TimerCallback(TimerTask),
                null,
                TimeSpan.FromSeconds(timerTimeInSeconds),
                TimeSpan.FromSeconds(timerTimeInSeconds));
            _progress = reporter;          
            _ct = ct;

        }
       
        public void Enqueue(Document document)
        {                         
            _docs.Enqueue(document);                                                           
        }

        private async void TimerTask(object? timerState)
        {
            try
            {
                if (_process)
                    return;
                _process = true;
                _ct.ThrowIfCancellationRequested();                
                _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.onTimer]);               
                if (_docBuffer.Count < _connector.PkgSize && !_docs.IsEmpty)
                    FillBuffer();
                await SendDocs();
                _process = false;
            }
            catch (OperationCanceledException oce)
            {
                // return docs from buffer to queue
                _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.cancel]);
                _progress.Report(oce.Message);
                RestoreQueue();
                _process = false;               
            }           
            catch(Exception ex) 
            {
                // process exception
                _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.exception]);
                _progress.Report(ex.Message);
                _process = false;              
            }
        }
         
        private void FillBuffer()
        {           
            var freeInBuffer = _connector.PkgSize - _docBuffer.Count;
            var amountToTake = freeInBuffer > _docs.Count ? _docs.Count : freeInBuffer;
            for (int i = 0; i < amountToTake; i++)
            {
                _docs.TryDequeue(out var document);
                _docBuffer.Add(document);
            }
            _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.bufferReady]);                         
        }
        private async Task SendDocs()
        {
            try
            {               
                _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.startSending]);
                if (_docBuffer.Count > _connector.PkgSize)
                {
                    _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.bufferOverflow]);
                    _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.warning]);
                    var tempChunk = new List<Document>();
                    while (tempChunk.Count < _connector.PkgSize)
                    {
                        _docBuffer.TryTake(out var document);
                        tempChunk.Add(document);
                    }
                    await _connector.SendDocuments(tempChunk, _ct);
                }
                else
                {
                    await _connector.SendDocuments(_docBuffer, _ct);
                    _ct.ThrowIfCancellationRequested();
                    // clear buffer  if success
                    _docBuffer.Clear();
                }                
                _progress.Report(ReportMessageStorage.MessageDictionary[QueueStateEnum.endSending]);               
            }           
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void RestoreQueue()
        {
            foreach (var document in _docBuffer)
            {
                _docs.Enqueue(document);
            }
            _docBuffer.Clear();
        }

        public void Dispose()
        {
            if (!_disposedValue)
            {
                _timer.Dispose();                
                _docs.Clear();
                _docBuffer.Clear();

                _disposedValue = true;
            }                       
        }
    }
}
