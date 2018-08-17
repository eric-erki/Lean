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
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;
using Ionic.Zip;
using SharpZipLibZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;
using IonicZipFile = Ionic.Zip.ZipFile;
using ZipOutputStream = ICSharpCode.SharpZipLib.Zip.ZipOutputStream;

namespace QuantConnect.Archives
{
    /// <summary>
    /// Provides methods for easily creating archive instances using different underlying implementations
    /// </summary>
    public static class Archive
    {
        /// <summary>
        /// Opens the archive at the specified path
        /// </summary>
        /// <param name="path">The file path to the archive</param>
        /// <param name="impl">The archive implementation to use</param>
        /// <returns>The archive</returns>
        public static IArchive OpenReadOnly(string path, ArchiveImplementation impl = ArchiveImplementation.DotNetFramework)
        {
            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new NotImplementedException($"Archive file with extension '{extension}' is not implemented.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Archive file was not found at {new FileInfo(path).FullName}");
            }

            var fileStream = new Lazy<FileStream>(() => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));

            switch (impl)
            {
                case ArchiveImplementation.DotNetFramework:
                    return new DotNetFrameworkZipArchive(new ZipArchive(fileStream.Value));

                case ArchiveImplementation.SharpZipLib:
                    return new SharpZipArchive(new SharpZipLibZipFile(fileStream.Value));

                case ArchiveImplementation.Ionic:
                    return new IonicZipArchive(new IonicZipFile(path));

                default:
                    throw new ArgumentOutOfRangeException(nameof(impl), impl, null);
            }
        }

        /// <summary>
        /// Opens the archive at the specified path for read/write
        /// </summary>
        /// <param name="path">The file path to the archive</param>
        /// <param name="impl">The archive implementation to use</param>
        /// <returns>The archive</returns>
        public static IArchive OpenWrite(string path, ArchiveImplementation impl = ArchiveImplementation.DotNetFramework)
        {
            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new NotImplementedException($"Archive file with extension '{extension}' is not implemented.");
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists && fileInfo.Length == 0)
            {
                fileInfo.Delete();
            }

            if (fileInfo.Exists)
            {
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
                switch (impl)
                {
                    case ArchiveImplementation.DotNetFramework:
                        return new DotNetFrameworkZipArchive(new ZipArchive(fileStream, ZipArchiveMode.Update));

                    case ArchiveImplementation.SharpZipLib:
                        return new SharpZipArchive(new ZipOutputStream(fileStream));

                    case ArchiveImplementation.Ionic:
                        return new IonicZipArchive(IonicZipFile.Read(fileStream));

                    default:
                        throw new ArgumentOutOfRangeException(nameof(impl), impl, null);
                }
            }
            else
            {
                switch (impl)
                {
                    case ArchiveImplementation.DotNetFramework:
                        return new DotNetFrameworkZipArchive(new ZipArchive(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None), ZipArchiveMode.Create));

                    case ArchiveImplementation.SharpZipLib:
                        return new SharpZipArchive(new ZipOutputStream(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)));

                    case ArchiveImplementation.Ionic:
                        return new IonicZipArchive(new IonicZipFile(path));

                    default:
                        throw new ArgumentOutOfRangeException(nameof(impl), impl, null);
                }
            }
        }
    }
}