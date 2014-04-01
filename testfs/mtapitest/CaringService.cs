using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mtapitest
{
    public abstract class CaringService : IDisposable
    {
        protected string username = null;
        protected System.Security.SecureString password = null;
        private Exception lastException = null;

        public bool Success
        {
            get { return lastException == null ? true : false; }
        }

        public CaringService(string username, System.Security.SecureString password)
        {
            if (String.IsNullOrEmpty(username))
                throw new ArgumentNullException("username");

            if (password == null || password.Length < 1)
                throw new ArgumentNullException("password");

            this.username = username;
            this.password = password;
        }

        protected void ResetLastError()
        {
            if(!Success)
                this.lastException = null;
        }

        #region IDisposable pattern
        /// <summary>
        /// distruttore
        /// </summary>
        ~CaringService()
        {
            this.Dispose(false);
        }

        private bool disposed = false;

        /// <summary>
        /// Dispose the context
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    username = null;
                    password = null;

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Dispose the context
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}