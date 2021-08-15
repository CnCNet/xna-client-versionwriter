/*
 * Copyright © 2017-2021 Starkku
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace VersionWriter
{
    interface IFileItem
    {
        /// <summary>
        /// Filename of file.
        /// </summary>
        string Filename { get; set; }

        /// <summary>
        /// MD5 hash of file as string.
        /// </summary>
        string ID { get; set; }

        /// <summary>
        /// Filesize of file, in kilobytes.
        /// </summary>
        int Size { get; set; }

        /// <summary>
        /// Should file be archived or not.
        /// </summary>
        bool Archived { get; set; }

        /// <summary>
        /// MD5 hash of the archived file.
        /// </summary>
        string ArchiveID { get; set; }

        /// <summary>
        /// Filesize of archived file, in kilobytes.
        /// </summary>
        int ArchiveSize { get; set; }
    }

    internal class FileItem : IFileItem
    {
        public string Filename { get; set; }
        public string ID { get; set; }
        public int Size { get; set; }
        public bool Archived { get; set; }
        public string ArchiveID { get; set; }
        public int ArchiveSize { get; set; }
    }

    internal class CustomComponentItem : IFileItem
    {
        /// <summary>
        /// Custom component ID.
        /// </summary>
        public string ComponentID { get; set; }
        public string Filename { get; set; }
        public string ID { get; set; }
        public int Size { get; set; }
        public bool Archived { get; set; }
        public string ArchiveID { get; set; }
        public int ArchiveSize { get; set; }
    }
}
