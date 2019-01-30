using System;
using System.ComponentModel.DataAnnotations;

namespace MobileManager.Models.Git
{
    public class GitRepository : IGitRepository
    {
        [Key] public Guid Id { get; set; }

        public string Name { get; set; }

        public string Remote { get; set; }
        
        public string DirectoryPath { get; set; }
    }
}
