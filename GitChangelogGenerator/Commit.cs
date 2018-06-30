using System;
using System.Collections.Generic;
using System.Text;

namespace GitChangelogGenerator
{
    public class Commit
    {
        public DateTime Date { get; set; }
        public string Body { get; set; }
        public string Category { get; set; }
    }
}
