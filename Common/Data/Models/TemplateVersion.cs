using System;

namespace Nebula.Common.Data.Models
{
    public class TemplateVersion
    {
        public Guid Id { get; set; }
        public string Version { get; set; }
        public string CommitSha { get; set; }
        public DateTime DateAdded { get; set; }
    }
}