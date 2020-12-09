﻿using System;
using System.IO;

namespace H.Containers.Tests
{
    public class TempDirectory : IDisposable
    {
        #region Properties

        public string Folder { get; }
        public bool DeleteOnDispose { get; }

        #endregion

        #region Constructors

        public TempDirectory(bool deleteOnDispose = true)
        {
            DeleteOnDispose = deleteOnDispose;
            
            var random = new Random();
            Folder = Path.Combine(Path.GetTempPath(), "H.Temp", $"{random.Next()}");
            
            Directory.CreateDirectory(Folder);
        }

        #endregion

        #region Methods

        public void Dispose()
        {
            if (!DeleteOnDispose)
            {
                return;
            }
            
            Directory.Delete(Folder, true);
        }
        
        #endregion
    }
}