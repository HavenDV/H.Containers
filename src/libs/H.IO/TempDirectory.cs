﻿using System;
using System.IO;

namespace H.IO
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TempDirectory : IDisposable
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Folder { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public bool DeleteOnDispose { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deleteOnDispose"></param>
        public TempDirectory(bool deleteOnDispose = true)
        {
            DeleteOnDispose = deleteOnDispose;
            
            var random = new Random();
            Folder = Path.Combine(Path.GetTempPath(), "H.Temp", $"{random.Next()}");
            
            Directory.CreateDirectory(Folder);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            if (!Directory.Exists(Folder))
            {
                return;
            }
            
            foreach (var path in Directory.EnumerateFiles(Folder, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(path);
                }
                catch (UnauthorizedAccessException)
                {
                    // ignored.
                }
            }

            try
            {
                Directory.Delete(Folder, true);
            }
            catch (UnauthorizedAccessException)
            {
                // ignored.
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (!DeleteOnDispose)
            {
                return;
            }

            Clear();
        }
        
        #endregion
    }
}
