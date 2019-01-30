using System;

namespace MobileManager.Models.Git
{
    public interface IGitRepository
    {
        Guid Id { get; set; }

        string Name { get; set; }

        string Remote { get; set; }

        string DirectoryPath { get; set; }
    }
}
