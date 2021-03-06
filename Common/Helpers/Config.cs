﻿using System.IO;
using System.Xml.Serialization;

namespace Common.Helpers
{
    public class Config
    {
        [XmlIgnore] public const string FileName = "RealmServerConfig.xml";

        public string MongoConnection;
        public int LimitCharacterRealm;
        public string[] ProfaneNames;
        public int RangeDistanceLimit;

        [XmlIgnore] public static Config Instance { get; private set; }

        private void SetDefaultValues()
        {
            RangeDistanceLimit = 30;
            MongoConnection = "mongodb://127.0.0.1:27017";
            LimitCharacterRealm = 10;
            ProfaneNames = new[] {"root", "Admin", "Administrator"};
        }

        public static void Default()
        {
            Instance = new Config();
            Instance.SetDefaultValues();
        }

        public static void Load()
        {
            var serializer = new XmlSerializer(typeof(Config));

            using (var fStream = new FileStream(FileName, FileMode.Open))
            {
                Instance = (Config) serializer.Deserialize(fStream);
            }
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(Config));

            using (var fStream = new FileStream(FileName, FileMode.Create))
            {
                serializer.Serialize(fStream, this);
            }
        }
    }
}