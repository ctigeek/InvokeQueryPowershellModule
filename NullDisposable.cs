using System;

namespace InvokeQuery
{
    class NullDisposable : IDisposable
    {
        private NullDisposable()
        {
        }
        private static NullDisposable nullDisposable;
        public static NullDisposable Instance
        {
            get
            {
                if (nullDisposable == null)
                {
                    nullDisposable = new NullDisposable();
                }
                return nullDisposable;
            }
        }
        public void Dispose()
        {
        }
    }
}
