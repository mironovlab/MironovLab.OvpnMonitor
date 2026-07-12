using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace MironovLab.OpenVPN.Management.Core
{
    internal class OneTimeEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private volatile int _count;

        public OneTimeEnumerable(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Interlocked.Increment(ref _count) == 1)
                return _enumerator;

            throw new InvalidOperationException(Resources.OneTimeEnumerable_CannotEnumerateTwice);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
