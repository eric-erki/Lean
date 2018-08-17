/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using QuantConnect.Util;

namespace QuantConnect.Archives
{
    /// <summary>
    /// Provides an implementation of <see cref="IArchive"/> that uses <see cref="ZipFile"/> from ICSharpZipLib
    /// </summary>
    public class SharpZipArchive : IArchive
    {
        private readonly ZipFile _zipFile;
        private readonly ZipOutputStream _zipOutputStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharpZipArchive"/> class
        /// </summary>
        /// <remarks>
        ///This is read-only constructor
        /// </remarks>
        /// <param name="zipFile">The zip file</param>
        public SharpZipArchive(ZipFile zipFile)
        {
            _zipFile = zipFile;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharpZipArchive"/> class
        /// </summary>
        /// <remarks>
        /// This is write-only constructor
        /// </remarks>
        /// <param name="zipOutputStream">The zip output stream</param>
        public SharpZipArchive(ZipOutputStream zipOutputStream)
        {
            _zipOutputStream = zipOutputStream;
        }

        /// <summary>
        /// Creates a new collection containing all of this archive's entries
        /// </summary>
        /// <returns>A new collection containing all of this archive's entries</returns>
        public IReadOnlyCollection<IArchiveEntry> GetEntries()
        {
            if (_zipFile == null)
            {
                throw new InvalidOperationException("Unable to get entries of SharpZipArchive in write-only mode.");
            }

            var entries = new List<IArchiveEntry>();
            var enumerator = _zipFile.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Current as ZipEntry;
                if (entry != null)
                {
                    entries.Add(new Entry(entry.Name, _zipFile, null, entry));
                }
            }

            return entries.AsReadOnly();

        }

        /// <summary>
        /// Gets the entry by the specified name or null if it does not exist
        /// </summary>
        /// <param name="key">The entry's key</param>
        /// <returns>The archive entry, or null if not found</returns>
        public IArchiveEntry GetEntry(string key)
        {
            if (_zipFile == null)
            {
                var entry = new ZipEntry(key);
                _zipOutputStream.PutNextEntry(entry);
                return new Entry(key, _zipFile, _zipOutputStream, entry);
            }
            else
            {
                var entry = _zipFile.GetEntry(key);
                return new Entry(key, _zipFile, null, entry);
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _zipOutputStream?.Finish();
            _zipOutputStream?.DisposeSafely();
            _zipFile?.DisposeSafely();
        }

        /// <summary>
        /// Defines an entry in a <see cref="ZipFile"/>
        /// </summary>
        private class Entry : IArchiveEntry
        {
            private ZipEntry _entry;
            private readonly ZipFile _zipFile;
            private readonly ZipOutputStream _zipOutputStream;

            /// <summary>
            /// Gets the entry's key
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// True if the archive contains and entry with this key, false if it does not exist
            /// </summary>
            public bool Exists => _entry != null;

            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> class
            /// </summary>
            /// <param name="key">The entry's key</param>
            /// <param name="zipFile">The zip file</param>
            /// <param name="outputStream">The output stream for writing</param>
            /// <param name="entry">the zip entry</param>
            public Entry(string key, ZipFile zipFile, ZipOutputStream outputStream, ZipEntry entry)
            {
                Key = key;
                _entry = entry;
                _zipFile = zipFile;
                _zipOutputStream = outputStream;
            }

            /// <summary>
            /// Opens the entry's stream for reading
            /// </summary>
            /// <returns>The entry's stream</returns>
            public Stream Read()
            {
                return _zipFile.GetInputStream(_entry);
            }

            /// <summary>
            /// Writes the stream to the archive under this entry
            /// </summary>
            /// <param name="stream">The data stream to write</param>
            public void Write(Stream stream)
            {
                if (_entry != null)
                {
                    _zipFile?.Delete(_entry);
                }

                _entry = new ZipEntry(Key);
                _zipOutputStream.PutNextEntry(_entry);
                stream.CopyTo(_zipOutputStream);
            }
        }
    }
}