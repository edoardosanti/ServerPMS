using System;
using System.Threading;
using System.Threading.Tasks;
namespace ServerPMS.Infrastructure.Generic
{
    /// <summary>
    /// An async-compatible reader/writer lock using SemaphoreSlim.
    /// </summary>
    public class ReaderWriterLockAsync
    {
        private readonly SemaphoreSlim _readerCountLock = new(1, 1); // protects readerCount
        private readonly SemaphoreSlim _writerLock = new(1, 1);      // exclusive write lock
        private int _readerCount = 0;

        /// <summary>
        /// Acquire a read lock (multiple readers allowed).
        /// </summary>
        public async Task<IDisposable> ReaderLockAsync(CancellationToken ct = default)
        {
            await _readerCountLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                _readerCount++;
                if (_readerCount == 1)
                {
                    // First reader locks out writers
                    await _writerLock.WaitAsync(ct).ConfigureAwait(false);
                }
            }
            finally
            {
                _readerCountLock.Release();
            }

            return new Releaser(this, isWriter: false);
        }

        /// <summary>
        /// Acquire a write lock (exclusive).
        /// </summary>
        public async Task<IDisposable> WriterLockAsync(CancellationToken ct = default)
        {
            await _writerLock.WaitAsync(ct).ConfigureAwait(false);
            return new Releaser(this, isWriter: true);
        }

        private void ReleaseReader()
        {
            _readerCountLock.Wait();
            try
            {
                _readerCount--;
                if (_readerCount == 0)
                {
                    _writerLock.Release();
                }
            }
            finally
            {
                _readerCountLock.Release();
            }
        }

        private void ReleaseWriter()
        {
            _writerLock.Release();
        }

        private sealed class Releaser : IDisposable
        {
            private ReaderWriterLockAsync? _parent;
            private readonly bool _isWriter;

            public Releaser(ReaderWriterLockAsync parent, bool isWriter)
            {
                _parent = parent;
                _isWriter = isWriter;
            }

            public void Dispose()
            {
                if (_parent != null)
                {
                    if (_isWriter)
                        _parent.ReleaseWriter();
                    else
                        _parent.ReleaseReader();

                    _parent = null;
                }
            }
        }
    }
}