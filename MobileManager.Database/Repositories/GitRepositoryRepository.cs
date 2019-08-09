using System;
using System.Collections.Generic;
using System.Linq;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Models.Git;

namespace MobileManager.Database.Repositories
{
    public class GitRepositoryRepository : IRepository<GitRepository>
    {
        private readonly GeneralDbContext _context;

        public GitRepositoryRepository(GeneralDbContext context)
        {
            _context = context;
        }

        public void Add(GitRepository entity)
        {
            _context.GitRepositories.Add(entity);
            _context.SaveChanges();
        }

        public void Add(IEnumerable<GitRepository> entities)
        {
            _context.GitRepositories.AddRange(entities);
            _context.SaveChanges();
        }

        public IEnumerable<GitRepository> GetAll()
        {
            return _context.GitRepositories;
        }

        public GitRepository Find(string id)
        {
            return _context.GitRepositories.First(x => x.Id == Guid.Parse(id));
        }

        public bool Remove(string id)
        {
            _context.GitRepositories.Remove(Find(id));
            _context.SaveChanges();

            return true;
        }

        public void Update(GitRepository entity)
        {
            _context.GitRepositories.Update(entity);
            _context.SaveChanges();
        }
    }
}
