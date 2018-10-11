﻿using System;
using System.Diagnostics;
using Common.Database.Tables;
using Common.Globals;
using Common.Helpers;
using Shaolinq;
using Shaolinq.MySql;

namespace Common.Database
{
    public class DatabaseModel<T> where T : DataAccessModel
    {
        protected static T Model;

        public DatabaseModel()
        {
            var configuration = MySqlConfiguration.Create("wow", "127.0.0.1", "homestead", "secret");
            
            try
            {
                Model = DataAccessModel.BuildDataAccessModel<T>(configuration);
            }
            catch (Exception e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
            }
        }
    }

    [DataAccessModel]
    public abstract class Models : DataAccessModel
    {
        [DataAccessObjects]
        public abstract DataAccessObjects<Users> Users { get; }

        [DataAccessObjects]
        public abstract DataAccessObjects<Realms> Realms { get; }

        [DataAccessObjects]
        public abstract DataAccessObjects<Characters> Characters { get; }
    }

    public class DatabaseManager : DatabaseModel<Models>
    {
        public DatabaseManager()
        {
            // Recria a base inteira
            Model.Create(DatabaseCreationOptions.DeleteExistingDatabase);

            using (var scope = new DataAccessScope())
            {
                // Inserindo Usuarios
                var user = Model.Users.Create();
                user.name       = "John Doe";
                user.username   = "john";
                user.email      = "john@doe.com";
                user.password   = "doe";
                user.created_at = DateTime.Now;

                var user2 = Model.Users.Create();
                user2.name       = "Dabal Doe";
                user2.username   = "doe";
                user2.email      = "dabal@doe.com";
                user2.password   = "doe";
                user2.created_at = DateTime.Now;

                var user3 = Model.Users.Create();
                user3.name       = "John Doe";
                user3.username   = "ban";
                user3.email      = "john@doe.com";
                user3.password   = "doe";
                user3.created_at = DateTime.Now;
                user3.bannet_at  = DateTime.Now;

                // Inserindo Realm
                var realmPvp = Model.Realms.Create();
                realmPvp.flag       = RealmFlag.NewPlayers;
                realmPvp.timezone   = RealmTimezone.AnyLocale;
                realmPvp.type       = RealmType.Normal;
                realmPvp.name       = "Firetree";
                realmPvp.address    = "127.0.0.1:1001";
                realmPvp.created_at = DateTime.Now;

                scope.Complete();
            }
        }
    }
}
