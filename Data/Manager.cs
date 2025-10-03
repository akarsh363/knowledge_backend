using System;
using System.Collections.Generic;

namespace Project_Version1.Data;

public partial class Manager
{
    public int ManagerId { get; set; }

    public int UserId { get; set; }

    public int DeptId { get; set; }

    public virtual ICollection<Commit> Commits { get; set; } = new List<Commit>();

    public virtual Department Dept { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
