using System;
using System.Collections.Generic;

namespace Project_Version1.Data;

public partial class Attachment
{
    public int AttachmentId { get; set; }

    public int? PostId { get; set; }

    public int? CommentId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual Comment? Comment { get; set; }

    public virtual Post? Post { get; set; }
}
