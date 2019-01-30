using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobileManager.Models.Git;
using MobileManager.Models.Xcuitest.enums;

namespace MobileManager.Models.Xcuitest
{
    /// <inheritdoc />
    public class Xcuitest : IXcuitest
    {
        public Xcuitest()
        {
            Id = Guid.NewGuid();
        }

        /// <inheritdoc />
        [Key]
        public Guid Id { get; set; }

        /// <inheritdoc />
        public GitRepository GitRepository { get; set; }

        /// <inheritdoc />
        public string Project { get; set; }

        /// <inheritdoc />
        public string Scheme { get; set; }

        /// <inheritdoc />
        public string Sdk { get; set; }

        /// <inheritdoc />
        public string Destination { get; set; }

        /// <inheritdoc />
        public string Action { get; set; }

        /// <inheritdoc />
        public string Results { get; set; }
        
        public XcuitestOutputFormat OutputFormat { get; set; }
    }
}
