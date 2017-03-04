using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edison.TickTackToe.Domain.DataAccess
{
    public class CustomCreateIfNotExist: CreateDatabaseIfNotExists<GameContext>
    {
        protected override void Seed(GameContext context)
        {
            base.Seed(context);
            DbSeeding.Seed(context);
        }
    }
}
