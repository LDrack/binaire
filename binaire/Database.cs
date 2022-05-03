using Microsoft.EntityFrameworkCore;

namespace binaire
{
    public static class Database
    {
        public class binaireDbContext : DbContext
        {
            // MySQL
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                var connectionString = @"server=smarttexserver.projekte.fh-hagenberg.at;database=DEPS_SRAM;uid=spectre;pwd=***REMOVED***;";
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
            if (bs != Board.BoardSpecifiers.NucleoF401RE) { throw new ArgumentException("Invalid boardSpecifier in addBoard()."); }

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
