using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Models.Xcuitest;

namespace MobileManager.Database.Repositories
{
    public class XcuitestRepository : IRepository<Xcuitest>
    {
        private readonly GeneralDbContext _context;

        public XcuitestRepository(GeneralDbContext context)
        {
            _context = context;
        }

        public void Add(Xcuitest entity)
        {
            _context.Xcuitests.Add(entity);
            _context.SaveChanges();
        }

        public void Add(IEnumerable<Xcuitest> entities)
        {
            _context.Xcuitests.AddRange(entities);
            _context.SaveChanges();
        }

        public IEnumerable<Xcuitest> GetAll()
        {
            return _context.Xcuitests;
        }

        public Xcuitest Find(string id)
        {
            return _context.Xcuitests.First(x => x.Id == Guid.Parse(id));
        }

        public bool Remove(string id)
        {
            _context.Xcuitests.Remove(Find(id));
            _context.SaveChanges();

            return true;
        }

        public void Update(Xcuitest entity)
        {
            _context.Xcuitests.Update(entity);
            _context.SaveChanges();        }
    }
}
