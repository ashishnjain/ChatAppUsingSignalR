using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalRChat.Models
{
    public class Messages
    {
        public int MessageId { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public string UserImage { get; set; }
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
    }

    public class FileUploadBE
    {
        public string FileName { get; set; }
        public byte[] FileByte { get; set; }
    }
}