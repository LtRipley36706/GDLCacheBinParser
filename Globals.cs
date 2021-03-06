using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.EntityFrameworkCore;

using PhatACCacheBinParser.Common;
using PhatACCacheBinParser.Properties;
using PhatACCacheBinParser.Seg1_RegionDescExtendedData;
using PhatACCacheBinParser.Seg2_SpellTableExtendedData;
using PhatACCacheBinParser.Seg3_TreasureTable;
using PhatACCacheBinParser.Seg4_CraftTable;
using PhatACCacheBinParser.Seg5_HousingPortals;
using PhatACCacheBinParser.Seg6_LandBlockExtendedData;
using PhatACCacheBinParser.Seg8_QuestDefDB;
using PhatACCacheBinParser.Seg9_WeenieDefaults;
using PhatACCacheBinParser.SegA_MutationFilters;
using PhatACCacheBinParser.SegB_GameEventDefDB;

using ACE.Database;
using ACE.Database.Models.World;
using ACE.Entity.Enum.Properties;

namespace PhatACCacheBinParser
{
    static class Globals
    {
        public static readonly ConcurrentDictionary<uint, string> WeenieClsNames = new ConcurrentDictionary<uint, string>();
        public static readonly ConcurrentDictionary<uint, string> WeenieNames = new ConcurrentDictionary<uint, string>();

        public static class CacheBin
        {
            public static bool IsLoaded;

            public static readonly RegionDescExtendedData RegionDescExtendedData = new RegionDescExtendedData();
            public static readonly SpellTableExtendedData SpellTableExtendedData = new SpellTableExtendedData();
            public static readonly TreasureTable TreasureTable = new TreasureTable();
            public static readonly CraftingTable CraftingTable = new CraftingTable();
            public static readonly HousingPortalsTable HousingPortalsTable = new HousingPortalsTable();
            public static readonly LandBlockData LandBlockData = new LandBlockData();
            // Segment 7
            public static readonly QuestDefDB QuestDefDB = new QuestDefDB();
            public static readonly WeenieDefaults WeenieDefaults = new WeenieDefaults();
            public static readonly MutationFilters MutationFilters = new MutationFilters();
            public static readonly GameEventDefDB GameEventDefDB = new GameEventDefDB();

            public static void AddToWeenieClsNames()
            {
                foreach (var weenie in WeenieDefaults.Weenies)
                {
                    //var className = weenie.Value.Description;
                    var className = "";

                    if (String.IsNullOrEmpty(className))
                    {
                        if (Enum.IsDefined(typeof(WCLASSID), (int)weenie.Value.WCID))
                            className = Enum.GetName(typeof(WCLASSID), weenie.Value.WCID).ToLower();
                        else if (Enum.IsDefined(typeof(WeenieClasses), (ushort)weenie.Value.WCID))
                        {
                            var clsName = Enum.GetName(typeof(WeenieClasses), weenie.Value.WCID).ToLower().Substring(2);
                            className = clsName.Substring(0, clsName.Length - 6);
                        }
                        else
                        {
                            var name = weenie.Value.StringValues.Where(x => x.Key == (int)PropertyString.Name).FirstOrDefault().Value;
                            if (!String.IsNullOrEmpty(name))
                                className = "ace" + weenie.Value.WCID.ToString() + "-" + name.Replace("'", "").Replace(" ", "").Replace(".", "").Replace("(", "").Replace(")", "").Replace("+", "").Replace(":", "").Replace("_", "").Replace("-", "").Replace(",", "").ToLower();
                        }

                        className = className.Replace("_", "-");
                    }

                    WeenieClsNames[weenie.Value.WCID] = className;
                }
            }

            public static void AddToWeenieNames()
            {
                foreach (var weenie in WeenieDefaults.Weenies)
                {
                    var name = weenie.Value.StringValues.Where(x => x.Key == (int)PropertyString.Name).FirstOrDefault().Value;
                    if (!String.IsNullOrEmpty(name))
                        WeenieNames[weenie.Value.WCID] = name;
                    else
                        WeenieNames[weenie.Value.WCID] = "ace" + weenie.Value.WCID.ToString();
                }
            }
        }

        public static class GDLE
        {
            public static bool IsLoaded;

            /// <summary>
            /// region.json + encounters.json
            /// </summary>
            //public static List<ACE.Database.Models.World.Encounter> Encounters;
            public static ACE.Adapter.GDLE.Models.Region Region;
            public static List<ACE.Adapter.GDLE.Models.TerrainData> TerrainData;

            /// <summary>
            /// events.json
            /// </summary>
            public static List<ACE.Database.Models.World.Event> Events;

            /// <summary>
            /// quests.json
            /// </summary>
            public static List<ACE.Database.Models.World.Quest> Quests;

            /// <summary>
            /// recipeprecursors.json
            /// </summary>
            public static List<ACE.Database.Models.World.CookBook> RecipePrecursors;

            /// <summary>
            /// recipes.json
            /// </summary>
            public static List<ACE.Database.Models.World.Recipe> Recipes;

            // restrictedlandblocks.json

            /// <summary>
            /// spells.json
            /// </summary>
            public static List<ACE.Database.Models.World.Spell> Spells;

            // treasureProfile.json

            /// <summary>
            /// wieldedtreasure.json
            /// </summary>
            public static List<ACE.Database.Models.World.TreasureWielded> WieldedTreasure;

            /// <summary>
            /// worldspawns.json
            /// </summary>
            public static List<ACE.Database.Models.World.LandblockInstance> Instances;

            /// <summary>
            /// worldspawns.json
            /// </summary>
            public static List<ACE.Database.Models.World.LandblockInstanceLink> Links;

            /// <summary>
            /// \weenies\*.json
            /// </summary>
            public static List<ACE.Database.Models.World.Weenie> Weenies;

            public static void AddToWeenieNames()
            {
                if (Weenies is null) return;

                foreach (var weenie in Weenies)
                {
                    string name = null;

                    foreach (var property in weenie.WeeniePropertiesString)
                    {
                        if (property.Type == (ushort)PropertyString.Name)
                        {
                            name = property.Value;
                            break;
                        }
                    }

                    if (String.IsNullOrEmpty(name))
                    {
                        //if (!WeenieClassNames.Values.TryGetValue(weenie.ClassId, out name))
                            name = "ace" + weenie.ClassId;
                    }

                    WeenieNames[weenie.ClassId] = name;
                }
            }
        }

        public static class ACEDatabase
        {
            /// <summary>
            /// This will be null if the database hasn't been init yet.<para />
            /// DO NOT SET THIS TO NULL. DO NOT DISPOSE OF IT. DO NOT WRAP IT IN A USING STATEMENT.
            /// </summary>
            public static WorldDbContext WorldDbContext;

            public static WorldDatabaseWithEntityCache WorldDatabase = new WorldDatabaseWithEntityCache();

            public static List<ACE.Database.Models.World.Weenie> Weenies;

            //public static List<ACE.Database.Models.World.LandblockInstance> LandblockInstances;

            public static bool TryInitWorldDatabaseContext()
            {
                try
                {
                    var optionsBuilder = new DbContextOptionsBuilder<WorldDbContext>();
                    optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");

                    WorldDbContext = new WorldDbContext(optionsBuilder.Options);

                    var result = WorldDatabase.GetWeenie(WorldDbContext, 1);

                    if (result != null)
                        return true;

                    WorldDbContext = null;
                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("TryInitWorldDatabaseContext failed with exception: " + Environment.NewLine + Environment.NewLine + ex);
                    return false;
                }
            }

            /// <summary>
            /// This will also update the Globals.WeenieNames dictionary
            /// </summary>
            public static void ReCacheAllWeeniesInParallel(bool updateWeenieNames = true)
            {
                WorldDatabase.ClearWeenieCache();

                var weenies = new List<ACE.Database.Models.World.Weenie>();

                var results = WorldDbContext.Weenie
                    .AsNoTracking()
                    .ToList();

                Parallel.ForEach(results, result =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<WorldDbContext>();
                    optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");

                    var worldDbContext = new WorldDbContext(optionsBuilder.Options);

                    var weenie = WorldDatabase.GetWeenie(worldDbContext, result.ClassId);

                    weenies.Add(weenie);

                    if (updateWeenieNames && weenie != null)
                    {
                        var name = weenie.GetProperty(PropertyString.Name);

                        if (!String.IsNullOrEmpty(name))
                            WeenieNames[weenie.ClassId] = name;
                    }
                });

                Weenies = weenies;
            }

            public static ACE.Database.Models.World.Weenie GetWeenie(uint weenieClassId, bool updateWeenieNames = true)
            {
                var optionsBuilder = new DbContextOptionsBuilder<WorldDbContext>();
                optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");

                var worldDbContext = new WorldDbContext(optionsBuilder.Options);

                var weenie = WorldDatabase.GetWeenie(worldDbContext, weenieClassId);

                if (updateWeenieNames && weenie != null)
                {
                    var name = weenie.GetProperty(PropertyString.Name);

                    if (!String.IsNullOrEmpty(name))
                        WeenieNames[weenie.ClassId] = name;
                }

                return weenie;
            }

            public static List<ACE.Database.Models.World.Weenie> GetAllWeenies()
            {
                var weenies = new List<ACE.Database.Models.World.Weenie>();

                var results = WorldDbContext.Weenie
                    .AsNoTracking()
                    .ToList();

                return results;
            }

            public static List<ACE.Database.Models.World.Weenie> GetAllWeeniesBetween(uint startWeenieClassId, uint endWeenieClassId)
            {
                var weenies = new List<ACE.Database.Models.World.Weenie>();

                var results = WorldDbContext.Weenie
                    .AsNoTracking()
                    .Where(w => w.ClassId >= startWeenieClassId && w.ClassId <= endWeenieClassId)
                    .ToList();

                return results;
            }

            public static List<ACE.Database.Models.World.LandblockInstance> GetAllLandblockInstances()
            {
                var landblocks = new List<ACE.Database.Models.World.LandblockInstance>();

                var results = WorldDbContext.LandblockInstance
                    .Include(r => r.LandblockInstanceLink)
                    .AsNoTracking()
                    .ToList();

                return results;
            }

            public static List<ACE.Database.Models.World.LandblockInstance> GetLandblockInstancesForLandblock(ushort landblock)
            {
                var optionsBuilder = new DbContextOptionsBuilder<WorldDbContext>();
                optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");

                var worldDbContext = new WorldDbContext(optionsBuilder.Options);

                var instance = WorldDatabase.GetCachedInstancesByLandblock(worldDbContext, landblock);

                return instance;
            }

            public static List<ACE.Database.Models.World.LandblockInstance> CloneLandblockToAnother(uint landblockToCloneFrom, uint landblockToCloneTo)
            {
                var optionsBuilder = new DbContextOptionsBuilder<WorldDbContext>();
                optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");

                var worldDbContext = new WorldDbContext(optionsBuilder.Options);

                var instances = WorldDatabase.GetCachedInstancesByLandblock(worldDbContext, (ushort)landblockToCloneFrom);

                var results = new List<ACE.Database.Models.World.LandblockInstance>();

                foreach (var instance in instances.Where(x => (x.ObjCellId >> 16) == landblockToCloneFrom).OrderBy(y => y.Guid))
                {
                    var newGuid = (instance.Guid & 0xF0000FFF) | (landblockToCloneTo << 12);
                    var newObjCellId = (instance.ObjCellId & 0x0000FFFF) | (landblockToCloneTo << 16);

                    var newInstance = new LandblockInstance
                    {
                        AnglesW = instance.AnglesW,
                        AnglesX = instance.AnglesX,
                        AnglesY = instance.AnglesY,
                        AnglesZ = instance.AnglesZ,
                        Guid = newGuid,
                        IsLinkChild = instance.IsLinkChild,
                        ObjCellId = newObjCellId,
                        OriginX = instance.OriginX,
                        OriginY = instance.OriginY,
                        OriginZ = instance.OriginZ,
                        WeenieClassId = instance.WeenieClassId,
                        LastModified = DateTime.UtcNow
                    };

                    foreach (var link in instance.LandblockInstanceLink)
                    {
                        var newChildGuid = (link.ChildGuid & 0xF0000FFF) | (landblockToCloneTo << 12);
                        newInstance.LandblockInstanceLink.Add(new LandblockInstanceLink { ChildGuid = newChildGuid, ParentGuid = newGuid, LastModified = DateTime.UtcNow });
                    }

                    results.Add(newInstance);
                }

                return results;
            }
        }
    }
}
