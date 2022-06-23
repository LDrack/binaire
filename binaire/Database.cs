//########################################################################
// (C) Embedded Systems Lab
// All rights reserved.
// ------------------------------------------------------------
// This document contains proprietary information belonging to
// Research & Development FH OÖ Forschungs und Entwicklungs GmbH.
// Using, passing on and copying of this document or parts of it
// is generally not permitted without prior written authorization.
// ------------------------------------------------------------
// info(at)embedded-lab.at
// https://www.embedded-lab.at/
//########################################################################
// File name: Database.cs
// Date of file creation: 2022-04-28
// List of autors: Lucas Drack
//########################################################################

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace binaire
{
    public static class Database
    {
        public class binaireDbContext : DbContext
        {
            // MySQL
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                // Username and password are read from appsettings.json
                // https://www.learnentityframeworkcore5.com/connection-strings-entity-framework-core
                var newbuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                IConfiguration iconfig = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();

                var connectionString = iconfig.GetConnectionString("MyConnection");
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 28));
                optionsBuilder.UseMySql(connectionString, serverVersion);
            }

            public DbSet<Reading> Readings { get; set; }
            public DbSet<Board> Boards { get; set; }
        }

        // First, check if a board with the given IDs already exists in the DB - if not, it is newly instantiated and uploaded to DB.
        // Returns the corresponding Board object, either newly created, or fetched from the DB.
        public static Board AddBoard(Board.BoardSpecifiers bs, int id1, int id2, int id3)
        {
            // Change this if more boards are added in the future:
            if (!Enum.IsDefined(typeof(Board.BoardSpecifiers), bs)) { throw new ArgumentException("Invalid boardSpecifier in addBoard()."); }

            Board? b = GetBoard(bs, id1, id2, id3);
            if (b == null)
            {
                b = new Board((int)bs, id1, id2, id3);
                using (var ctx = new binaireDbContext())
                {
                    ctx.Boards.Add(b);
                    ctx.SaveChanges();
                }
            }
            return b;
        }

        public static Reading? AddReading(Board b, int pufStart, int pufEnd, float temperature, byte[] fingerprint)
        {
            Reading r;
            try { r = new Reading(pufStart, pufEnd, temperature, fingerprint); }
            catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
            r.Board = b;

            using (var ctx = new binaireDbContext())
            {
                ctx.Boards.Attach(b);
                ctx.Readings.Add(r);
                ctx.SaveChanges();
            }
            return r;
        }

        public static Board? GetDefaultBoard()
        {
            using (var ctx = new binaireDbContext())
            {
                return ctx.Boards.Where(p => p.BoardId == 1).FirstOrDefault();
            }
        }


        public static Board? GetBoard(Board.BoardSpecifiers bs, int id1, int id2, int id3)
        {
            using (var ctx = new binaireDbContext())
            {
                Board? b = ctx.Boards.Where(p => p.BoardSpecifier == (int)bs
                                             && p.BoardId1 == id1
                                             && p.BoardId2 == id2
                                             && p.BoardId3 == id3).FirstOrDefault();
                return b;
            }
        }

        public static Reading? GetReadingByID(int id)
        {
            using (var ctx = new binaireDbContext())
            {
                return ctx.Readings.Where(p => p.ReadingId == id).FirstOrDefault();
            }
        }
    }
}
