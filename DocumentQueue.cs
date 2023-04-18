using ConsoleApp2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Terra
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DocumentQueue<T> : IDocumentsQueue, IAsyncDisposable , IDisposable
    {
        // To detect redundant calls
        private bool _disposedValue;

        private ExternalSystemConnector _connector = new ExternalSystemConnector();        
        private ConcurrentQueue<Document> _docs = new ConcurrentQueue<Document>();
        private ConcurrentBag<Document> _docBuffer = new ConcurrentBag<Document>();
        private ProgressReportFactory<T> _reportFactory;
        private IProgress<T> _progress;
        private Timer _timer;
        private CancellationToken _ct;

        public ConcurrentQueue<Document> DocumentsInQueue
        {
            get
            {
                return _docs;
            }
        }

        public DocumentQueue(uint timerTimeInSeconds, IProgress<T> reporter, CancellationToken ct) 
        {
            _timer = new Timer(
                callback: new TimerCallback(TimerTask),
                null,
                TimeSpan.FromSeconds(timerTimeInSeconds),
                TimeSpan.FromSeconds(timerTimeInSeconds));
            _progress = reporter;
            _reportFactory = new ProgressReportFactory<T>();
            _ct = ct;

        }
       
        public void Enqueue(Document document)
        {
            try
            {               
                _docs.Enqueue(document);               
            }
            catch(Exception ex) 
            {
                throw ex;
            }                       
        }

        private async void TimerTask(object? timerState)
        {
            try
            {
                _ct.ThrowIfCancellationRequested();
                // report something
                _progress.Report(_reportFactory.Report(QueueStateEnum.onTimer));
                // fill buffer 
                if (!_docBuffer.IsEmpty || !_docs.IsEmpty)
                {
                    var freeInBuffer = _connector.PkgSize - _docBuffer.Count;
                    var amountToTake = freeInBuffer > _docs.Count ? _docs.Count : freeInBuffer;
                    for (int i = 0; i < amountToTake; i++)
                    {
                        _docs.TryDequeue(out var document);
                        _docBuffer.Add(document);
                    }
                    _progress.Report(_reportFactory.Report(QueueStateEnum.bufferReady));
                    if (_docBuffer.Count > _connector.PkgSize)
                    {
                        throw new ArgumentException();
                    }
                    await SendDocs();
                }
            }
            catch (OperationCanceledException oce)
            {
                // return docs from buffer to queue
                _progress.Report(_reportFactory.Report(QueueStateEnum.cancel));
                RestoreQueue();
                
                return; // completing TimerTask
            }
            catch (ArgumentException)
            {                
                _progress.Report(_reportFactory.Report(QueueStateEnum.bufferOverflow));
                var over = _docBuffer.Count - _connector.PkgSize;
                for (int i = 0; i < over; i++)
                {
                    _docBuffer.TryTake(out var document);
                    _docs.Enqueue(document);                   
                }
                await SendDocs();
            }
            catch(Exception ex) 
            {
                // process exception
                Console.WriteLine(ex.Message);
            }
        }
              
        private async Task SendDocs()
        {
            try
            {
                _progress.Report(_reportFactory.Report(QueueStateEnum.startSending));
                await _connector.SendDocuments(_docBuffer, _ct);
                _progress.Report(_reportFactory.Report(QueueStateEnum.endSending));
                _ct.ThrowIfCancellationRequested();
                                               
                // clear buffer  if success
                _docBuffer.Clear();
            }
            catch (OperationCanceledException oce)
            {
                _progress.Report(_reportFactory.Report(QueueStateEnum.cancel));
                RestoreQueue();
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

        /// <summary>
        /// Stop SendDocument process
        /// </summary>
        /// <returns></returns>
        public ValueTask DisposeAsync()
        {
            Task.Run(() => Dispose()).Wait();   
            return ValueTask.CompletedTask;
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
