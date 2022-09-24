extern alias MySqlConnectorAlias;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhatACCacheBinParser.ACE_Helpers;
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
using PhatACCacheBinParser.SQLWriters;

namespace PhatACCacheBinParser
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();


            // Cache.bin

			lblOutputFolder.Text = (string)Settings.Default["OutputFolder"];

			parserControl1.ProperyName = "_1";
			parserControl1.Label = "1 RegionDescExtendedData";
			parserControl1.DoExportJSON += ParserControl1_ExportJSON;

            parserControl2.ProperyName = "_2";
			parserControl2.Label = "2 SpellTableExtendedData";
			parserControl2.DoExportJSON += ParserControl2_ExportJSON;

            parserControl3.ProperyName = "_3";
			parserControl3.Label = "3 TreasureTable";
			parserControl3.DoExportJSON += ParserControl3_ExportJSON;

            parserControl4.ProperyName = "_4";
			parserControl4.Label = "4 CraftTable";
			parserControl4.DoExportJSON += ParserControl4_ExportJSON;

            parserControl5.ProperyName = "_5";
			parserControl5.Label = "5 HousingPortals";
			parserControl5.DoExportJSON += ParserControl5_ExportJSON;

            parserControl6.ProperyName = "_6";
			parserControl6.Label = "6 LandBlockExtendedData";
			parserControl6.DoExportJSON += ParserControl6_ExportJSON;

            parserControl7.ProperyName = "_7";
			parserControl7.Label = "7 Jumpsuits";
			parserControl7.DoExportJSON += ParserControl7_ExportJSON;

            parserControl8.ProperyName = "_8";
			parserControl8.Label = "8 QuestDefDB";
			parserControl8.DoExportJSON += ParserControl8_ExportJSON;

            parserControl9.ProperyName = "_9";
			parserControl9.Label = "9 WeenieDefaults";
			parserControl9.DoExportJSON += ParserControl9_ExportJSON;

            parserControlA.ProperyName = "_A";
			parserControlA.Label = "A MutationFilters";
			parserControlA.DoExportJSON += ParserControlA_ExportJSON;

            parserControlB.ProperyName = "_B";
            parserControlB.Label = "B GameEventDefDB";
            parserControlB.DoExportJSON += ParserControlB_ExportJSON;


            // GDLE

		    lblGDLEJSONRootFolder.Text = Settings.Default.GDLEJSONRootFolder;
		    lblGDLESQLOutputFolder.Text = Settings.Default.GDLESQLOutputFolder;


            // ACE

		    txtACEWorldServer.Text = Settings.Default.ACEWorldServer;
		    txtACEWorldPort.Text = Settings.Default.ACEWorldPort.ToString();
            txtACEWorldUser.Text = Settings.Default.ACEWorldUser;
		    txtACEWorldPassword.Text = Settings.Default.ACEWorldPassword;
		    txtACEWorldDatabase.Text = Settings.Default.ACEWorldDatabase;
            chkHidePassword.Checked = Settings.Default.HideACEWorldPassword;
		}

        protected override void OnClosing(CancelEventArgs e)
		{
			Settings.Default.Save();

			base.OnClosing(e);
		}


        // ====================================================================================
        // ==================================== Cache.bin =====================================
        // ====================================================================================

        private void cmdOutputFolder_Click(object sender, EventArgs e)
		{
			using (var dialog = new FolderBrowserDialog())
			{
				dialog.SelectedPath = lblOutputFolder.Text;

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					lblOutputFolder.Text = dialog.SelectedPath;
					Settings.Default["OutputFolder"] = lblOutputFolder.Text;
					Settings.Default.Save();
				}
			}
		}

		private void ParserControl1_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<RegionDescExtendedData>(parserControl);
		}

		private void ParserControl2_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<SpellTableExtendedData>(parserControl);
		}

		private void ParserControl3_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<TreasureTable>(parserControl);
		}

		private void ParserControl4_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<CraftingTable>(parserControl);
		}

		private void ParserControl5_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<HousingPortalsTable>(parserControl);
		}

		private void ParserControl6_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<LandBlockData>(parserControl);
		}

		private void ParserControl7_ExportJSON(ParserControl parserControl)
		{
			MessageBox.Show("Not implemented.");
		}

		private void ParserControl8_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<QuestDefDB>(parserControl);
		}

		private void ParserControl9_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<WeenieDefaults>(parserControl);
		}

		private void ParserControlA_ExportJSON(ParserControl parserControl)
		{
			ParserControl_ExportJSON<MutationFilters>(parserControl);
		}

        private void ParserControlB_ExportJSON(ParserControl parserControl)
        {
            ParserControl_ExportJSON<GameEventDefDB>(parserControl);
        }

        private void ParserControl_ExportJSON<T>(ParserControl parserControl) where T : Segment, new()
		{
			parserControl.Enabled = false;

			parserControl.ExportJSONProgress = 0;

			if (!File.Exists(parserControl.SourceBin))
			{
				MessageBox.Show("Source bin path does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				parserControl.Enabled = true;
				return;
			}

			if (!Directory.Exists(lblOutputFolder.Text))
			{
				MessageBox.Show("Output folder does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				parserControl.Enabled = true;
				return;
			}

			ThreadPool.QueueUserWorkItem(o =>
			{
				var data = File.ReadAllBytes(parserControl.SourceBin);

				// Parse the data
				using (var memoryStream = new MemoryStream(data))
				using (var binaryReader = new BinaryReader(memoryStream))
				{
					var outputFolder = lblOutputFolder.Text + "\\" + parserControl.Label + "\\" + "\\JSON\\";

					var segment = new T();

					parserControl.BeginInvoke((Action)(() => parserControl.ExportJSONProgress = 1));

					if (segment.Unpack(binaryReader))
					{
						parserControl.BeginInvoke((Action)(() => parserControl.ExportJSONProgress = 50));

                        // Export the parsed data
                        segment.WriteJSONOutput(outputFolder);
                        parserControl.BeginInvoke((Action)(() => parserControl.ExportJSONProgress = 100));
                    }
                }

                parserControl.BeginInvoke((Action)(() => parserControl.Enabled = true));
			});
		}


        // ====================================================================================
        // ======================================= GDLE =======================================
        // ====================================================================================

        private void cmdChooseJSONRootFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = lblGDLEJSONRootFolder.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    lblGDLEJSONRootFolder.Text = dialog.SelectedPath;
                    Settings.Default.GDLEJSONRootFolder = lblGDLEJSONRootFolder.Text;
                    Settings.Default.Save();
                }
            }
        }

        private void cmdChooseSQLOutputFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = lblGDLESQLOutputFolder.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    lblGDLESQLOutputFolder.Text = dialog.SelectedPath;
                    Settings.Default.GDLESQLOutputFolder = lblGDLESQLOutputFolder.Text;
                    Settings.Default.Save();
                }
            }
        }

        private void cmdParseGDLEJSONs_Click(object sender, EventArgs e)
        {
            cmdParseGDLEJSONs.Enabled = false;

            txtGDLEJSONParser.Text = null;

            try
            { 
                // Read in the GDLE jsons

                txtGDLEJSONParser.Text += "Loading events.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadEventsConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "events.json"), out Globals.GDLE.Events))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Events.Count} entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                txtGDLEJSONParser.Text += "Loading quests.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadQuestsConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "quests.json"), out Globals.GDLE.Quests))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Quests.Count} entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                var recipesprecursorsFound = File.Exists(Path.Combine(lblGDLEJSONRootFolder.Text, "recipeprecursors.json"));
                var recipesFound = File.Exists(Path.Combine(lblGDLEJSONRootFolder.Text, "recipes.json")); 

                txtGDLEJSONParser.Text += "Loading recipesprecursors.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadRecipePrecursorsConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "recipeprecursors.json"), out Globals.GDLE.RecipePrecursors))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.RecipePrecursors.Count} entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;
                
                txtGDLEJSONParser.Text += "Loading recipes.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadRecipesConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "recipes.json"), out Globals.GDLE.Recipes))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Recipes.Count} entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                if (!recipesprecursorsFound || !recipesFound)
                {
                    txtGDLEJSONParser.Text += "Loading \\recipes\\*.json...";
                    if (ACE.Adapter.GDLE.GDLELoader.TryLoadRecipeCombinedConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "recipes"), out Globals.GDLE.Recipes, out Globals.GDLE.RecipePrecursors))
                        txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Recipes.Count} recipes and {Globals.GDLE.RecipePrecursors.Count} cookbooks found." + Environment.NewLine;
                    else
                        txtGDLEJSONParser.Text += " failed." + Environment.NewLine;
                }

                // restrictedlandblocks.json

                txtGDLEJSONParser.Text += "Loading spells.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadSpellsConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "spells.json"), out Globals.GDLE.Spells))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Spells.Count} entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                // treasureProfile.json

                txtGDLEJSONParser.Text += "Loading wieldedtreasure.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadWieldedTreasureTableConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "wieldedtreasure.json"), out Globals.GDLE.WieldedTreasure))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.WieldedTreasure.Count} entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                var worldspawnsFound = false;
                txtGDLEJSONParser.Text += "Loading worldspawns.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadWorldSpawnsConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "worldspawns.json"), out Globals.GDLE.Instances, out Globals.GDLE.Links, 1000))
                {
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Instances.Count} instances and {Globals.GDLE.Links.Count} links found." + Environment.NewLine;
                    worldspawnsFound = true;
                }
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                if (!worldspawnsFound)
                {
                    txtGDLEJSONParser.Text += "Loading \\spawnMaps\\*.json...";
                    if (ACE.Adapter.GDLE.GDLELoader.TryLoadLandblocksConverted(Path.Combine(lblGDLEJSONRootFolder.Text, "spawnMaps"), out Globals.GDLE.Instances, out Globals.GDLE.Links, 1000))
                        txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Instances.Count} instances and {Globals.GDLE.Links.Count} links found." + Environment.NewLine;
                    else
                        txtGDLEJSONParser.Text += " failed." + Environment.NewLine;
                }

                txtGDLEJSONParser.Text += "Loading region.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadRegion(Path.Combine(lblGDLEJSONRootFolder.Text, "region.json"), out Globals.GDLE.Region))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Region.EncounterMap.Count} encounter maps and {Globals.GDLE.Region.Encounters.Count()} encounter tables found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                txtGDLEJSONParser.Text += "Loading encounters.json...";
                if (ACE.Adapter.GDLE.GDLELoader.TryLoadTerrainData(Path.Combine(lblGDLEJSONRootFolder.Text, "encounters.json"), out Globals.GDLE.TerrainData))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.TerrainData.Count} supplemental terrain data entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                txtGDLEJSONParser.Text += "Loading \\weenies\\*.json";
                if (ACE.Adapter.Lifestoned.LifestonedLoader.TryLoadWeeniesConvertedInParallel(Path.Combine(lblGDLEJSONRootFolder.Text, "weenies"), out Globals.GDLE.Weenies, chkGDLEApplyEnumShift.Checked))
                    txtGDLEJSONParser.Text += $" completed. {Globals.GDLE.Weenies.Count} entries found." + Environment.NewLine;
                else
                    txtGDLEJSONParser.Text += " failed." + Environment.NewLine;

                Globals.GDLE.IsLoaded = true;


                // Collect some meta data that we'll use to pretty up the SQL files

                Globals.GDLE.AddToWeenieNames();

                if (Globals.GDLE.Region != null && Globals.GDLE.Region.TableCount > 0
                    && Globals.GDLE.TerrainData != null && Globals.GDLE.TerrainData.Count > 0
                    && Globals.CacheBin.LandBlockData != null && Globals.CacheBin.LandBlockData.TerrainLandblocks.Count > 0)
                    cmdGDLE1RegionsParse.Enabled = true;
                if (Globals.GDLE.Spells != null && Globals.GDLE.Spells.Count > 0)
                    cmdGDLE2SpellsParse.Enabled = true;
                if (Globals.GDLE.WieldedTreasure != null && Globals.GDLE.WieldedTreasure.Count > 0)
                    cmdGDLE3TreasureParse.Enabled = true;
                if (Globals.GDLE.Recipes != null && Globals.GDLE.Recipes.Count > 0
                    && Globals.GDLE.RecipePrecursors != null && Globals.GDLE.RecipePrecursors.Count > 0)
                    cmdGDLE4CraftingParse.Enabled = true;
                if (Globals.GDLE.Instances != null && Globals.GDLE.Instances.Count > 0)
                    cmdGDLE6LandblocksParse.Enabled = true;
                if (Globals.GDLE.Quests != null && Globals.GDLE.Quests.Count > 0)
                    cmdGDLE8QuestsParse.Enabled = true;
                if (Globals.GDLE.Weenies != null && Globals.GDLE.Weenies.Count > 0)
                    cmdGDLE9WeeniesParse.Enabled = true;
                if (Globals.GDLE.Events != null && Globals.GDLE.Events.Count > 0)
                    cmdGDLEBEventsParse.Enabled = true;                
            }
            catch (Exception ex)
            {
                txtGDLEJSONParser.Text += Environment.NewLine + ex;
            }


            cmdParseGDLEJSONs.Enabled = true;
        }

        private void cmdGDLE1RegionsParse_Click(object sender, EventArgs e)
        {
            cmdGDLE1RegionsParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting region/encounters... please wait. ";

            var results = new List<ACE.Database.Models.World.Encounter>();

            var encounters = new Dictionary<int, List<ACE.Database.Models.World.Encounter>>();

            for (var landblock = 0; landblock < (255 * 255); landblock++)
            {
                var block_x = (landblock & 0xFF00) >> 8;
                var block_y = (landblock & 0x00FF) >> 0;

                var tbIndex = ((block_x * 255) + block_y);

                var terrain_base = Globals.CacheBin.LandBlockData.TerrainLandblocks[tbIndex];

                var terrain_base_patch = Globals.GDLE.TerrainData.Where(p => p.Key == landblock).FirstOrDefault();

                if (terrain_base_patch != null)
                {
                    var tD = new TerrainData();
                    tD.Terrain.AddRange(terrain_base_patch.Value);
                    terrain_base = tD;
                }

                if (terrain_base == null)
                    continue;

                for (var cell_x = 0; cell_x < 8; cell_x++)
                {
                    for (var cell_y = 0; cell_y < 8; cell_y++)
                    {
                        var terrain = terrain_base.Terrain[(cell_x * 9) + cell_y];

                        int encounterIndex = (terrain >> 7) & 0xF;

                        if (terrain_base_patch != null)
                            encounterIndex = terrain;

                        var encounterMap = Globals.GDLE.Region.EncounterMap[(block_x * 255) + block_y];
                        var encounterTable = Globals.GDLE.Region.Encounters.FirstOrDefault(t => t.Key == encounterMap);

                        if (encounterTable == null)
                            continue;

                        var wcid = encounterTable.Value[encounterIndex];


                        if (wcid > 0)
                        {
                            if (!encounters.ContainsKey(landblock))
                                encounters.Add(landblock, new List<ACE.Database.Models.World.Encounter>());

                            encounters[landblock].Add(new ACE.Database.Models.World.Encounter { Landblock = landblock, WeenieClassId = wcid, CellX = cell_x, CellY = cell_y });
                        }
                    }
                }
            }

            foreach (var kvp in encounters)
            {
                if (Globals.GDLE.TerrainData.FirstOrDefault(p => p.Key == kvp.Key) == null && chkCacheDedupe.Checked == true)
                    continue;

                foreach (var value in kvp.Value)
                {
                    value.LastModified = new System.DateTime(2005, 2, 9, 10, 00, 00);
                    results.Add(value);
                }
            }

            RegionDescSQLWriter.WriteFiles(results, Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\SQL\\", Globals.WeenieNames, true);

            txtGDLEJSONParser.Text += $"Successfully exported {encounters.Count} region/encounter instances." + Environment.NewLine;

            cmdGDLE1RegionsParse.Enabled = true;
        }

        private void cmdGDLE2SpellsParse_Click(object sender, EventArgs e)
        {
            cmdGDLE2SpellsParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting spells... please wait. ";

            foreach (var x in Globals.GDLE.Spells)
                x.LastModified = DateTime.UtcNow;

            SpellsSQLWriter.WriteFiles(Globals.GDLE.Spells, Settings.Default["GDLESQLOutputFolder"] + "\\2 SpellTableExtendedData\\SQL\\", Globals.WeenieNames, true);

            txtGDLEJSONParser.Text += $"Successfully exported {Globals.GDLE.Spells.Count} spells." + Environment.NewLine;

            cmdGDLE2SpellsParse.Enabled = true;
        }

        private void cmdGDLE3TreasureParse_Click(object sender, EventArgs e)
        {
            cmdGDLE3TreasureParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting wielded treasure... please wait. ";

            foreach (var x in Globals.GDLE.WieldedTreasure)
                x.LastModified = DateTime.UtcNow;

            var trimmedWieldedTreasure = new List<ACE.Database.Models.World.TreasureWielded>();

            var cachedWieldedTreasure = Globals.CacheBin.TreasureTable.WieldedTreasure;

            if (chkCacheDedupe.Checked)
            {
                foreach(var entry in Globals.GDLE.WieldedTreasure)
                {
                    if (!cachedWieldedTreasure.ContainsKey(entry.TreasureType))
                        trimmedWieldedTreasure.Add(entry);
                }
            }
            else
                trimmedWieldedTreasure = Globals.GDLE.WieldedTreasure;

            TreasureSQLWriter.WriteFiles(trimmedWieldedTreasure, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\SQL\\Wielded\\", Globals.WeenieNames, true);

            txtGDLEJSONParser.Text += $"Successfully exported {trimmedWieldedTreasure.Count} wielded tresure entries." + Environment.NewLine;

            cmdGDLE3TreasureParse.Enabled = true;
        }

        private void cmdGDLE4CraftingParse_Click(object sender, EventArgs e)
        {
            cmdGDLE4CraftingParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting recipes... please wait. ";

            foreach (var x in Globals.GDLE.RecipePrecursors)
                x.LastModified = DateTime.UtcNow;

            foreach (var x in Globals.GDLE.Recipes)
                x.LastModified = DateTime.UtcNow;

            //if (Globals.GDLE.Recipes.Count != Globals.GDLE.RecipePrecursors.Count)
            //    txtGDLEJSONParser.Text += $"Recipe({Globals.GDLE.Recipes.Count}) and Cookbook({Globals.GDLE.RecipePrecursors.Count}) counts do not match! This could be bad. ";

            CraftingSQLWriter.WriteFiles(Globals.GDLE.Recipes, Globals.GDLE.RecipePrecursors, Globals.WeenieNames, Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\SQL\\", true);

            txtGDLEJSONParser.Text += $"Successfully exported {Globals.GDLE.Recipes.Count} recipes & {Globals.GDLE.RecipePrecursors.Count} cookbooks." + Environment.NewLine;

            cmdGDLE4CraftingParse.Enabled = true;
        }

        private void cmdGDLE5HousingParse_Click(object sender, EventArgs e)
        {

        }

        private void cmdGDLE6LandblocksParse_Click(object sender, EventArgs e)
        {
            cmdGDLE6LandblocksParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting landblock instances and links... please wait. " + Environment.NewLine;

            var trimmedInstances = new List<ACE.Database.Models.World.LandblockInstance>();

            var margin = 4f;

            foreach (var x in Globals.GDLE.Instances)
            {
                x.LastModified = DateTime.UtcNow;

                foreach (var y in x.LandblockInstanceLink)
                    y.LastModified = DateTime.UtcNow;

                var lbid = (x.ObjCellId & 0xFFFF0000) >> 16;

                var rotate = new Quaternion(x.AnglesX, x.AnglesY, x.AnglesZ, x.AnglesW);
                if (Math.Abs(1 - rotate.Length()) > 0.001)
                {
                    // bad rotation data
                    txtGDLEJSONParser.Text += $"Warning: BAD ROTATION found for {(Globals.WeenieNames.TryGetValue(x.WeenieClassId, out var wname) ? wname + " " : "")}{x.Guid} - {x.WeenieClassId} - found in landblock 0x{lbid:X4} - W: {x.AnglesW} X: {x.AnglesX} Y: {x.AnglesY} Z: {x.AnglesZ} - defaulting to 1 0 0 0" + Environment.NewLine;

                    x.AnglesW = 1;
                    x.AnglesX = 0;
                    x.AnglesY = 0;
                    x.AnglesZ = 0;
                }

                var cachedLandblock = Globals.CacheBin.LandBlockData.Landblocks.Where(y => y.Key == lbid).FirstOrDefault();

                if (cachedLandblock != null && chkCacheDedupe.Checked)
                {
                    var foundInCache = cachedLandblock.Weenies
                        .Where(z => z.WCID == x.WeenieClassId
                                && Math.Abs(z.Position.Origin.X - x.OriginX) < margin
                                && Math.Abs(z.Position.Origin.Y - x.OriginY) < margin
                                && Math.Abs(z.Position.Origin.Z - x.OriginZ) < margin
                              )
                        .FirstOrDefault();

                    if (foundInCache == null)
                        trimmedInstances.Add(x);

                    //if (foundInCache != null & Globals.WeenieNames[x.WeenieClassId].ToLower().Contains("generator"))
                    //    trimmedInstances.Add(x);
                }
                else
                    trimmedInstances.Add(x);
            }

            LandblockSQLWriter.WriteFiles(trimmedInstances, Settings.Default["GDLESQLOutputFolder"] + "\\6 LandBlockExtendedData\\SQL\\", Globals.WeenieNames, false);

            txtGDLEJSONParser.Text += $"Successfully exported {trimmedInstances.Count} landblock instances and links. {(chkCacheDedupe.Checked ? "Unchanged entries from cache.bin were skipped." : "")}" + Environment.NewLine;

            cmdGDLE6LandblocksParse.Enabled = true;
        }

        private void cmdGDLE8QuestsParse_Click(object sender, EventArgs e)
        {
            cmdGDLE8QuestsParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting quests... please wait. ";

            var trimmedQuests = new List<ACE.Database.Models.World.Quest>();

            foreach (var x in Globals.GDLE.Quests)
            {
                x.LastModified = DateTime.UtcNow;

                var foundInCache = Globals.CacheBin.QuestDefDB.QuestDefs.Where(y => y.Name.ToLower() == x.Name.ToLower() && y.MaxSolves == x.MaxSolves && y.Message.ToLower() == x.Message.ToLower() && y.MinDelta == x.MinDelta).FirstOrDefault();

                if (foundInCache == null)
                    trimmedQuests.Add(x);
            }

            QuestSQLWriter.WriteFiles(trimmedQuests, Settings.Default["GDLESQLOutputFolder"] + "\\8 QuestDefDB\\SQL\\", true);

            txtGDLEJSONParser.Text += $"Successfully exported {trimmedQuests.Count} quests. Unchanged entries from cache.bin were skipped." + Environment.NewLine;

            cmdGDLE8QuestsParse.Enabled = true;
        }

        private void cmdGDLE9WeeniesParse_Click(object sender, EventArgs e)
        {
            cmdGDLE9WeeniesParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting weenies... please wait. ";

            foreach (var x in Globals.GDLE.Weenies)
            {
                x.LastModified = DateTime.UtcNow;

                foreach (var flo in x.WeeniePropertiesFloat)
                {
                    flo.Value = Math.Round(flo.Value, 3);
                }
            }

            var aceTreasureWielded = Globals.CacheBin.TreasureTable.WieldedTreasure.ConvertToACE();
            var aceTreasureDeath = Globals.CacheBin.TreasureTable.DeathTreasure.ConvertToACE();

            var treasureWielded = new Dictionary<uint, List<ACE.Database.Models.World.TreasureWielded>>();
            foreach (var item in aceTreasureWielded)
            {
                if (!treasureWielded.ContainsKey(item.TreasureType))
                    treasureWielded.Add(item.TreasureType, new List<ACE.Database.Models.World.TreasureWielded>());

                treasureWielded[item.TreasureType].Add(item);
            }
            var treasureDeath = new Dictionary<uint, ACE.Database.Models.World.TreasureDeath>();
            foreach (var item in aceTreasureDeath)
            {
                if (!treasureDeath.ContainsKey(item.TreasureType))
                    treasureDeath.Add(item.TreasureType, item);
            }

            WeenieSQLWriter.WriteFiles(Globals.GDLE.Weenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\SQL\\", Globals.WeenieNames, treasureWielded, treasureDeath, Globals.GDLE.Weenies.ToDictionary(x => x.ClassId, x => x), true);

            txtGDLEJSONParser.Text += $"Successfully exported {Globals.GDLE.Weenies.Count} weenies." + Environment.NewLine;

            cmdGDLE9WeeniesParse.Enabled = true;
        }

        private void cmdGDLEAMutationParse_Click(object sender, EventArgs e)
        {

        }

        private void cmdGDLEBEventsParse_Click(object sender, EventArgs e)
        {
            cmdGDLEBEventsParse.Enabled = false;

            txtGDLEJSONParser.Text += "Exporting events... please wait. ";

            var trimmedEvents = new List<ACE.Database.Models.World.Event>();

            foreach (var x in Globals.GDLE.Events)
            {
                x.LastModified = DateTime.UtcNow;

                var foundInCache = Globals.CacheBin.GameEventDefDB.GameEventDefs.Where(y => y.Name.ToLower() == x.Name.ToLower() && (int)y.GameEventState == x.State && y.StartTime == x.StartTime && y.EndTime == x.EndTime).FirstOrDefault();

                if (foundInCache == null)
                    trimmedEvents.Add(x);
            }

            EventSQLWriter.WriteFiles(trimmedEvents, Settings.Default["GDLESQLOutputFolder"] + "\\B GameEventDefDB\\SQL\\", true);

            txtGDLEJSONParser.Text += $"Successfully exported {trimmedEvents.Count} events. Unchanged entries from cache.bin were skipped." + Environment.NewLine;

            cmdGDLEBEventsParse.Enabled = true;
        }


        // ====================================================================================
        // =================================== ACE Database ===================================
        // ====================================================================================

        private void txtACEWorldServer_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.ACEWorldServer = txtACEWorldServer.Text;
            Settings.Default.Save();
        }

        private void txtACEWorldPort_TextChanged(object sender, EventArgs e)
        {
            if (ushort.TryParse(txtACEWorldPort.Text, out var port))
            {
                Settings.Default.ACEWorldPort = port;
                Settings.Default.Save();
            }
        }

        private void txtACEWorldUser_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.ACEWorldUser = txtACEWorldUser.Text;
            Settings.Default.Save();
        }

        private void txtACEWorldPassword_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.ACEWorldPassword = txtACEWorldPassword.Text;
            Settings.Default.Save();
        }

        private void txtACEWorldDatabase_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.ACEWorldDatabase = txtACEWorldDatabase.Text;
            Settings.Default.Save();
        }

        private void cmdTestInitDatabaseConnection_Click(object sender, EventArgs e)
        {
            if (Globals.ACEDatabase.TryInitWorldDatabaseContext())
            {
                txtACEDatabaseConnector.Text += "Connection succeeded!" + Environment.NewLine;

                cmdACE1RegionsParse.Enabled = true;
                cmdACE2SpellsParse.Enabled = true;
                cmdACE3TreasureParse.Enabled = true;
                cmdACE4CraftingParse.Enabled = true;
                cmdACE5HousingParse.Enabled = true;
                cmdACE6LandblocksParse.Enabled = true;
                cmdACE8QuestsParse.Enabled = true;
                cmdACE9WeeniesParse.Enabled = true;
                cmdACEAMutationParse.Enabled = true;
                cmdACEBEventsParse.Enabled = true;

                cmdACEExportWeenieAsJson.Enabled = true;
            }
            else
                txtACEDatabaseConnector.Text += "Connection failed." + Environment.NewLine;
        }

	    private void cmdACEDatabaseCacheAllWeenies_Click(object sender, EventArgs e)
	    {
	        if (Globals.ACEDatabase.WorldDbContext == null)
	        {
	            txtACEDatabaseConnector.Text += "You must Test/Init Database Connection first." + Environment.NewLine;
                return;
	        }

	        cmdACEDatabaseCacheAllWeenies.Enabled = false;

            txtACEDatabaseConnector.Text += "Caching all weenies in parallel. This may take several minutes and consume lots of CPU...";
	        txtACEDatabaseConnector.Refresh();
            Globals.ACEDatabase.ReCacheAllWeeniesInParallel();
            //txtACEDatabaseConnector.Text += $" completed. {Globals.ACEDatabase.WorldDatabase.GetWeenieCacheCount():N0} weenies cached." + Environment.NewLine;
            txtACEDatabaseConnector.Text += $" completed. {Globals.ACEDatabase.WorldDbContext.Weenie.Count():N0} weenies cached." + Environment.NewLine;

            cmdACEDatabaseCacheAllWeenies.Enabled = true;
        }

        private void chkHidePassword_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.HideACEWorldPassword = chkHidePassword.Checked;
            Settings.Default.Save();
            txtACEWorldPassword.UseSystemPasswordChar = chkHidePassword.Checked;
        }

        private bool writeDeletedFiles = false;

        private void cmdACE1RegionsParse_Click(object sender, EventArgs e)
        {
            cmdACE1RegionsParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting regions from database... ";

            var cacheRegion = Globals.CacheBin.RegionDescExtendedData.ConvertToACE(Globals.CacheBin.LandBlockData);

            Globals.ACEDatabase.WorldDbContext.Encounter.Load();
            var results = Globals.ACEDatabase.WorldDbContext.Encounter.ToList();

            DeDupeRegions(cacheRegion, results, out var deDupedEncounters);

            //foreach (var thing in deDupedEncounters)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedEncounters.Count > 0)
                //RegionDescSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\SQL\\", Globals.WeenieNames, true);
                RegionDescSQLWriter.WriteFiles(deDupedEncounters, Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\", Globals.WeenieNames, true);

            var cacheLbs = cacheRegion.GroupBy(x => x.Landblock).Select(x => x.First().Landblock).ToHashSet();
            var encounterLbs = results.GroupBy(x => x.Landblock).Select(x => x.First().Landblock).ToHashSet();
            var deDupeLbs = deDupedEncounters.GroupBy(x => x.Landblock).Select(x => x.First().Landblock).ToHashSet();

            var deletedLbs = cacheLbs.Except(encounterLbs).Except(deDupeLbs).ToHashSet();

            if (writeDeletedFiles && deletedLbs.Count > 0)
            {
                var sqlWriter = new ACE.Database.SQLFormatters.World.EncounterSQLWriter();

                sqlWriter.WeenieNames = Globals.WeenieNames;

                //Parallel.ForEach(sortedInput, kvp =>
                ////foreach (var kvp in sortedInput)
                //{
                //    string fileName = sqlWriter.GetDefaultFileName(kvp.Value[0]);

                //    using (StreamWriter writer = new StreamWriter(outputFolder + fileName))
                //    {
                //        if (includeDELETEStatementBeforeInsert)
                //        {
                //            sqlWriter.CreateSQLDELETEStatement(kvp.Value, writer);
                //            writer.WriteLine();
                //        }

                //        sqlWriter.CreateSQLINSERTStatement(kvp.Value, writer);
                //    }
                //});

                foreach (var lb in deletedLbs)
                {
                    var thing = new List<ACE.Database.Models.World.Encounter> { new ACE.Database.Models.World.Encounter { Landblock = lb } };

                    string fileName = sqlWriter.GetDefaultFileName(thing[0]);

                    //using (StreamWriter writer = new StreamWriter(Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\SQL\\" + fileName.Replace(".sql", " - DELETED.sql")))
                    using (StreamWriter writer = new StreamWriter(Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\" + fileName.Replace(".sql", " - DELETED.sql")))
                    {
                        sqlWriter.CreateSQLDELETEStatement(thing, writer);
                        //writer.WriteLine();
                    }
                }
            }

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.Encounter.Load();
                var prevEncounters = prevContext.Encounter.ToList();

                DeDupeRegions(prevEncounters, deDupedEncounters, out deDupedEncounters);

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedEncounters)
                        thing.LastModified = GetTimestampForExport();
                }
            }

            if (deDupedEncounters.Count > 0)
                //RegionDescSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\SQL\\", Globals.WeenieNames, true);
                RegionDescSQLWriter.WriteFiles(deDupedEncounters, Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\", Globals.WeenieNames, true);

            txtACEDatabaseConnector.Text += $" completed. {deDupedEncounters.Count:N0} encounters for a combined total of {deDupedEncounters.GroupBy(x => x.Landblock).Select(g => g.First()).ToList().Count:N0} regions exported." + Environment.NewLine;

            //txtACEDatabaseConnector.Text += $"Skipped {cacheRegion.Count - deDuped.Count:N0} encounters for a combined total of {cacheRegion.GroupBy(x => x.Landblock).Select(g => g.First()).ToList().Count - deDuped.GroupBy(x => x.Landblock).Select(g => g.First()).ToList().Count:N0} unchanged regions." + Environment.NewLine;

            //var z = cacheRegion.Where(x => !deDupeLbs.Contains(x.Landblock)).ToList();
            txtACEDatabaseConnector.Text += $"Skipped {cacheRegion.Where(x => !deDupeLbs.Contains(x.Landblock)).ToList().Count:N0} encounters for a combined total of {cacheLbs.Except(deDupeLbs).Except(deletedLbs).Count():N0} unchanged regions." + Environment.NewLine;

            cmdACE1RegionsParse.Enabled = true;
        }

        private DateTime? timestamp = null;
        private DateTime GetTimestampForExport()
        {
            if (timestamp != null)
                return timestamp.Value;
            else
            {
                timestamp = DateTime.UtcNow;

                return timestamp.Value;
            }
        }

        private bool usePrevVersion = false;

        private void DeDupeRegions(List<ACE.Database.Models.World.Encounter> cacheEncounters, List<ACE.Database.Models.World.Encounter> encounters, out List<ACE.Database.Models.World.Encounter> deDupedEncounters)
        {
            ////var cacheWeenies = Globals.CacheBin.WeenieDefaults.ConvertToACE();

            ////Globals.ACEDatabase.ReCacheAllWeeniesInParallel();

            //var cacheRegion = Globals.CacheBin.RegionDescExtendedData.ConvertToACE(Globals.CacheBin.LandBlockData);

            ////var results = Globals.ACEDatabase.WorldDbContext.Encounter
            ////        .AsNoTracking()
            ////        .ToList();

            //Globals.ACEDatabase.WorldDbContext.Encounter.Load();
            //var results = Globals.ACEDatabase.WorldDbContext.Encounter.ToList();

            var x = new Dictionary<int, List<ACE.Database.Models.World.Encounter>>();

            foreach (var r in cacheEncounters)
            {
                if (!x.TryAdd(r.Landblock, new List<ACE.Database.Models.World.Encounter> { r }))
                    x[r.Landblock].Add(r);
            }

            var y = new Dictionary<int, List<ACE.Database.Models.World.Encounter>>();

            foreach (var r in encounters)
            {
                if (!y.TryAdd(r.Landblock, new List<ACE.Database.Models.World.Encounter> { r }))
                    y[r.Landblock].Add(r);
            }

            deDupedEncounters = new List<ACE.Database.Models.World.Encounter>();

            var deDupedIndex = new HashSet<int>();

            foreach (var q in encounters)
            {
                //q.LastModified = DateTime.Now;

                if (!x.ContainsKey(q.Landblock))
                {
                    //deDuped.Add(q);
                    deDupedIndex.Add(q.Landblock);
                }
                else
                {
                    if (deDupedIndex.Contains(q.Landblock))
                        continue;

                    var a = x[q.Landblock];
                    var b = y[q.Landblock];

                    //if (deDupedIndex.Contains(q.Landblock))
                    //deDuped.Add(q);
                    //else if (a.Count != b.Count)
                    if (a.Count != b.Count)
                    {
                        //deDuped.Add(q);
                        deDupedIndex.Add(q.Landblock);
                    }
                    else
                    {
                        //if (b[0].AnglesW != a[0].AnglesW || b[0].AnglesX != a[0].AnglesX || b[0].AnglesY != a[0].AnglesY || b[0].AnglesZ != a[0].AnglesZ || b[0].ObjCellId != a[0].ObjCellId
                        //    || b[0].OriginX != a[0].OriginX || b[0].OriginY != a[0].OriginY || b[0].OriginZ != a[0].OriginZ)
                        //{
                        //    deDuped.Add(q);
                        //}
                        //else if (b.Count == a.Count && b.Count == 2)
                        //{
                        //    if (b[1].AnglesW != a[1].AnglesW || b[1].AnglesX != a[1].AnglesX || b[1].AnglesY != a[1].AnglesY || b[1].AnglesZ != a[1].AnglesZ || b[1].ObjCellId != a[1].ObjCellId
                        //    || b[1].OriginX != a[1].OriginX || b[1].OriginY != a[1].OriginY || b[1].OriginZ != a[1].OriginZ)
                        //    {
                        //        deDuped.Add(q);
                        //    }
                        //}
                        var c = a.ToDictionary(e => (e.CellX, e.CellY), e => e.WeenieClassId);
                        var d = b.ToDictionary(e => (e.CellX, e.CellY), e => e.WeenieClassId);

                        var key = (q.CellX, q.CellY);

                        //if (!c.ContainsKey(key))
                        //    deDuped.Add(q);
                        //else if (c[key] != q.WeenieClassId)
                        //    deDuped.Add(q);
                        //else if (d.ContainsKey(key))
                        //    deDuped.Add(q);
                        //if (deDupedIndex.Contains(q.Landblock))
                        //    deDuped.Add(q);
                        //else if (!c.ContainsKey(key) || c[key] != q.WeenieClassId)
                        if (!c.ContainsKey(key) || c[key] != q.WeenieClassId)
                        {
                            //deDuped.Add(q);
                            deDupedIndex.Add(q.Landblock);
                        }
                    }
                }
            }

            //deDuped.Clear();

            foreach (var q in encounters)
            {
                //q.LastModified = DateTime.Now;

                if (deDupedIndex.Contains(q.Landblock))
                    deDupedEncounters.Add(q);
            }
        }

        private void cmdACE2SpellsParse_Click(object sender, EventArgs e)
        {
            cmdACE2SpellsParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting spells from database... ";

            var cacheSpells = Globals.CacheBin.SpellTableExtendedData.ConvertToACE();

            Globals.ACEDatabase.WorldDbContext.Spell.Load();
            var results = Globals.ACEDatabase.WorldDbContext.Spell.ToList();

            DeDupeSpells(cacheSpells, results, out var deDupedSpells);

            //foreach (var thing in deDupedSpells)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedSpells.Count > 0)
                //SpellsSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\2 SpellTableExtendedData\\SQL\\", Globals.WeenieNames, true);
                SpellsSQLWriter.WriteFiles(deDupedSpells, Settings.Default["GDLESQLOutputFolder"] + "\\2 SpellTableExtendedData\\", Globals.WeenieNames, true);

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.Spell.Load();
                var prevSpells = prevContext.Spell.ToList();

                DeDupeSpells(prevSpells, deDupedSpells, out deDupedSpells);

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedSpells)
                        thing.LastModified = GetTimestampForExport();
                }
            }

            if (deDupedSpells.Count > 0)
                //SpellsSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\2 SpellTableExtendedData\\SQL\\", Globals.WeenieNames, true);
                SpellsSQLWriter.WriteFiles(deDupedSpells, Settings.Default["GDLESQLOutputFolder"] + "\\2 SpellTableExtendedData\\", Globals.WeenieNames, true);

            var cacheIds = cacheSpells.Select(x => x.Id).ToHashSet();
            var spellIds = results.Select(x => x.Id).ToHashSet();
            var deDupeIds = deDupedSpells.Select(x => x.Id).ToHashSet();

            var deletedIds = cacheIds.Except(spellIds).Except(deDupeIds).ToHashSet();

            txtACEDatabaseConnector.Text += $" completed. {deDupedSpells.Count:N0} spells exported." + Environment.NewLine;

            txtACEDatabaseConnector.Text += $"Skipped {cacheIds.Except(deDupeIds).Except(deletedIds).Count():N0} unchanged spells." + Environment.NewLine;

            cmdACE2SpellsParse.Enabled = true;
        }

        private void DeDupeSpells(List<ACE.Database.Models.World.Spell> cacheSpells, List<ACE.Database.Models.World.Spell> spells, out List<ACE.Database.Models.World.Spell> deDupedSpells)
        {
            var x = cacheSpells.ToDictionary(z => z.Id, z => z);

            deDupedSpells = new List<ACE.Database.Models.World.Spell>();

            foreach (var q in spells)
            {
                if (x.ContainsKey(q.Id))
                {
                    var s = x[q.Id];

                    if (q.Align != s.Align
                        || q.BaseIntensity != s.BaseIntensity
                        || q.Boost != s.Boost
                        || q.BoostVariance != s.BoostVariance
                        || !ApproximatelyEqual(q.CreateOffsetOriginX, s.CreateOffsetOriginX) //q.CreateOffsetOriginX != s.CreateOffsetOriginX
                        || !ApproximatelyEqual(q.CreateOffsetOriginY, s.CreateOffsetOriginY) //q.CreateOffsetOriginY != s.CreateOffsetOriginY
                        || !ApproximatelyEqual(q.CreateOffsetOriginZ, s.CreateOffsetOriginZ) //q.CreateOffsetOriginZ != s.CreateOffsetOriginZ
                        || q.CritFreq != s.CritFreq
                        || q.CritMultiplier != s.CritMultiplier
                        || !ApproximatelyEqual(q.DamageRatio, s.DamageRatio) //q.DamageRatio != s.DamageRatio
                        || q.DamageType != s.DamageType
                        || !ApproximatelyEqual(q.DefaultLaunchAngle, s.DefaultLaunchAngle) //q.DefaultLaunchAngle != s.DefaultLaunchAngle
                        || q.Destination != s.Destination
                        || !ApproximatelyEqual(q.DimsOriginX, s.DimsOriginX) //q.DimsOriginX != s.DimsOriginX
                        || !ApproximatelyEqual(q.DimsOriginY, s.DimsOriginY) //q.DimsOriginY != s.DimsOriginY
                        || !ApproximatelyEqual(q.DimsOriginY, s.DimsOriginY) //q.DimsOriginY != s.DimsOriginZ
                        || q.DispelSchool != s.DispelSchool
                        || q.DotDuration != s.DotDuration
                        || !ApproximatelyEqual(q.DrainPercentage, s.DrainPercentage) //q.DrainPercentage != s.DrainPercentage
                        || q.ElementalModifier != s.ElementalModifier
                        || q.EType != s.EType
                        || q.IgnoreMagicResist != s.IgnoreMagicResist
                        || q.ImbuedEffect != s.ImbuedEffect
                        || q.Index != s.Index
                        || q.Link != s.Link
                        || !ApproximatelyEqual(q.LossPercent, s.LossPercent) //q.LossPercent != s.LossPercent
                        || q.MaxBoostAllowed != s.MaxBoostAllowed
                        || q.MaxPower != s.MaxPower
                        || q.MinPower != s.MinPower
                        || q.Name != s.Name
                        || q.NonTracking != s.NonTracking
                        || q.Number != s.Number
                        || !ApproximatelyEqual(q.NumberVariance, s.NumberVariance) //q.NumberVariance != s.NumberVariance
                        || q.NumProjectiles != s.NumProjectiles
                        || q.NumProjectilesVariance != s.NumProjectilesVariance
                        || !ApproximatelyEqual(q.PaddingOriginX, s.PaddingOriginX) //q.PaddingOriginX != s.PaddingOriginX
                        || !ApproximatelyEqual(q.PaddingOriginX, s.PaddingOriginX) //q.PaddingOriginX != s.PaddingOriginY
                        || !ApproximatelyEqual(q.PaddingOriginZ, s.PaddingOriginZ) //q.PaddingOriginZ != s.PaddingOriginZ
                        || !ApproximatelyEqual(q.PeturbationOriginX, s.PeturbationOriginX) //q.PeturbationOriginX != s.PeturbationOriginX
                        || !ApproximatelyEqual(q.PeturbationOriginY, s.PeturbationOriginY) //q.PeturbationOriginY != s.PeturbationOriginY
                        || !ApproximatelyEqual(q.PeturbationOriginZ, s.PeturbationOriginZ) // q.PeturbationOriginZ != s.PeturbationOriginZ
                        || !ApproximatelyEqual(q.PositionAnglesW, s.PositionAnglesW) //q.PositionAnglesW != s.PositionAnglesW
                        || !ApproximatelyEqual(q.PositionAnglesX, s.PositionAnglesX) //q.PositionAnglesX != s.PositionAnglesX
                        || !ApproximatelyEqual(q.PositionAnglesY, s.PositionAnglesY) //q.PositionAnglesY != s.PositionAnglesY
                        || !ApproximatelyEqual(q.PositionAnglesZ, s.PositionAnglesZ) //q.PositionAnglesZ != s.PositionAnglesZ
                        || q.PositionObjCellId != s.PositionObjCellId
                        || !ApproximatelyEqual(q.PositionOriginX, s.PositionOriginX) //q.PositionOriginX != s.PositionOriginX
                        || !ApproximatelyEqual(q.PositionOriginY, s.PositionOriginY) //q.PositionOriginY != s.PositionOriginY
                        || !ApproximatelyEqual(q.PositionOriginZ, s.PositionOriginZ) //q.PositionOriginZ != s.PositionOriginZ
                        || !ApproximatelyEqual(q.PowerVariance, s.PowerVariance) //q.PowerVariance != s.PowerVariance
                        || !ApproximatelyEqual(q.Proportion, s.Proportion) //q.Proportion != s.Proportion
                        || q.SlayerCreatureType != s.SlayerCreatureType
                        || !ApproximatelyEqual(q.SlayerDamageBonus, s.SlayerDamageBonus) //q.SlayerDamageBonus != s.SlayerDamageBonus
                        || q.Source != s.Source
                        || q.SourceLoss != s.SourceLoss
                        || !ApproximatelyEqual(q.SpreadAngle, s.SpreadAngle) //q.SpreadAngle != s.SpreadAngle
                        || q.StatModKey != s.StatModKey
                        || q.StatModType != s.StatModType
                        || !ApproximatelyEqual(q.StatModVal, s.StatModVal) //q.StatModVal != s.StatModVal
                        || q.TransferBitfield != s.TransferBitfield
                        || q.TransferCap != s.TransferCap
                        || q.Variance != s.Variance
                        || !ApproximatelyEqual(q.VerticalAngle, s.VerticalAngle) //q.VerticalAngle != s.VerticalAngle
                        || q.Wcid != s.Wcid
                        )
                        deDupedSpells.Add(q);
                }
                else
                    deDupedSpells.Add(q);
            }
        }

        public static bool ApproximatelyEqual(float? a, float? b, int places = 6)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            //const float floatNormal = (1 << 23) * float.Epsilon;
            //float absA = Math.Abs(a.Value);
            //float absB = Math.Abs(b.Value);
            //float diff = Math.Abs(a.Value - b.Value);

            //if (a == b)
            //{
            //    // Shortcut, handles infinities
            //    return true;
            //}

            //if (a == 0.0f || b == 0.0f || diff < floatNormal)
            //{
            //    // a or b is zero, or both are extremely close to it.
            //    // relative error is less meaningful here
            //    return diff < (epsilon * floatNormal);
            //}

            //// use relative error
            //return diff / Math.Min((absA + absB), float.MaxValue) < epsilon;

            var x = Math.Abs(a.Value).ToString($"0.{new string('#', places)}");
            var y = Math.Abs(b.Value).ToString($"0.{new string('#', places)}");

            //if ((x == $"-0.{new string('0', places)}" && y == $"0.{new string('0', places)}")
            //    || (x == $"0.{new string('0', places)}" && y == $"-0.{new string('0', places)}")
            //    || (x == "0" && y == "-0")
            //    || (x == "-0" && y == "0"))
            //    return true;

            return x.Equals(y);
        }

        private void cmdACE3TreasureParse_Click(object sender, EventArgs e)
        {
            cmdACE3TreasureParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting Treasure from database... ";

            var cacheTreasureDeath = Globals.CacheBin.TreasureTable.DeathTreasure.ConvertToACE();
            var cacheTreasureWielded = Globals.CacheBin.TreasureTable.WieldedTreasure.ConvertToACE();

            var treasureDeath = Globals.ACEDatabase.GetAllTreasureDeath();
            var treasureWielded = Globals.ACEDatabase.GetAllTreasureWielded();

            DeDupeTreasureDeath(cacheTreasureDeath, treasureDeath, out var deDupedTreasureDeath);
            DeDupeTreasureWielded(cacheTreasureWielded, treasureWielded, out var deDupedTreasureWielded);

            //foreach (var thing in deDupedTreasureDeath)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            //foreach (var thing in deDupedTreasureWielded)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedTreasureDeath.Count > 0)
                //TreasureSQLWriter.WriteFiles(deDupedTreasureDeath, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\SQL\\Death\\", true);
                TreasureSQLWriter.WriteFiles(deDupedTreasureDeath, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\Death\\", true);
            if (deDupedTreasureWielded.Count > 0)
                //TreasureSQLWriter.WriteFiles(deDupedTreasureWielded, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\SQL\\Wielded\\", Globals.WeenieNames, true);
                TreasureSQLWriter.WriteFiles(deDupedTreasureWielded, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\Wielded\\", Globals.WeenieNames, true);

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.TreasureDeath.Load();
                prevContext.TreasureWielded.Load();
                var prevTreasureDeath = prevContext.TreasureDeath.ToList();
                var prevTreasureWielded = prevContext.TreasureWielded.ToList();

                DeDupeTreasureDeath(prevTreasureDeath, deDupedTreasureDeath, out deDupedTreasureDeath);
                DeDupeTreasureWielded(prevTreasureWielded, deDupedTreasureWielded, out deDupedTreasureWielded);

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedTreasureDeath)
                        thing.LastModified = GetTimestampForExport();
                    foreach (var thing in deDupedTreasureWielded)
                        thing.LastModified = GetTimestampForExport();
                }
            }

            if (deDupedTreasureDeath.Count > 0)
                //TreasureSQLWriter.WriteFiles(deDupedTreasureDeath, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\SQL\\Death\\", true);
                TreasureSQLWriter.WriteFiles(deDupedTreasureDeath, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\Death\\", true);
            if (deDupedTreasureWielded.Count > 0)
                //TreasureSQLWriter.WriteFiles(deDupedTreasureWielded, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\SQL\\Wielded\\", Globals.WeenieNames, true);
                TreasureSQLWriter.WriteFiles(deDupedTreasureWielded, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\Wielded\\", Globals.WeenieNames, true);

            var cacheTDIds = cacheTreasureDeath.Select(x => x.TreasureType).ToHashSet();
            var cacheTWIds = cacheTreasureWielded.Select(x => x.TreasureType).ToHashSet();
            var tdIds = treasureDeath.Select(x => x.TreasureType).ToHashSet();
            var twIds = treasureWielded.Select(x => x.TreasureType).ToHashSet();
            var deDupeTDIds = deDupedTreasureDeath.Select(x => x.TreasureType).ToHashSet();
            var deDupeTWIds = deDupedTreasureWielded.Select(x => x.TreasureType).ToHashSet();

            var deletedTDIds = cacheTDIds.Except(tdIds).Except(deDupeTDIds).ToHashSet();
            var deletedTWIds = cacheTWIds.Except(twIds).Except(deDupeTWIds).ToHashSet();

            txtACEDatabaseConnector.Text += $" completed. {deDupedTreasureDeath.Count:N0} death treasure and {deDupedTreasureWielded.GroupBy(x => x.TreasureType).Select(g => g.First()).ToList().Count:N0} wielded treasure exported." + Environment.NewLine;

            txtACEDatabaseConnector.Text += $"Skipped {cacheTDIds.Except(deDupeTDIds).Except(deletedTDIds).Count():N0} unchanged death treasure and {cacheTWIds.Except(deDupeTWIds).Except(deletedTWIds).Count():N0} unchanged wielded treasure." + Environment.NewLine;

            cmdACE3TreasureParse.Enabled = true;
        }

        private void DeDupeTreasureWielded(List<ACE.Database.Models.World.TreasureWielded> cacheTreasureWielded, List<ACE.Database.Models.World.TreasureWielded> treasureWielded, out List<ACE.Database.Models.World.TreasureWielded> deDupedTreasureWielded)
        {
            deDupedTreasureWielded = new List<ACE.Database.Models.World.TreasureWielded>();

            var ids = treasureWielded.GroupBy(x => x.TreasureType).Select(y => y.First()).Select(z => z.TreasureType).ToHashSet();

            foreach (var x in ids)
            {
                var a = cacheTreasureWielded.Where(y => y.TreasureType == x).ToList();

                if (a == null)
                    deDupedTreasureWielded.AddRange(treasureWielded.Where(y => y.TreasureType == x));
                else
                {
                    var b = treasureWielded.Where(y => y.TreasureType == x).ToList();

                    if (a.Count != b.Count)
                        deDupedTreasureWielded.AddRange(b);
                    else
                    {
                        for (var y = 0; y < b.Count; y++)
                        {
                            if (
                                   b[y].ContinuesPreviousSet != a[y].ContinuesPreviousSet
                                || b[y].HasSubSet != a[y].HasSubSet
                                || b[y].PaletteId != a[y].PaletteId
                                || b[y].Probability != a[y].Probability
                                || b[y].SetStart != a[y].SetStart
                                || b[y].Shade != a[y].Shade
                                || b[y].StackSize != a[y].StackSize
                                || b[y].StackSizeVariance != a[y].StackSizeVariance
                                || b[y].Unknown1 != a[y].Unknown1
                                || b[y].Unknown10 != a[y].Unknown10
                                || b[y].Unknown11 != a[y].Unknown11
                                || b[y].Unknown12 != a[y].Unknown12
                                || b[y].Unknown3 != a[y].Unknown3
                                || b[y].Unknown4 != a[y].Unknown4
                                || b[y].Unknown5 != a[y].Unknown5
                                || b[y].Unknown9 != a[y].Unknown9
                                || b[y].WeenieClassId != a[y].WeenieClassId
                                )
                            {
                                deDupedTreasureWielded.AddRange(b);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void DeDupeTreasureDeath(List<ACE.Database.Models.World.TreasureDeath> cacheTreasureDeath, List<ACE.Database.Models.World.TreasureDeath> treasureDeath, out List<ACE.Database.Models.World.TreasureDeath> deDupedTreasureDeath)
        {
            deDupedTreasureDeath = new List<ACE.Database.Models.World.TreasureDeath>();

            foreach (var x in treasureDeath)
            {
                var y = cacheTreasureDeath.FirstOrDefault(z => z.TreasureType == x.TreasureType);

                if (y == null)
                    deDupedTreasureDeath.Add(x);
                else
                {
                    if (x.ItemChance != y.ItemChance || x.ItemMaxAmount != y.ItemMaxAmount || x.ItemMinAmount != y.ItemMinAmount || x.ItemTreasureTypeSelectionChances != y.ItemTreasureTypeSelectionChances || x.LootQualityMod != y.LootQualityMod || x.MagicItemChance != y.MagicItemChance
                        || x.MagicItemMaxAmount != y.MagicItemMaxAmount || x.MagicItemMinAmount != y.MagicItemMinAmount || x.MagicItemTreasureTypeSelectionChances != y.MagicItemTreasureTypeSelectionChances || x.MundaneItemChance != y.MundaneItemChance || x.MundaneItemMaxAmount != y.MundaneItemMaxAmount
                        || x.MundaneItemMinAmount != y.MundaneItemMinAmount || x.MundaneItemTypeSelectionChances != y.MundaneItemTypeSelectionChances || x.Tier != y.Tier || x.TreasureType != y.TreasureType || x.UnknownChances != y.UnknownChances
                        )
                        deDupedTreasureDeath.Add(x);
                }
            }
        }

        private void cmdACE4CraftingParse_Click(object sender, EventArgs e)
        {
            cmdACE4CraftingParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting Crafting from database... ";

            var cacheCraftingTables = Globals.CacheBin.CraftingTable.ConvertToACE();
            Globals.ACEDatabase.WorldDbContext.CookBook.Load();
            Globals.ACEDatabase.WorldDbContext.Recipe.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeMod.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeModsBool.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeModsDID.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeModsFloat.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeModsIID.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeModsInt.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeModsString.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeRequirementsBool.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeRequirementsDID.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeRequirementsFloat.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeRequirementsIID.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeRequirementsInt.Load();
            Globals.ACEDatabase.WorldDbContext.RecipeRequirementsString.Load();
            var cookBooks = Globals.ACEDatabase.WorldDbContext.CookBook.ToList();
            var recipes = Globals.ACEDatabase.WorldDbContext.Recipe.ToList();

            DeDupeCrafting(cacheCraftingTables.Recipies, cacheCraftingTables.CookBooks, recipes, cookBooks, out var deDupedRecipes, out var deDupedCookBooks, out var deDupeRecipeIds);

            //foreach (var thing in deDupedRecipes)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            //foreach (var thing in deDupedCookBooks)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedRecipes.Count > 0 && deDupedCookBooks.Count > 0)
                //CraftingSQLWriter.WriteFiles(deDupedRecipes, deDupedCookBooks, Globals.WeenieNames, Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\SQL\\", true);
                CraftingSQLWriter.WriteFiles(deDupedRecipes, deDupedCookBooks, Globals.WeenieNames, Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\", true);

            var cacheIds = cacheCraftingTables.Recipies.Select(x => x.Id).ToHashSet();
            var spellIds = recipes.Select(x => x.Id).ToHashSet();
            var deDupeIds = deDupedRecipes.Select(x => x.Id).ToHashSet();

            var deletedIds = cacheIds.Except(spellIds).Except(deDupeIds).ToHashSet();

            if (writeDeletedFiles && deletedIds.Count > 0)
            {
                var sqlWriter = new ACE.Database.SQLFormatters.World.RecipeSQLWriter();

                sqlWriter.WeenieNames = Globals.WeenieNames;

                //Parallel.ForEach(sortedInput, kvp =>
                ////foreach (var kvp in sortedInput)
                //{
                //    string fileName = sqlWriter.GetDefaultFileName(kvp.Value[0]);

                //    using (StreamWriter writer = new StreamWriter(outputFolder + fileName))
                //    {
                //        if (includeDELETEStatementBeforeInsert)
                //        {
                //            sqlWriter.CreateSQLDELETEStatement(kvp.Value, writer);
                //            writer.WriteLine();
                //        }

                //        sqlWriter.CreateSQLINSERTStatement(kvp.Value, writer);
                //    }
                //});

                foreach (var lb in deletedIds)
                {
                    var thing = cacheCraftingTables.Recipies.FirstOrDefault(i => i.Id == lb);//new ACE.Database.Models.World.Recipe { Id = lb };

                    string fileName = sqlWriter.GetDefaultFileName(thing, cacheCraftingTables.CookBooks.Where(i => i.RecipeId == lb).ToList());//new List<ACE.Database.Models.World.CookBook>());

                    //using (StreamWriter writer = new StreamWriter(Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\SQL\\" + fileName.Replace(".sql"," - DELETED.sql")))
                    using (StreamWriter writer = new StreamWriter(Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\" + fileName.Replace(".sql", " - DELETED.sql")))
                    {
                        sqlWriter.CreateSQLDELETEStatement(thing, writer);
                        //writer.WriteLine();
                    }
                }
            }

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.CookBook.Load();
                prevContext.Recipe.Load();
                prevContext.RecipeMod.Load();
                prevContext.RecipeModsBool.Load();
                prevContext.RecipeModsDID.Load();
                prevContext.RecipeModsFloat.Load();
                prevContext.RecipeModsIID.Load();
                prevContext.RecipeModsInt.Load();
                prevContext.RecipeModsString.Load();
                prevContext.RecipeRequirementsBool.Load();
                prevContext.RecipeRequirementsDID.Load();
                prevContext.RecipeRequirementsFloat.Load();
                prevContext.RecipeRequirementsIID.Load();
                prevContext.RecipeRequirementsInt.Load();
                prevContext.RecipeRequirementsString.Load();
                var prevCookBooks = prevContext.CookBook.ToList();
                var prevRecipes = prevContext.Recipe.ToList();

                //DeDupeCrafting(prevRecipes, prevCookBooks, recipes, cookBooks, out var deDupedRecipes, out var deDupedCookBooks, out var deDupeRecipeIds);
                DeDupeCrafting(prevRecipes, prevCookBooks, deDupedRecipes, deDupedCookBooks, out deDupedRecipes, out deDupedCookBooks, out deDupeRecipeIds);

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedRecipes)
                        thing.LastModified = GetTimestampForExport();

                    foreach (var thing in deDupedCookBooks)
                        thing.LastModified = GetTimestampForExport();
                }
            }

            if (deDupedRecipes.Count > 0 && deDupedCookBooks.Count > 0)
                //CraftingSQLWriter.WriteFiles(deDupedRecipes, deDupedCookBooks, Globals.WeenieNames, Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\SQL\\", true);
                CraftingSQLWriter.WriteFiles(deDupedRecipes, deDupedCookBooks, Globals.WeenieNames, Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\", true);          

            txtACEDatabaseConnector.Text += $" completed. {deDupedCookBooks.Count:N0} cookbooks and {deDupedRecipes.Count:N0} recipes for a combined total of {deDupeRecipeIds.Count:N0} recipes exported." + Environment.NewLine;

            //cacheRegion.Where(x => !deDupeLbs.Contains(x.Landblock)).ToList().Count
            txtACEDatabaseConnector.Text += $"Skipped {cacheCraftingTables.CookBooks.Where(x=>!deDupeIds.Contains(x.RecipeId)).ToList().Count:N0} unchanged cookbooks and {cacheCraftingTables.Recipies.Where(x => !deDupeIds.Contains(x.Id)).ToList().Count:N0} unchanged recipes for a combined total of {cacheIds.Except(deDupeIds).Except(deletedIds).Count():N0} unchanged recipes." + Environment.NewLine;

            cmdACE4CraftingParse.Enabled = true;
        }

        private void DeDupeCrafting(List<ACE.Database.Models.World.Recipe> cacheRecipes, List<ACE.Database.Models.World.CookBook> cacheCookBooks, List<ACE.Database.Models.World.Recipe> recipes, List<ACE.Database.Models.World.CookBook> cookBooks, out List<ACE.Database.Models.World.Recipe> deDupedRecipes, out List<ACE.Database.Models.World.CookBook> deDupedCookBooks, out HashSet<uint> deDupeRecipeIds)
        {
            deDupedCookBooks = new List<ACE.Database.Models.World.CookBook>();
            deDupedRecipes = new List<ACE.Database.Models.World.Recipe>();

            deDupeRecipeIds = new HashSet<uint>();

            foreach (var cookBook in cookBooks)
            {
                var cacheCookbook = cacheCookBooks.FirstOrDefault(x => x.RecipeId == cookBook.RecipeId && x.SourceWCID == cookBook.SourceWCID && x.TargetWCID == cookBook.TargetWCID);

                if (cacheCookbook == null)
                    deDupeRecipeIds.Add(cookBook.RecipeId);
            }

            foreach (var recipe in recipes)
            {
                var cacheRecipe = cacheRecipes.FirstOrDefault(x => x.Id == recipe.Id);

                if (cacheRecipe == null)
                {
                    deDupeRecipeIds.Add(recipe.Id);
                    continue;
                }

                if (
                       recipe.DataId != cacheRecipe.DataId
                    || recipe.Difficulty != cacheRecipe.Difficulty
                    || recipe.FailAmount != cacheRecipe.FailAmount
                    || recipe.FailDestroySourceAmount != cacheRecipe.FailDestroySourceAmount
                    || recipe.FailDestroySourceChance != cacheRecipe.FailDestroySourceChance
                    || recipe.FailDestroySourceMessage != cacheRecipe.FailDestroySourceMessage
                    || recipe.FailDestroyTargetAmount != cacheRecipe.FailDestroyTargetAmount
                    || recipe.FailDestroyTargetChance != cacheRecipe.FailDestroyTargetChance
                    || recipe.FailDestroyTargetMessage != cacheRecipe.FailDestroyTargetMessage
                    || recipe.FailMessage != cacheRecipe.FailMessage
                    || recipe.FailWCID != cacheRecipe.FailWCID
                    || recipe.RecipeMod.Count != cacheRecipe.RecipeMod.Count
                    || recipe.RecipeRequirementsBool.Count != cacheRecipe.RecipeRequirementsBool.Count
                    || recipe.RecipeRequirementsDID.Count != cacheRecipe.RecipeRequirementsDID.Count
                    || recipe.RecipeRequirementsFloat.Count != cacheRecipe.RecipeRequirementsFloat.Count
                    || recipe.RecipeRequirementsIID.Count != cacheRecipe.RecipeRequirementsIID.Count
                    || recipe.RecipeRequirementsInt.Count != cacheRecipe.RecipeRequirementsInt.Count
                    || recipe.RecipeRequirementsString.Count != cacheRecipe.RecipeRequirementsString.Count
                    || recipe.SalvageType != cacheRecipe.SalvageType
                    || recipe.Skill != cacheRecipe.Skill
                    || recipe.SuccessAmount != cacheRecipe.SuccessAmount
                    || recipe.SuccessDestroySourceAmount != cacheRecipe.SuccessDestroySourceAmount
                    || recipe.SuccessDestroySourceChance != cacheRecipe.SuccessDestroySourceChance
                    || recipe.SuccessDestroySourceMessage != cacheRecipe.SuccessDestroySourceMessage
                    || recipe.SuccessDestroyTargetAmount != cacheRecipe.SuccessDestroyTargetAmount
                    || recipe.SuccessDestroyTargetChance != cacheRecipe.SuccessDestroyTargetChance
                    || recipe.SuccessDestroyTargetMessage != cacheRecipe.SuccessDestroyTargetMessage
                    || recipe.SuccessMessage != cacheRecipe.SuccessMessage
                    || recipe.SuccessWCID != cacheRecipe.SuccessWCID
                    || recipe.Unknown1 != cacheRecipe.Unknown1
                    )
                {
                    deDupeRecipeIds.Add(recipe.Id);
                    continue;
                }

                var notIdentical = false;
            addToHashSet:
                if (notIdentical)
                {
                    deDupeRecipeIds.Add(recipe.Id);
                    continue;
                }

                for (var i = 0; i < recipe.RecipeRequirementsBool.Count; i++)
                {
                    var cacheReq = cacheRecipe.RecipeRequirementsBool?.ElementAt(i);

                    if (cacheReq == null)
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    var req = recipe.RecipeRequirementsBool.ElementAt(i);

                    if (
                           req.Enum != cacheReq.Enum
                        || req.Index != cacheReq.Index
                        || req.Message != cacheReq.Message
                        || req.Stat != cacheReq.Stat
                        || req.Value != cacheReq.Value
                        )
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }
                }

                for (var i = 0; i < recipe.RecipeRequirementsDID.Count; i++)
                {
                    var cacheReq = cacheRecipe.RecipeRequirementsDID?.ElementAt(i);

                    if (cacheReq == null)
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    var req = recipe.RecipeRequirementsDID.ElementAt(i);

                    if (
                           req.Enum != cacheReq.Enum
                        || req.Index != cacheReq.Index
                        || req.Message != cacheReq.Message
                        || req.Stat != cacheReq.Stat
                        || req.Value != cacheReq.Value
                        )
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }
                }

                for (var i = 0; i < recipe.RecipeRequirementsFloat.Count; i++)
                {
                    var cacheReq = cacheRecipe.RecipeRequirementsFloat?.ElementAt(i);

                    if (cacheReq == null)
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    var req = recipe.RecipeRequirementsFloat.ElementAt(i);

                    if (
                           req.Enum != cacheReq.Enum
                        || req.Index != cacheReq.Index
                        || req.Message != cacheReq.Message
                        || req.Stat != cacheReq.Stat
                        || req.Value != cacheReq.Value
                        )
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }
                }

                for (var i = 0; i < recipe.RecipeRequirementsIID.Count; i++)
                {
                    var cacheReq = cacheRecipe.RecipeRequirementsIID?.ElementAt(i);

                    if (cacheReq == null)
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    var req = recipe.RecipeRequirementsIID.ElementAt(i);

                    if (
                           req.Enum != cacheReq.Enum
                        || req.Index != cacheReq.Index
                        || req.Message != cacheReq.Message
                        || req.Stat != cacheReq.Stat
                        || req.Value != cacheReq.Value
                        )
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }
                }

                for (var i = 0; i < recipe.RecipeRequirementsInt.Count; i++)
                {
                    var cacheReq = cacheRecipe.RecipeRequirementsInt?.ElementAt(i);

                    if (cacheReq == null)
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    var req = recipe.RecipeRequirementsInt.ElementAt(i);

                    if (
                           req.Enum != cacheReq.Enum
                        || req.Index != cacheReq.Index
                        || req.Message != cacheReq.Message
                        || req.Stat != cacheReq.Stat
                        || req.Value != cacheReq.Value
                        )
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }
                }

                for (var i = 0; i < recipe.RecipeRequirementsString.Count; i++)
                {
                    var cacheReq = cacheRecipe.RecipeRequirementsString?.ElementAt(i);

                    if (cacheReq == null)
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    var req = recipe.RecipeRequirementsString.ElementAt(i);

                    if (
                           req.Enum != cacheReq.Enum
                        || req.Index != cacheReq.Index
                        || req.Message != cacheReq.Message
                        || req.Stat != cacheReq.Stat
                        || req.Value != cacheReq.Value
                        )
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }
                }

                for (var i = 0; i < recipe.RecipeMod.Count; i++)
                {
                    var cacheMod = cacheRecipe.RecipeMod?.ElementAt(i);

                    if (cacheMod == null)
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    var mod = recipe.RecipeMod.ElementAt(i);

                    if (
                           mod.DataId != cacheMod.DataId
                        || mod.ExecutesOnSuccess != cacheMod.ExecutesOnSuccess
                        || mod.Health != cacheMod.Health
                        || mod.InstanceId != cacheMod.InstanceId
                        || mod.Mana != cacheMod.Mana
                        || mod.RecipeModsBool.Count != cacheMod.RecipeModsBool.Count
                        || mod.RecipeModsDID.Count != cacheMod.RecipeModsDID.Count
                        || mod.RecipeModsFloat.Count != cacheMod.RecipeModsFloat.Count
                        || mod.RecipeModsIID.Count != cacheMod.RecipeModsIID.Count
                        || mod.RecipeModsInt.Count != cacheMod.RecipeModsInt.Count
                        || mod.RecipeModsString.Count != cacheMod.RecipeModsString.Count
                        || mod.Stamina != cacheMod.Stamina
                        || mod.Unknown7 != cacheMod.Unknown7
                        || mod.Unknown9 != cacheMod.Unknown9
                        )
                    {
                        notIdentical = true;
                        goto addToHashSet;
                    }

                    for (var x = 0; x < mod.RecipeModsBool.Count; x++)
                    {
                        var cacheModReq = cacheMod.RecipeModsBool?.ElementAt(x);

                        if (cacheModReq == null)
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }

                        var modReq = mod.RecipeModsBool.ElementAt(x);

                        if (
                               modReq.Enum != cacheModReq.Enum
                            || modReq.Index != cacheModReq.Index
                            || modReq.Source != cacheModReq.Source
                            || modReq.Stat != cacheModReq.Stat
                            || modReq.Value != cacheModReq.Value
                            )
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }
                    }

                    for (var x = 0; x < mod.RecipeModsDID.Count; x++)
                    {
                        var cacheModReq = cacheMod.RecipeModsDID?.ElementAt(x);

                        if (cacheModReq == null)
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }

                        var modReq = mod.RecipeModsDID.ElementAt(x);

                        if (
                               modReq.Enum != cacheModReq.Enum
                            || modReq.Index != cacheModReq.Index
                            || modReq.Source != cacheModReq.Source
                            || modReq.Stat != cacheModReq.Stat
                            || modReq.Value != cacheModReq.Value
                            )
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }
                    }

                    for (var x = 0; x < mod.RecipeModsFloat.Count; x++)
                    {
                        var cacheModReq = cacheMod.RecipeModsFloat?.ElementAt(x);

                        if (cacheModReq == null)
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }

                        var modReq = mod.RecipeModsFloat.ElementAt(x);

                        if (
                               modReq.Enum != cacheModReq.Enum
                            || modReq.Index != cacheModReq.Index
                            || modReq.Source != cacheModReq.Source
                            || modReq.Stat != cacheModReq.Stat
                            || modReq.Value != cacheModReq.Value
                            )
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }
                    }

                    for (var x = 0; x < mod.RecipeModsIID.Count; x++)
                    {
                        var cacheModReq = cacheMod.RecipeModsIID?.ElementAt(x);

                        if (cacheModReq == null)
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }

                        var modReq = mod.RecipeModsIID.ElementAt(x);

                        if (
                               modReq.Enum != cacheModReq.Enum
                            || modReq.Index != cacheModReq.Index
                            || modReq.Source != cacheModReq.Source
                            || modReq.Stat != cacheModReq.Stat
                            || modReq.Value != cacheModReq.Value
                            )
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }
                    }

                    for (var x = 0; x < mod.RecipeModsInt.Count; x++)
                    {
                        var cacheModReq = cacheMod.RecipeModsInt?.ElementAt(x);

                        if (cacheModReq == null)
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }

                        var modReq = mod.RecipeModsInt.ElementAt(x);

                        if (
                               modReq.Enum != cacheModReq.Enum
                            || modReq.Index != cacheModReq.Index
                            || modReq.Source != cacheModReq.Source
                            || modReq.Stat != cacheModReq.Stat
                            || modReq.Value != cacheModReq.Value
                            )
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }
                    }

                    for (var x = 0; x < mod.RecipeModsString.Count; x++)
                    {
                        var cacheModReq = cacheMod.RecipeModsString?.ElementAt(x);

                        if (cacheModReq == null)
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }

                        var modReq = mod.RecipeModsString.ElementAt(x);

                        if (
                               modReq.Enum != cacheModReq.Enum
                            || modReq.Index != cacheModReq.Index
                            || modReq.Source != cacheModReq.Source
                            || modReq.Stat != cacheModReq.Stat
                            || modReq.Value != cacheModReq.Value
                            )
                        {
                            notIdentical = true;
                            goto addToHashSet;
                        }
                    }
                }
            }


            var a = deDupeRecipeIds;

            deDupedCookBooks.AddRange(cookBooks.Where(x => a.Contains(x.RecipeId)));
            deDupedRecipes.AddRange(recipes.Where(x => a.Contains(x.Id)));
        }

        private void cmdACE5HousingParse_Click(object sender, EventArgs e)
        {
            cmdACE5HousingParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting housing from database... ";

            var cacheHousePortals = Globals.CacheBin.HousingPortalsTable.ConvertToACE();

            var results = Globals.ACEDatabase.WorldDbContext.HousePortal
                    .AsNoTracking()
                    .ToList();

            var x = new Dictionary<uint, List<ACE.Database.Models.World.HousePortal>>();

            foreach (var h in cacheHousePortals)
            {
                if (!x.TryAdd(h.HouseId, new List<ACE.Database.Models.World.HousePortal> { h }))
                    x[h.HouseId].Add(h);
            }

            var y = new Dictionary<uint, List<ACE.Database.Models.World.HousePortal>>();

            foreach (var h in results)
            {
                if (!y.TryAdd(h.HouseId, new List<ACE.Database.Models.World.HousePortal> { h }))
                    y[h.HouseId].Add(h);
            }

            var deDuped = new List<ACE.Database.Models.World.HousePortal>();

            foreach (var q in results)
            {
                //q.LastModified = DateTime.Now;

                if (!x.ContainsKey(q.HouseId))
                    deDuped.Add(q);
                else
                {
                    var a = x[q.HouseId];
                    var b = y[q.HouseId];

                    if (a.Count != b.Count)
                    {
                        deDuped.Add(q);
                    }
                    else
                    {
                        if (
                               !ApproximatelyEqual(b[0].AnglesW, a[0].AnglesW) //b[0].AnglesW != a[0].AnglesW
                            || !ApproximatelyEqual(b[0].AnglesX, a[0].AnglesX) //b[0].AnglesX != a[0].AnglesX
                            || !ApproximatelyEqual(b[0].AnglesY, a[0].AnglesY) //b[0].AnglesY != a[0].AnglesY
                            || !ApproximatelyEqual(b[0].AnglesZ, a[0].AnglesZ) //b[0].AnglesZ != a[0].AnglesZ
                            || b[0].ObjCellId != a[0].ObjCellId
                            || !ApproximatelyEqual(b[0].OriginX, a[0].OriginX) //b[0].OriginX != a[0].OriginX
                            || !ApproximatelyEqual(b[0].OriginY, a[0].OriginY) //b[0].OriginY != a[0].OriginY
                            || !ApproximatelyEqual(b[0].OriginZ, a[0].OriginZ) //b[0].OriginZ != a[0].OriginZ
                            )
                        {
                            deDuped.Add(q);
                        }
                        else if (b.Count == a.Count && b.Count == 2)
                        {
                            if (
                                   !ApproximatelyEqual(b[1].AnglesW, a[1].AnglesW) //b[1].AnglesW != a[1].AnglesW
                                || !ApproximatelyEqual(b[1].AnglesX, a[1].AnglesX) //b[1].AnglesX != a[1].AnglesX
                                || !ApproximatelyEqual(b[1].AnglesY, a[1].AnglesY) //b[1].AnglesY != a[1].AnglesY
                                || !ApproximatelyEqual(b[1].AnglesZ, a[1].AnglesZ) //b[1].AnglesZ != a[1].AnglesZ
                                || b[1].ObjCellId != a[1].ObjCellId
                                || !ApproximatelyEqual(b[1].OriginX, a[1].OriginX) //b[1].OriginX != a[1].OriginX
                                || !ApproximatelyEqual(b[1].OriginY, a[1].OriginY) //b[1].OriginY != a[1].OriginY
                                || !ApproximatelyEqual(b[1].OriginZ, a[1].OriginZ) //b[1].OriginZ != a[1].OriginZ
                            )
                            {
                                deDuped.Add(q);
                            }
                        }
                    }
                }
            }

            if (deDuped.Count > 0)
                //HouseSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\5 HousingPortals\\SQL\\", true);
                HouseSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\5 HousingPortals\\", true);

            var cacheIds = cacheHousePortals.Select(x => x.HouseId).ToHashSet();
            var spellIds = results.Select(x => x.HouseId).ToHashSet();
            var deDupeIds = deDuped.Select(x => x.HouseId).ToHashSet();

            var deletedIds = cacheIds.Except(spellIds).Except(deDupeIds).ToHashSet();

            txtACEDatabaseConnector.Text += $" completed. {deDuped.Count:N0} Housing portals exported." + Environment.NewLine;

            txtACEDatabaseConnector.Text += $"Skipped {cacheIds.Except(deDupeIds).Except(deletedIds).Count():N0} unchanged housing portals." + Environment.NewLine;

            cmdACE5HousingParse.Enabled = true;
        }

        private void cmdACE6LandblocksParse_Click(object sender, EventArgs e)
        {
            cmdACE6LandblocksParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting Landblocks from database... ";

            var cachelandblockInstances = Globals.CacheBin.LandBlockData.ConvertToACE();

            Globals.ACEDatabase.WorldDbContext.LandblockInstance.Load();
            Globals.ACEDatabase.WorldDbContext.LandblockInstanceLink.Load();
            var landblocks = Globals.ACEDatabase.WorldDbContext.LandblockInstance.ToList();

            DeDupeLandblocks(cachelandblockInstances, landblocks, out var deDupedLandblockInstances);

            //foreach (var thing in deDupedLandblockInstances)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedLandblockInstances.Count > 0)
                //LandblockSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\6 LandBlockExtendedData\\SQL\\", Globals.WeenieNames, true);
                LandblockSQLWriter.WriteFiles(deDupedLandblockInstances, Settings.Default["GDLESQLOutputFolder"] + "\\6 LandBlockExtendedData\\", Globals.WeenieNames, true);

            var cacheLbs = cachelandblockInstances.GroupBy(x => (x.ObjCellId >> 16)).Select(x => (x.First().ObjCellId >> 16)).ToHashSet();
            var encounterLbs = landblocks.GroupBy(x => (x.ObjCellId >> 16)).Select(x => (x.First().ObjCellId >> 16)).ToHashSet();
            var deDupeLbs = deDupedLandblockInstances.GroupBy(x => (x.ObjCellId >> 16)).Select(x => (x.First().ObjCellId >> 16)).ToHashSet();

            var deletedLbs = cacheLbs.Except(encounterLbs).Except(deDupeLbs).ToHashSet();

            if (writeDeletedFiles && deletedLbs.Count > 0)
            {
                var sqlWriter = new ACE.Database.SQLFormatters.World.LandblockInstanceWriter();

                sqlWriter.WeenieNames = Globals.WeenieNames;

                foreach (var lb in deletedLbs)
                {
                    var thing = new List<ACE.Database.Models.World.LandblockInstance> { new ACE.Database.Models.World.LandblockInstance { ObjCellId = lb << 16 } };

                    string fileName = sqlWriter.GetDefaultFileName(thing[0]);

                    using (StreamWriter writer = new StreamWriter(Settings.Default["GDLESQLOutputFolder"] + "\\6 LandBlockExtendedData\\" + fileName.Replace(".sql", " - DELETED.sql")))
                    {
                        sqlWriter.CreateSQLDELETEStatement(thing, writer);
                        //writer.WriteLine();
                    }
                }
            }

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.LandblockInstance.Load();
                prevContext.LandblockInstanceLink.Load();
                var prevLandblocks = prevContext.LandblockInstance.ToList();

                DeDupeLandblocks(prevLandblocks, deDupedLandblockInstances, out deDupedLandblockInstances);

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedLandblockInstances)
                    {
                        thing.LastModified = GetTimestampForExport();

                        if (thing.Guid >= 0x80000000)
                            txtACEDatabaseConnector.Text += Environment.NewLine + $"WARNING: Landblock instance 0x{thing.Guid:X8} in landblock 0x{thing.ObjCellId:X4} is not in the static range and will cause issues!!! " + Environment.NewLine;

                        foreach (var subthing in thing.LandblockInstanceLink)
                            subthing.LastModified = GetTimestampForExport();
                    }
                }
            }

            //foreach (var thing in deDupedLandblockInstances)
            //{
            //    var date = new DateTime(2021, 11, 1);
            //    //thing.LastModified = date;
            //    date = thing.LastModified;

            //    foreach (var subthing in thing.LandblockInstanceLink)
            //        subthing.LastModified = date;
            //}

            if (deDupedLandblockInstances.Count > 0)
                //LandblockSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\6 LandBlockExtendedData\\SQL\\", Globals.WeenieNames, true);
                LandblockSQLWriter.WriteFiles(deDupedLandblockInstances, Settings.Default["GDLESQLOutputFolder"] + "\\6 LandBlockExtendedData\\", Globals.WeenieNames, true);

            deDupeLbs = deDupedLandblockInstances.GroupBy(x => (x.ObjCellId >> 16)).Select(x => (x.First().ObjCellId >> 16)).ToHashSet();

            txtACEDatabaseConnector.Text += $" completed. {deDupeLbs.Count:N0} landblocks exported." + Environment.NewLine;

            //txtACEDatabaseConnector.Text += $"Skipped {cachelandblockInstances.GroupBy(x => x.Landblock).Select(g => g.First()).ToList().Count - deDuped.GroupBy(x => x.Landblock).Select(g => g.First()).ToList().Count:N0} unchanged landblocks." + Environment.NewLine;
            txtACEDatabaseConnector.Text += $"Skipped {cacheLbs.Except(deDupeLbs).Except(deletedLbs).Count():N0} unchanged landblocks." + Environment.NewLine;

            cmdACE6LandblocksParse.Enabled = true;
        }

        private void DeDupeLandblocks(List<ACE.Database.Models.World.LandblockInstance> cacheLandblockInstances, List<ACE.Database.Models.World.LandblockInstance> landblockInstances, out List<ACE.Database.Models.World.LandblockInstance> deDupedLandblockInstances)
        {
            ////var cacheWeenies = Globals.CacheBin.WeenieDefaults.ConvertToACE();

            ////Globals.ACEDatabase.ReCacheAllWeeniesInParallel();

            //var cachelandblockInstances = Globals.CacheBin.LandBlockData.ConvertToACE();

            ////var landblocks = Globals.ACEDatabase.GetAllLandblockInstances();
            //Globals.ACEDatabase.WorldDbContext.LandblockInstance.Load();
            //Globals.ACEDatabase.WorldDbContext.LandblockInstanceLink.Load();
            //var landblocks = Globals.ACEDatabase.WorldDbContext.LandblockInstance.ToList();

            ////uint landblockToCloneFrom = 0x01C9;
            ////uint landblockToCloneTo = 0x003C;
            ////var landblocks = Globals.ACEDatabase.CloneLandblockToAnother(landblockToCloneFrom, landblockToCloneTo);

            var x = new Dictionary<uint, List<ACE.Database.Models.World.LandblockInstance>>();

            foreach (var h in cacheLandblockInstances)
            {
                var lbid = h.ObjCellId >> 16;
                if (!x.TryAdd(lbid, new List<ACE.Database.Models.World.LandblockInstance> { h }))
                    x[lbid].Add(h);
            }

            var y = new Dictionary<uint, List<ACE.Database.Models.World.LandblockInstance>>();

            foreach (var h in landblockInstances)
            {
                var lbid = h.ObjCellId >> 16;
                if (!y.TryAdd(lbid, new List<ACE.Database.Models.World.LandblockInstance> { h }))
                    y[lbid].Add(h);
            }

            //var deDuped = new List<ACE.Database.Models.World.LandblockInstance>();
            var deDupedIndex = new HashSet<uint>();

            foreach (var q in landblockInstances)
            {
                var lbid = q.ObjCellId >> 16;

                if (!x.ContainsKey(lbid))
                {
                    //deDuped.Add(q);
                    deDupedIndex.Add(lbid);
                }
                else
                {
                    if (deDupedIndex.Contains(lbid))
                        continue;

                    var a = x[lbid];
                    var b = y[lbid];

                    //if (deDupedIndex.Contains(lbid))
                    //deDuped.Add(q);
                    //else if (a.Count != b.Count)
                    if (a.Count != b.Count)
                    {
                        //deDuped.Add(q);
                        deDupedIndex.Add(lbid);
                    }
                    else
                    {
                        var c = a.FirstOrDefault(l => l.Guid == q.Guid);
                        var d = b.FirstOrDefault(l => l.Guid == q.Guid);

                        if (c == null && d != null)
                        {
                            //deDuped.Add(q);
                            deDupedIndex.Add(lbid);
                        }
                        //else if (c.AnglesW != d.AnglesW || c.AnglesX != d.AnglesX || c.AnglesY != d.AnglesY || c.IsLinkChild != d.IsLinkChild || c.ObjCellId != d.ObjCellId || c.OriginX != d.OriginX || c.OriginY != d.OriginY || c.OriginZ != d.OriginZ || c.WeenieClassId != d.WeenieClassId)
                        else if (
                               !ApproximatelyEqual(c.AnglesW, d.AnglesW) //c.AnglesW != d.AnglesW
                            || !ApproximatelyEqual(c.AnglesX, d.AnglesX) //c.AnglesX != d.AnglesX
                            || !ApproximatelyEqual(c.AnglesY, d.AnglesY) //c.AnglesY != d.AnglesY
                            || !ApproximatelyEqual(c.AnglesZ, d.AnglesZ) //c.AnglesZ != d.AnglesZ
                            || c.IsLinkChild != d.IsLinkChild
                            || c.ObjCellId != d.ObjCellId
                            || !ApproximatelyEqual(c.OriginX, d.OriginX) //c.OriginX != d.OriginX
                            || !ApproximatelyEqual(c.OriginY, d.OriginY) //c.OriginY != d.OriginY
                            || !ApproximatelyEqual(c.OriginZ, d.OriginZ) //c.OriginZ != d.OriginZ
                            || c.WeenieClassId != d.WeenieClassId
                            )
                        {
                            //deDuped.Add(q);
                            deDupedIndex.Add(lbid);
                        }
                    }
                }
            }

            //deDuped.Clear();
            deDupedLandblockInstances = new List<ACE.Database.Models.World.LandblockInstance>();

            foreach (var q in landblockInstances)
            {
                var lbid = q.ObjCellId >> 16;

                if (deDupedIndex.Contains(lbid))
                    deDupedLandblockInstances.Add(q);
            }
        }

        private void cmdACE8QuestsParse_Click(object sender, EventArgs e)
        {
            cmdACE8QuestsParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting quests from database... ";

            var cacheQuests = Globals.CacheBin.QuestDefDB.ConvertToACE();

            var results = Globals.ACEDatabase.WorldDbContext.Quest
                    .AsNoTracking()
                    .ToList();

            var x = cacheQuests.ToDictionary(z => z.Name.ToLower(), z => z);

            var deDuped = new List<ACE.Database.Models.World.Quest>();

            foreach (var q in results)
            {
                var z = q.Name.ToLower();

                //q.LastModified = DateTime.Now;

                if (x.ContainsKey(z))
                {
                    if (!q.Name.Equals(x[z].Name))
                        q.Name = x[z].Name;

                    if (x[z].MaxSolves != q.MaxSolves || x[z].MinDelta != q.MinDelta || x[z].Message != q.Message)
                        deDuped.Add(q);
                }
                else
                    deDuped.Add(q);
            }

            DeDupeQuests(cacheQuests, results, out var deDupedQuests);

            //foreach (var thing in deDupedQuests)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedQuests.Count > 0)
                //QuestSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\8 QuestDefDB\\SQL\\", true);
                QuestSQLWriter.WriteFiles(deDupedQuests, Settings.Default["GDLESQLOutputFolder"] + "\\8 QuestDefDB\\", true);

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.Quest.Load();
                var prevQuests = prevContext.Quest.ToList();

                DeDupeQuests(prevQuests, deDupedQuests, out deDupedQuests);

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedQuests)
                        thing.LastModified = GetTimestampForExport();
                }
            }

            if (deDupedQuests.Count > 0)
                //QuestSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\8 QuestDefDB\\SQL\\", true);
                QuestSQLWriter.WriteFiles(deDupedQuests, Settings.Default["GDLESQLOutputFolder"] + "\\8 QuestDefDB\\", true);

            var cacheIds = cacheQuests.Select(x => x.Name.ToUpper()).ToHashSet();
            var spellIds = results.Select(x => x.Name.ToUpper()).ToHashSet();
            var deDupeIds = deDupedQuests.Select(x => x.Name.ToUpper()).ToHashSet();

            var deletedIds = cacheIds.Except(spellIds).Except(deDupeIds).ToHashSet();

            txtACEDatabaseConnector.Text += $" completed. {deDupedQuests.Count:N0} quests exported." + Environment.NewLine;

            txtACEDatabaseConnector.Text += $"Skipped {cacheIds.Except(deDupeIds).Except(deletedIds).Count():N0} unchanged quests." + Environment.NewLine;

            cmdACE8QuestsParse.Enabled = true;
        }

        private void DeDupeQuests(List<ACE.Database.Models.World.Quest> cacheQuests, List<ACE.Database.Models.World.Quest> quests, out List<ACE.Database.Models.World.Quest> deDupedQuests)
        {
            var x = cacheQuests.ToDictionary(z => z.Name.ToLower(), z => z);

            deDupedQuests = new List<ACE.Database.Models.World.Quest>();

            foreach (var q in quests)
            {
                var z = q.Name.ToLower();

                if (x.ContainsKey(z))
                {
                    if (!q.Name.Equals(x[z].Name))
                        q.Name = x[z].Name;

                    if (x[z].MaxSolves != q.MaxSolves || x[z].MinDelta != q.MinDelta || x[z].Message != q.Message)
                        deDupedQuests.Add(q);
                }
                else
                    deDupedQuests.Add(q);
            }
        }

        private bool captureAndResortESfiles = true;
        private bool tab2spaceESfiles = true;
        private bool doDateUpdate = false;

        private void cmdACE9WeeniesParse_Click(object sender, EventArgs e)
        {
            cmdACE9WeeniesParse.Enabled = false;

            if (captureAndResortESfiles)
            {
                txtACEDatabaseConnector.Text += Environment.NewLine + "Capturing ES files from patches repo on disk... ";

                //var esFiles = Directory.GetFiles(@"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches", "*.es", new EnumerationOptions { RecurseSubdirectories = true });
                var esFiles = Directory.EnumerateFiles(@"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches", "*.es", SearchOption.AllDirectories);
                foreach (var file in esFiles)
                {
                    var rootToRemove = @"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches\";
                    //var x = file.Remove(rootToRemove.Length);
                    var currentFileNameAndPath = file[rootToRemove.Length..file.Length];
                    //Console.WriteLine(x);
                    //var newRoot = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
                    var newRoot = Settings.Default["GDLESQLOutputFolder"] + "\\D EmoteScript\\";
                    var fileInfo = new FileInfo(currentFileNameAndPath);
                    var fileNameES = fileInfo.Name;
                    var fileDirectory = newRoot + currentFileNameAndPath[0..^(fileNameES.Length + 1)];

                    Directory.CreateDirectory(fileDirectory);

                    var outputFile = fileDirectory + "\\" + fileNameES;

                    File.Copy(file, outputFile);
                }

                txtACEDatabaseConnector.Text += $"completed. Captured {esFiles.Count():N0} EmoteScript files." + Environment.NewLine;
            }

            cmdACEDatabaseCacheAllWeenies_Click(sender, e);

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting Weenies from database... ";

            var cacheWeenies = Globals.CacheBin.WeenieDefaults.ConvertToACE();

            //Globals.ACEDatabase.ReCacheAllWeeniesInParallel();

            //var results = Globals.ACEDatabase.WorldDatabase.GetAllWeenies();
            //.AsNoTracking()
            //.ToList();

            var aceTreasureWielded = Globals.ACEDatabase.GetAllTreasureWielded();
            var aceTreasureDeath = Globals.ACEDatabase.GetAllTreasureDeath();

            var treasureWielded = new Dictionary<uint, List<ACE.Database.Models.World.TreasureWielded>>();
            foreach (var item in aceTreasureWielded)
            {
                if (!treasureWielded.ContainsKey(item.TreasureType))
                    treasureWielded.Add(item.TreasureType, new List<ACE.Database.Models.World.TreasureWielded>());

                treasureWielded[item.TreasureType].Add(item);
            }
            var treasureDeath = new Dictionary<uint, ACE.Database.Models.World.TreasureDeath>();
            foreach (var item in aceTreasureDeath)
            {
                if (!treasureDeath.ContainsKey(item.TreasureType))
                    treasureDeath.Add(item.TreasureType, item);
            }

            if (!usePrevVersion)
                CleanupWeenies(Globals.ACEDatabase.Weenies);

            DeDupeWeenies(cacheWeenies, Globals.ACEDatabase.Weenies, out var deDupedWeenies);

            //foreach (var wo in deDupedWeenies)
            //{
            //    var xx = cacheWeenies.FirstOrDefault(z => z.ClassId == wo.ClassId);

            //    if (xx?.WeeniePropertiesEventFilter != null && (wo.WeeniePropertiesEventFilter.Count == 0 || wo.WeeniePropertiesEventFilter.Count != xx?.WeeniePropertiesEventFilter.Count))
            //    {
            //        wo.WeeniePropertiesEventFilter.Clear();
            //        foreach (var ev in xx.WeeniePropertiesEventFilter)
            //        {
            //            wo.WeeniePropertiesEventFilter.Add(new ACE.Database.Models.World.WeeniePropertiesEventFilter { Event = ev.Event, ObjectId = ev.ObjectId });
            //        }
            //    }
            //}

            //foreach (var thing in deDupedWeenies)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedWeenies.Count > 0)
                //WeenieSQLWriter.WriteFiles(Globals.ACEDatabase.Weenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\SQL\\", Globals.WeenieNames, treasureWielded, treasureDeath, Globals.ACEDatabase.Weenies.ToDictionary(x => x.ClassId, x => x), true);
                //WeenieSQLWriter.WriteFiles(deDupedWeenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\SQL\\", Globals.WeenieNames, treasureWielded, treasureDeath, deDupedWeenies.ToDictionary(x => x.ClassId, x => x), true);
                WeenieSQLWriter.WriteFiles(deDupedWeenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\", Globals.WeenieNames, treasureWielded, treasureDeath, deDupedWeenies.ToDictionary(x => x.ClassId, x => x), true);

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.Weenie.Load();
                prevContext.WeeniePropertiesBool.Load();
                prevContext.WeeniePropertiesDID.Load();
                prevContext.WeeniePropertiesFloat.Load();
                prevContext.WeeniePropertiesIID.Load();
                prevContext.WeeniePropertiesInt.Load();
                prevContext.WeeniePropertiesInt64.Load();
                prevContext.WeeniePropertiesPosition.Load();
                prevContext.WeeniePropertiesString.Load();
                prevContext.WeeniePropertiesAnimPart.Load();
                prevContext.WeeniePropertiesAttribute.Load();
                prevContext.WeeniePropertiesAttribute2nd.Load();
                prevContext.WeeniePropertiesBodyPart.Load();
                prevContext.WeeniePropertiesBook.Load();
                prevContext.WeeniePropertiesBookPageData.Load();
                prevContext.WeeniePropertiesCreateList.Load();
                prevContext.WeeniePropertiesEmote.Load();
                prevContext.WeeniePropertiesEmoteAction.Load();
                prevContext.WeeniePropertiesEventFilter.Load();
                prevContext.WeeniePropertiesGenerator.Load();
                prevContext.WeeniePropertiesPalette.Load();
                prevContext.WeeniePropertiesSkill.Load();
                prevContext.WeeniePropertiesSpellBook.Load();
                prevContext.WeeniePropertiesTextureMap.Load();
                var prevWeenies = prevContext.Weenie.ToList();

                //CleanupWeenies(prevWeenies);

                DeDupeWeenies(prevWeenies, deDupedWeenies, out deDupedWeenies);

                //foreach (var wo in deDupedWeenies)
                //{
                //    var xx = cacheWeenies.FirstOrDefault(z => z.ClassId == wo.ClassId);

                //    if (xx?.WeeniePropertiesEventFilter != null && (wo.WeeniePropertiesEventFilter.Count == 0 || wo.WeeniePropertiesEventFilter.Count != xx?.WeeniePropertiesEventFilter.Count))
                //    {
                //        wo.WeeniePropertiesEventFilter.Clear();
                //        foreach (var ev in xx.WeeniePropertiesEventFilter)
                //        {
                //            wo.WeeniePropertiesEventFilter.Add(new ACE.Database.Models.World.WeeniePropertiesEventFilter { Event = ev.Event, ObjectId = ev.ObjectId });
                //        }
                //    }
                //}

                //foreach (var wo in deDupedWeenies)
                //{
                //    var xx = cacheWeenies.FirstOrDefault(z => z.ClassId == wo.ClassId);

                //    wo.WeeniePropertiesPosition.Clear();
                //    foreach (var pos in xx.WeeniePropertiesPosition)
                //    {
                //        wo.WeeniePropertiesPosition.Add(new ACE.Database.Models.World.WeeniePropertiesPosition { AnglesW = pos.AnglesW, AnglesX = pos.AnglesX, AnglesY = pos.AnglesY, AnglesZ = pos.AnglesZ, ObjCellId = pos.ObjCellId, ObjectId = pos.ObjectId, OriginX = pos.OriginX, OriginY = pos.OriginY, OriginZ = pos.OriginZ, PositionType = pos.PositionType });
                //    }
                //}

                //DeDupeWeenies(cacheWeenies, deDupedWeenies, out deDupedWeenies);

                //foreach (var wo in deDupedWeenies)
                //{
                //    var xx = Globals.ACEDatabase.Weenies.FirstOrDefault(z => z.ClassId == wo.ClassId);

                //    wo.WeeniePropertiesPosition.Clear();
                //    foreach (var pos in xx.WeeniePropertiesPosition)
                //    {
                //        wo.WeeniePropertiesPosition.Add(new ACE.Database.Models.World.WeeniePropertiesPosition { AnglesW = pos.AnglesW, AnglesX = pos.AnglesX, AnglesY = pos.AnglesY, AnglesZ = pos.AnglesZ, ObjCellId = pos.ObjCellId, ObjectId = pos.ObjectId, OriginX = pos.OriginX, OriginY = pos.OriginY, OriginZ = pos.OriginZ, PositionType = pos.PositionType });
                //    }
                //}

                //txtACEDatabaseConnector.Text += Environment.NewLine + "Cleaning up weenies... ";
                CleanupWeenies(deDupedWeenies);
                //txtACEDatabaseConnector.Text += $" completed." + Environment.NewLine;

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedWeenies)
                        thing.LastModified = GetTimestampForExport();
                }
            }

            if (deDupedWeenies.Count > 0)
                //WeenieSQLWriter.WriteFiles(Globals.ACEDatabase.Weenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\SQL\\", Globals.WeenieNames, treasureWielded, treasureDeath, Globals.ACEDatabase.Weenies.ToDictionary(x => x.ClassId, x => x), true);
                //WeenieSQLWriter.WriteFiles(deDupedWeenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\SQL\\", Globals.WeenieNames, treasureWielded, treasureDeath, deDupedWeenies.ToDictionary(x => x.ClassId, x => x), true);
                WeenieSQLWriter.WriteFiles(deDupedWeenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\", Globals.WeenieNames, treasureWielded, treasureDeath, deDupedWeenies.ToDictionary(x => x.ClassId, x => x), true);

            var cacheIds = cacheWeenies.OrderBy(x => x.ClassId).Select(x => x.ClassId).ToHashSet();
            var spellIds = Globals.ACEDatabase.Weenies.OrderBy(x => x.ClassId).Select(x => x.ClassId).ToHashSet();
            var deDupeIds = deDupedWeenies.OrderBy(x => x.ClassId).Select(x => x.ClassId).ToHashSet();

            var deletedIds = cacheIds.Except(spellIds).Except(deDupeIds).ToHashSet();

            txtACEDatabaseConnector.Text += $" completed. {deDupedWeenies.Count:N0} weenies exported." + Environment.NewLine;

            txtACEDatabaseConnector.Text += $"Skipped {cacheIds.Except(deDupeIds).Except(deletedIds).Count():N0} unchanged weenies." + Environment.NewLine;

            if (captureAndResortESfiles)
            {
                txtACEDatabaseConnector.Text += Environment.NewLine + "Re-linking captured ES files... ";

                var sqlWriter = new ACE.Database.SQLFormatters.World.WeenieSQLWriter();
                var weenieRoot = Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\";
                //var esRoot = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
                var esRoot = Settings.Default["GDLESQLOutputFolder"] + "\\D EmoteScript\\9 WeenieDefaults\\";
                var esLink = 0;
                //foreach (var weenie in deDupedWeenies)
                foreach (var weenie in Globals.ACEDatabase.Weenies)
                {
                    string weenieFileName = sqlWriter.GetDefaultFileName(weenie);
                    var weenieSubFolder = sqlWriter.GetDefaultSubfolder(weenie);

                    var fileInfo = new FileInfo(weenieRoot + weenieSubFolder + weenieFileName);
                    var trimName = weenieFileName[0..5];
                    var trimName2 = weenieFileName[0..5].TrimStart('0');
                    var x = Directory.EnumerateFiles(esRoot, trimName + "*.es", SearchOption.AllDirectories).Union(Directory.EnumerateFiles(esRoot, trimName2 + ".es", SearchOption.AllDirectories));
                    var y = x?.LastOrDefault();

                    if (y != null)
                    {
                        var esFile = new FileInfo(y);
                        File.Move(y, weenieRoot + weenieSubFolder + trimName + ".es", true);
                        esLink++;
                    }
                }

                DeleteEmptySubdirectories(esRoot);

                var esRoot2 = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
                var esRoot3 = Settings.Default["GDLESQLOutputFolder"] + "\\D EmoteScript\\C EmoteScript\\";                
                var esRoot4 = Settings.Default["GDLESQLOutputFolder"] + "\\D EmoteScript\\";
                var unmatchedESFiles = Directory.EnumerateFiles(esRoot3, "*.es", SearchOption.AllDirectories);
                var esUnLink = 0;
                foreach (var file in unmatchedESFiles)
                {
                    //var esFile = new FileInfo(file);
                    //File.Move(file, esRoot2 + esFile.Name, true);

                    //var rootToRemove = @"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches\";
                    var rootToRemove = esRoot3;
                    //var x = file.Remove(rootToRemove.Length);
                    var currentFileNameAndPath = file[rootToRemove.Length..file.Length];
                    //Console.WriteLine(x);
                    //var newRoot = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
                    var newRoot = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
                    var fileInfo = new FileInfo(currentFileNameAndPath);
                    var fileNameES = fileInfo.Name;
                    var fileDirectory = newRoot + currentFileNameAndPath[0..^(fileNameES.Length + 1)];

                    Directory.CreateDirectory(fileDirectory);

                    var outputFile = fileDirectory + "\\" + fileNameES;

                    File.Move(file, outputFile, true);
                    esUnLink++;
                }

                DeleteEmptySubdirectories(esRoot4);

                if (!Directory.EnumerateFiles(esRoot4).Any())
                    Directory.Delete(esRoot4, true);

                txtACEDatabaseConnector.Text += $" completed. {esLink:N0} EmoteScript files re-linked, and {esUnLink:N0} unlinked EmoteScript files for a combined total of {esLink + esUnLink:N0} EmoteScript files saved." + Environment.NewLine;
            }

            if (tab2spaceESfiles)
            {
                var esRoot = Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\";
                var matchedESFiles = Directory.EnumerateFiles(esRoot, "*.es", SearchOption.AllDirectories);
                var esRoot2 = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
                var unmatchedESFiles = Directory.EnumerateFiles(esRoot2, "*.es", SearchOption.AllDirectories);
                var ESFiles = matchedESFiles.Concat(unmatchedESFiles);
                var affected = 0;
                txtACEDatabaseConnector.Text += $"Checking for tabs/newlines/whitespace to clean up in {ESFiles.Count():N0} EmoteScript files...";
                foreach (var ESFile in ESFiles)
                {
                    var text = File.ReadAllText(ESFile);
                    var changed = false;

                    if (!text.Equals(text.Trim()))
                    {
                        text = text.Trim();
                        //changed = true;
                    }

                    if (text.Contains("\t"))
                    {
                        text = text.Replace("\t", "    ");
                        changed = true;
                    }

                    if (!text.EndsWith("\r\n"))
                    {
                        text += "\r\n";
                        //changed = true;
                    }

                    if (changed)
                    {
                        File.WriteAllText(ESFile, text);
                        affected++;
                    }
                }
                txtACEDatabaseConnector.Text += $" completed. {affected:N0} files were cleaned." + Environment.NewLine;
            }

            cmdACE9WeeniesParse.Enabled = true;
        }

        private static void CleanupWeenies(List<ACE.Database.Models.World.Weenie> weenies)
        {
            foreach (var weenie in weenies)
            {
                var name = weenie.WeeniePropertiesString.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyString.Name);

                var className = "";
                if (Enum.IsDefined(typeof(WCLASSID), (int)weenie.ClassId))
                    className = Enum.GetName(typeof(WCLASSID), weenie.ClassId).ToLower();
                else if (weenie.ClassId <= ushort.MaxValue && Enum.IsDefined(typeof(WeenieClasses), (ushort)weenie.ClassId))
                {
                    var clsName = Enum.GetName(typeof(WeenieClasses), weenie.ClassId).ToLower().Substring(2);
                    className = clsName.Substring(0, clsName.Length - 6);
                }
                else
                    className = "ace" + weenie.ClassId.ToString() + "-" + name.Value.Replace("'", "").Replace(" ", "").Replace(".", "").Replace("(", "").Replace(")", "").Replace("+", "").Replace(":", "").Replace("_", "").Replace("-", "").Replace(",", "").Replace("\"", "").ToLower();

                className = className.Replace("_", "-");

                if (!weenie.ClassName.Equals(className))
                    weenie.ClassName = className;

                var defaultCombatStyle = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.DefaultCombatStyle);

                if (weenie.Type == (int)ACE.Entity.Enum.WeenieType.Portal || weenie.Type == (int)ACE.Entity.Enum.WeenieType.HousePortal)
                {
                    //if (weenie.ClassId > 31034)
                    //    continue;

                    //var attackable = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.Attackable);
                    //var gravityStatus = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.GravityStatus);
                    var portalShowDestination = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.PortalShowDestination);

                    //if (attackable != null && attackable.Value)
                    //    weenie.WeeniePropertiesBool.Remove(attackable);

                    //if (gravityStatus != null && gravityStatus.Value)
                    //    weenie.WeeniePropertiesBool.Remove(gravityStatus);

                    if (portalShowDestination != null && portalShowDestination.Value)
                        weenie.WeeniePropertiesBool.Remove(portalShowDestination);
                }
                else if (weenie.Type == (int)ACE.Entity.Enum.WeenieType.Creature)
                {
                    var npcLooksLikeObject = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.NpcLooksLikeObject);

                    if (npcLooksLikeObject != null && npcLooksLikeObject.Value)
                    {
                        var aiImmobile = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.AiImmobile);
                        var dontTurnOrMoveWhenGiving = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.DontTurnOrMoveWhenGiving);

                        if (aiImmobile == null)
                            weenie.WeeniePropertiesBool.Add(new ACE.Database.Models.World.WeeniePropertiesBool { ObjectId = weenie.ClassId, Type = (ushort)ACE.Entity.Enum.Properties.PropertyBool.AiImmobile, Value = true });
                        else if (!aiImmobile.Value)
                            aiImmobile.Value = true;

                        if (dontTurnOrMoveWhenGiving == null || (dontTurnOrMoveWhenGiving != null && !dontTurnOrMoveWhenGiving.Value))
                            weenie.WeeniePropertiesBool.Add(new ACE.Database.Models.World.WeeniePropertiesBool { ObjectId = weenie.ClassId, Type = (ushort)ACE.Entity.Enum.Properties.PropertyBool.DontTurnOrMoveWhenGiving, Value = true });
                        else if (!dontTurnOrMoveWhenGiving.Value)
                            dontTurnOrMoveWhenGiving.Value = true;
                    }

                    //var canGenerateRare = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.CanGenerateRare);
                    //var level = weenie.WeeniePropertiesInt.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.Level);

                    ////if (level?.Value < 100 && (canGenerateRare?.Value ?? false))
                    ////{
                    ////    weenie.WeeniePropertiesBool.Remove(canGenerateRare);
                    ////}

                    //if (canGenerateRare != null)
                    //    weenie.WeeniePropertiesBool.Remove(canGenerateRare);

                    var creatureOvers = weenie.WeeniePropertiesInt.Where(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.DamageRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.DamageResistRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.CritRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.CritDamageRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.CritResistRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.CritDamageResistRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearDamage
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearDamageResist
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearCrit
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearCritResist
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearCritDamage
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearCritDamageResist
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearHealingBoost
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearNetherResist
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearLifeResist
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearMaxHealth
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.PKDamageRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.PKDamageResistRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearPKDamageRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearPKDamageResistRating
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.Overpower
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.OverpowerResist
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearOverpower
                                                                           || y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GearOverpowerResist
                    ).ToList();

                    foreach (var item in creatureOvers)
                    {
                        if (item.Value == 0)
                            weenie.WeeniePropertiesInt.Remove(item);
                    }
                }
                else if (weenie.Type == (int)ACE.Entity.Enum.WeenieType.Caster)
                {
                    if (defaultCombatStyle == null)
                        weenie.WeeniePropertiesInt.Add(new ACE.Database.Models.World.WeeniePropertiesInt { Type = (ushort)ACE.Entity.Enum.Properties.PropertyInt.DefaultCombatStyle, Value = (int)ACE.Entity.Enum.CombatStyle.Magic });
                }
                //else
                //    continue;

                foreach (var page in weenie.WeeniePropertiesBookPageData)
                {
                    if (page.AuthorAccount != "prewritten")
                        page.AuthorAccount = "prewritten";

                    if (page.AuthorId != 0xFFFFFFFF)
                        page.AuthorId = 0xFFFFFFFF;
                }

                if (weenie.Type == (int)ACE.Entity.Enum.WeenieType.Book)
                {
                    if (weenie.WeeniePropertiesBook != null)
                    {
                        var maxNumCharsPerPage = weenie.WeeniePropertiesBook.MaxNumCharsPerPage;
                        var maxNumPages = weenie.WeeniePropertiesBook.MaxNumPages;

                        if (maxNumCharsPerPage > 2000)
                            maxNumCharsPerPage = 2000;

                        if (maxNumCharsPerPage < 1000)
                            maxNumCharsPerPage = 1000;

                        var currentPageCount = weenie.WeeniePropertiesBookPageData?.Count ?? 0;

                        if (maxNumPages < currentPageCount)
                            maxNumPages = currentPageCount;

                        weenie.WeeniePropertiesBook.MaxNumPages = maxNumPages;
                        weenie.WeeniePropertiesBook.MaxNumCharsPerPage = maxNumCharsPerPage;
                    }
                    else
                    {
                        var currentPageCount = weenie.WeeniePropertiesBookPageData?.Count ?? 0;

                        weenie.WeeniePropertiesBook = new ACE.Database.Models.World.WeeniePropertiesBook { ObjectId = weenie.ClassId, MaxNumCharsPerPage = 1000, MaxNumPages = currentPageCount };
                    }
                }

                var procSpell = weenie.WeeniePropertiesDID.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyDataId.ProcSpell);
                if (procSpell != null)
                {
                    var spell = weenie.WeeniePropertiesSpellBook.FirstOrDefault(s => s.Spell == procSpell.Value);
                    if (spell != null)
                        weenie.WeeniePropertiesSpellBook.Remove(spell);
                }

                var didSpell = weenie.WeeniePropertiesDID.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyDataId.Spell);
                if (didSpell != null)
                {
                    var spell = weenie.WeeniePropertiesSpellBook.FirstOrDefault(s => s.Spell == didSpell.Value);
                    if (spell != null)
                        weenie.WeeniePropertiesSpellBook.Remove(spell);
                }

                var physicsState = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.PhysicsState);
                if (physicsState != null)
                {
                    var ps = (ACE.Entity.Enum.PhysicsState)physicsState.Value;
                    ps &= ~ACE.Entity.Enum.PhysicsState.HasPhysicsBSP;
                    physicsState.Value = (int)ps;
                }

                foreach (var intV in weenie.WeeniePropertiesInt)
                {
                    if (intV.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.ItemsCapacity && intV.Value == 255)
                        intV.Value = -1;
                    if (intV.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.ContainersCapacity && intV.Value == 255)
                        intV.Value = -1;
                }

                var creationTimestamp = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.CreationTimestamp);
                if (creationTimestamp != null)
                    weenie.WeeniePropertiesInt.Remove(creationTimestamp);

                var appraisalMaxPages = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.AppraisalMaxPages);
                if (appraisalMaxPages != null)
                    weenie.WeeniePropertiesInt.Remove(appraisalMaxPages);

                var appraisalPages = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.AppraisalPages);
                if (appraisalPages != null)
                    weenie.WeeniePropertiesInt.Remove(appraisalPages);

                var appraisalItemSkill = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.AppraisalItemSkill);
                if (appraisalItemSkill != null)
                {
                    weenie.WeeniePropertiesInt.Remove(appraisalItemSkill);

                    var itemSkillLimit = weenie.WeeniePropertiesDID.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyDataId.ItemSkillLimit);
                    if (itemSkillLimit == null)
                        weenie.WeeniePropertiesDID.Add(new ACE.Database.Models.World.WeeniePropertiesDID { Type = (ushort)ACE.Entity.Enum.Properties.PropertyDataId.ItemSkillLimit, Value = (uint)appraisalItemSkill.Value });
                    else if (itemSkillLimit.Value != appraisalItemSkill.Value)
                        itemSkillLimit.Value = (uint)appraisalItemSkill.Value;
                }

                var lockpickSuccess = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.AppraisalLockpickSuccessPercent);
                if (lockpickSuccess != null)
                    weenie.WeeniePropertiesInt.Remove(lockpickSuccess);

                var appraisalLongDescDecoration = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.AppraisalLongDescDecoration);
                if (appraisalLongDescDecoration != null)
                    weenie.WeeniePropertiesInt.Remove(appraisalLongDescDecoration);

                var appraisalHasAllowedActivator = weenie.WeeniePropertiesBool.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.AppraisalHasAllowedActivator);
                if (appraisalHasAllowedActivator != null)
                    weenie.WeeniePropertiesBool.Remove(appraisalHasAllowedActivator);

                var appraisalHasAllowedWielder = weenie.WeeniePropertiesBool.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.AppraisalHasAllowedWielder);
                if (appraisalHasAllowedWielder != null)
                    weenie.WeeniePropertiesBool.Remove(appraisalHasAllowedWielder);

                var currentWieldedLocation = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.CurrentWieldedLocation);
                if (currentWieldedLocation != null)
                    weenie.WeeniePropertiesInt.Remove(currentWieldedLocation);

                var remainingLifespan = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.RemainingLifespan);
                if (remainingLifespan != null)
                    weenie.WeeniePropertiesInt.Remove(remainingLifespan);

                var attackerAi = weenie.WeeniePropertiesBool.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.AttackerAi);
                if (attackerAi != null)
                    weenie.WeeniePropertiesBool.Remove(attackerAi);

                if (weenie.WeeniePropertiesEmote.Any(e => e.Category == (uint)ACE.Entity.Enum.EmoteCategory.Give))
                {
                    if (weenie.ClassId != 4055 && weenie.ClassId != 6823) // skip these wcids from cache
                    {
                        var allowGive = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.AllowGive);

                        if (allowGive == null)
                            weenie.WeeniePropertiesBool.Add(new ACE.Database.Models.World.WeeniePropertiesBool { ObjectId = weenie.ClassId, Type = (ushort)ACE.Entity.Enum.Properties.PropertyBool.AllowGive, Value = true });
                        else if (!allowGive.Value)
                            allowGive.Value = true;
                    }
                }

                var parentLocation = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.ParentLocation);
                var placementPosition = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.PlacementPosition);

                if (weenie.Type != (int)ACE.Entity.Enum.WeenieType.MissileLauncher)
                {
                    if (parentLocation != null)
                        weenie.WeeniePropertiesInt.Remove(parentLocation);


                    if (placementPosition != null)
                        weenie.WeeniePropertiesInt.Remove(placementPosition);
                }
                else
                {
                    if (defaultCombatStyle != null && defaultCombatStyle.Value != (int)ACE.Entity.Enum.CombatStyle.Atlatl)
                    {
                        if (parentLocation == null)
                            weenie.WeeniePropertiesInt.Add(new ACE.Database.Models.World.WeeniePropertiesInt { ObjectId = weenie.ClassId, Type = (ushort)ACE.Entity.Enum.Properties.PropertyInt.ParentLocation, Value = (int)ACE.Entity.Enum.ParentLocation.LeftHand });
                        else if (parentLocation.Value != (int)ACE.Entity.Enum.ParentLocation.LeftHand)
                            parentLocation.Value = (int)ACE.Entity.Enum.ParentLocation.LeftHand;

                        if (placementPosition == null)
                            weenie.WeeniePropertiesInt.Add(new ACE.Database.Models.World.WeeniePropertiesInt { ObjectId = weenie.ClassId, Type = (ushort)ACE.Entity.Enum.Properties.PropertyInt.PlacementPosition, Value = (int)ACE.Entity.Enum.Placement.LeftHand });
                        else if (placementPosition.Value != (int)ACE.Entity.Enum.Placement.LeftHand)
                            placementPosition.Value = (int)ACE.Entity.Enum.Placement.LeftHand;
                    }
                }

                foreach (var str in weenie.WeeniePropertiesString)
                {
                    str.Value = str.Value.Replace("''", "'");
                }

                var rotationSpeed = weenie.WeeniePropertiesFloat.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyFloat.RotationSpeed);
                if (rotationSpeed != null && (rotationSpeed.Value >= 5 || weenie.Type == (int)ACE.Entity.Enum.WeenieType.Creature))
                    weenie.WeeniePropertiesFloat.Remove(rotationSpeed);

                var canGenerateRare = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.CanGenerateRare);
                if (canGenerateRare != null)
                    weenie.WeeniePropertiesBool.Remove(canGenerateRare);

                var corpseGeneratedRare = weenie.WeeniePropertiesBool.FirstOrDefault(p => p.Type == (ushort)ACE.Entity.Enum.Properties.PropertyBool.CorpseGeneratedRare);
                if (corpseGeneratedRare != null)
                    weenie.WeeniePropertiesBool.Remove(corpseGeneratedRare);

                var generatorStartTime = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GeneratorStartTime);
                //if (generatorStartTime != null)
                //{
                //    var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(generatorStartTime.Value);
                //    if (date.Year == DateTime.Now.Year - 1)
                //        generatorStartTime.Value = (int)(date.AddYears(1) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                //}

                var generatorEndTime = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.GeneratorEndTime);
                //if (generatorEndTime != null)
                //{
                //    var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(generatorEndTime.Value);
                //    if (date.Year == DateTime.Now.Year - 1)
                //        generatorEndTime.Value = (int)(date.AddYears(1) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                //}

                if (generatorStartTime != null && generatorEndTime != null)
                {
                    var startDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(generatorStartTime.Value);
                    var endDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(generatorEndTime.Value);
                    //if (startDate.Hour == 10 && endDate.Hour == 9)
                    //{
                    //    generatorStartTime.Value = (int)(startDate.AddHours(-5) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    //    generatorEndTime.Value = (int)(endDate.AddHours(-5) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    //}

                    //if (startDate.Hour == 14 && endDate.Hour == 13)
                    //{
                    //    generatorStartTime.Value = (int)(startDate.AddHours(-9) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    //    generatorEndTime.Value = (int)(endDate.AddHours(-9) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    //}

                    //if (startDate.Year == DateTime.UtcNow.Year && endDate.Year == DateTime.UtcNow.Year && DateTime.UtcNow > endDate)
                    if (startDate.Year > 2017 && DateTime.UtcNow > endDate && DateTime.UtcNow > startDate)
                    {
                        generatorStartTime.Value = (int)(startDate.AddYears(1) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                        generatorEndTime.Value = (int)(endDate.AddYears(1) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    }
                }

                var itemCurMana = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.ItemCurMana);
                var itemMaxMana = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.ItemMaxMana);
                if (itemMaxMana != null && itemCurMana == null)
                    weenie.WeeniePropertiesInt.Add(new ACE.Database.Models.World.WeeniePropertiesInt { ObjectId = weenie.ClassId, Type = (ushort)ACE.Entity.Enum.Properties.PropertyInt.ItemCurMana, Value = itemMaxMana.Value });


                //if (weenie.Type == (int)ACE.Entity.Enum.WeenieType.Key || weenie.Type == (int)ACE.Entity.Enum.WeenieType.Lockpick || weenie.Type == (int)ACE.Entity.Enum.WeenieType.Healer)
                //{
                //    var maxStructure = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.MaxStructure);
                //    if (maxStructure != null)
                //    {
                //        var structure = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.Structure);
                //        if (structure != null)
                //        {
                //            var value = weenie.WeeniePropertiesInt.FirstOrDefault(y => y.Type == (ushort)ACE.Entity.Enum.Properties.PropertyInt.Value);

                //            int calcValue = 0;
                //            int calcValuePerStructure = 0;

                //            if (value?.Value > 0)
                //            {
                //                calcValuePerStructure = value.Value / structure.Value;
                //                calcValue = calcValuePerStructure * (maxStructure.Value - structure.Value);

                //                value.Value += calcValue;
                //                if (maxStructure.Value > 2 && maxStructure.Value < 100)
                //                    value.Value = (int)(Math.Ceiling(value.Value / 5.0d) * 5);
                //            }
                //        }
                //        structure.Value = maxStructure.Value;
                //    }
                //}

                foreach (var emote in weenie.WeeniePropertiesEmote)
                {
                    foreach (var action in emote.WeeniePropertiesEmoteAction)
                    {
                        var emoteType = (ACE.Entity.Enum.EmoteType)action.Type;

                        // CreationProfile
                        if (
                               emoteType is ACE.Entity.Enum.EmoteType.Give
                            || emoteType is ACE.Entity.Enum.EmoteType.TakeItems
                            || emoteType is ACE.Entity.Enum.EmoteType.InqOwnsItems
                            )
                        {
                            if (action.DestinationType is null)
                                action.DestinationType = 0;
                            if (action.StackSize is null)
                                action.StackSize = 1;
                            if (action.Palette is null)
                                action.Palette = 0;
                            if (action.Shade is null)
                                action.Shade = 0;
                            if (action.TryToBond is null || action.TryToBond is true)
                                action.TryToBond = false;
                        }
                        else
                        {
                            if (action.DestinationType is not null)
                                action.DestinationType = null;
                            if (action.StackSize is not null)
                                action.StackSize = null;
                            if (action.Palette is not null)
                                action.Palette = null;
                            if (action.Shade is not null)
                                action.Shade = null;
                            if (action.TryToBond is not null)
                                action.TryToBond = null;
                        }

                        // Frame
                        if (
                               emoteType is ACE.Entity.Enum.EmoteType.MoveHome
                            || emoteType is ACE.Entity.Enum.EmoteType.Move
                            || emoteType is ACE.Entity.Enum.EmoteType.Turn
                            || emoteType is ACE.Entity.Enum.EmoteType.MoveToPos
                            )
                        {
                            if (action.OriginX is null)
                                action.OriginX = 0;
                            if (action.OriginY is null)
                                action.OriginY = 0;
                            if (action.OriginZ is null)
                                action.OriginZ = 0;
                            if (action.AnglesW is null)
                                action.AnglesW = 0;
                            if (action.AnglesX is null)
                                action.AnglesX = 0;
                            if (action.AnglesY is null)
                                action.AnglesY = 0;
                            if (action.AnglesZ is null)
                                action.AnglesZ = 0;
                            if (action.AnglesW == 0 && action.AnglesZ == 0)
                                action.AnglesW = 1;
                            if (action.ObjCellId is not null)
                                action.ObjCellId = null;
                        }
                        else if (
                                emoteType is not ACE.Entity.Enum.EmoteType.SetSanctuaryPosition
                             && emoteType is not ACE.Entity.Enum.EmoteType.TeleportTarget
                             && emoteType is not ACE.Entity.Enum.EmoteType.TeleportSelf
                            )
                        {
                            action.ObjCellId = null;
                            action.OriginX = null;
                            action.OriginY = null;
                            action.OriginZ = null;
                            action.AnglesW = null;
                            action.AnglesX = null;
                            action.AnglesY = null;
                            action.AnglesZ = null;
                        }

                        // Position
                        if (
                               emoteType is ACE.Entity.Enum.EmoteType.SetSanctuaryPosition
                            || emoteType is ACE.Entity.Enum.EmoteType.TeleportTarget
                            || emoteType is ACE.Entity.Enum.EmoteType.TeleportSelf
                            )
                        {
                            if (action.ObjCellId is null)
                                action.ObjCellId = 0x00000000;
                            if (action.OriginX is null)
                                action.OriginX = 0;
                            if (action.OriginY is null)
                                action.OriginY = 0;
                            if (action.OriginZ is null)
                                action.OriginZ = 0;
                            if (action.AnglesW is null)
                                action.AnglesW = 0;
                            if (action.AnglesX is null)
                                action.AnglesX = 0;
                            if (action.AnglesY is null)
                                action.AnglesY = 0;
                            if (action.AnglesZ is null)
                                action.AnglesZ = 0;
                            if (action.AnglesW == 0 && action.AnglesZ == 0)
                                action.AnglesW = 1;
                        }
                        else if (
                                emoteType is not ACE.Entity.Enum.EmoteType.MoveHome
                             && emoteType is not ACE.Entity.Enum.EmoteType.Move
                             && emoteType is not ACE.Entity.Enum.EmoteType.Turn
                             && emoteType is not ACE.Entity.Enum.EmoteType.MoveToPos
                            )
                        {
                            action.ObjCellId = null;
                            action.OriginX = null;
                            action.OriginY = null;
                            action.OriginZ = null;
                            action.AnglesW = null;
                            action.AnglesX = null;
                            action.AnglesY = null;
                            action.AnglesZ = null;
                        }

                        // Display cleanup
                        if (emoteType is not ACE.Entity.Enum.EmoteType.AwardLevelProportionalXP && emoteType is not ACE.Entity.Enum.EmoteType.AwardLevelProportionalSkillXP)
                            action.Display = null;

                        if (emoteType is ACE.Entity.Enum.EmoteType.AwardXP || emoteType is ACE.Entity.Enum.EmoteType.AwardNoShareXP)
                        {
                            if (action.Amount is not null)
                            {
                                action.Amount64 = action.Amount;
                                action.Amount = null;
                            }

                            if (action.Amount64 is null)
                                action.Amount64 = 0;

                            if (action.HeroXP64 is null)
                                action.HeroXP64 = 0;
                        }

                        if (emoteType is ACE.Entity.Enum.EmoteType.AwardLevelProportionalXP)
                        {
                            if (action.Min is not null)
                            {
                                action.Min64 = action.Min;
                                action.Min = null;
                            }

                            if (action.Max is not null)
                            {
                                action.Max64 = action.Max;
                                action.Max = null;
                            }

                            if (action.Min64 is null || action.Min64 == long.MinValue || action.Min64 == int.MinValue)
                                action.Min64 = 0;

                            if (action.Max64 is null || action.Max64 == long.MaxValue || action.Max64 == int.MaxValue)
                                action.Max64 = 0;

                            //if (action.Max64 >= 3390451400)
                            //    action.Max64 = 3390451400;

                            if (action.Percent is null)
                                action.Percent = 1;

                            if (action.Percent > 4 /*|| weenie.ClassId == 31933 /* The Deep, 3.5% to 0.035% */)
                                action.Percent *= 0.01;

                            action.Percent = Math.Round(action.Percent.Value, 3);

                            if (action.Display is null)
                                action.Display = false;
                        }

                        if (emoteType is ACE.Entity.Enum.EmoteType.AwardLuminance || emoteType is ACE.Entity.Enum.EmoteType.SpendLuminance)
                        {
                            if (action.Amount is not null)
                            {
                                action.Amount64 = action.Amount;
                                action.Amount = null;
                            }

                            if (action.HeroXP64 is not null)
                            {
                                action.Amount64 = action.HeroXP64;
                                action.HeroXP64 = null;
                            }
                        }

                        if (   emoteType is ACE.Entity.Enum.EmoteType.DecrementQuest
                            || emoteType is ACE.Entity.Enum.EmoteType.IncrementQuest
                            || emoteType is ACE.Entity.Enum.EmoteType.SetQuestCompletions
                            || emoteType is ACE.Entity.Enum.EmoteType.DecrementMyQuest
                            || emoteType is ACE.Entity.Enum.EmoteType.IncrementMyQuest
                            || emoteType is ACE.Entity.Enum.EmoteType.SetMyQuestCompletions
                            || emoteType is ACE.Entity.Enum.EmoteType.InqPackSpace
                            || emoteType is ACE.Entity.Enum.EmoteType.SetQuestCompletions
                            || emoteType is ACE.Entity.Enum.EmoteType.InqQuestBitsOn
                            || emoteType is ACE.Entity.Enum.EmoteType.InqQuestBitsOff
                            || emoteType is ACE.Entity.Enum.EmoteType.InqMyQuestBitsOn
                            || emoteType is ACE.Entity.Enum.EmoteType.InqMyQuestBitsOff
                            || emoteType is ACE.Entity.Enum.EmoteType.SetQuestBitsOn
                            || emoteType is ACE.Entity.Enum.EmoteType.SetQuestBitsOff
                            || emoteType is ACE.Entity.Enum.EmoteType.SetMyQuestBitsOn
                            || emoteType is ACE.Entity.Enum.EmoteType.SetMyQuestBitsOff
                            || emoteType is ACE.Entity.Enum.EmoteType.IncrementIntStat
                            || emoteType is ACE.Entity.Enum.EmoteType.DecrementIntStat
                            )
                        {
                            if (action.Amount64 is not null)
                            {
                                action.Amount = (int)action.Amount64;
                                action.Amount64 = null;
                            }

                            if (action.Amount is null)
                                action.Amount = 1;
                        }

                        if (   emoteType is ACE.Entity.Enum.EmoteType.InqQuestSolves
                            || emoteType is ACE.Entity.Enum.EmoteType.InqFellowNum
                            || emoteType is ACE.Entity.Enum.EmoteType.InqNumCharacterTitles
                            || emoteType is ACE.Entity.Enum.EmoteType.InqMyQuestSolves
                            )
                        {
                            if (action.Min64 is not null)
                            {
                                action.Min = (int)action.Min64;
                                action.Min64 = null;
                            }

                            if (action.Max64 is not null)
                            {
                                action.Max = (int)action.Max64;
                                action.Max64 = null;
                            }

                            if (action.Min is null || action.Min < 0)
                                action.Min = 1;

                            if (action.Max is null)
                                action.Max = int.MaxValue;
                        }

                        if (emoteType is ACE.Entity.Enum.EmoteType.InqInt64Stat)
                        {
                            if (action.Min is not null)
                            {
                                action.Min64 = action.Min;
                                action.Min = null;
                            }

                            if (action.Max is not null)
                            {
                                action.Max64 = action.Max;
                                action.Max = null;
                            }

                            if (action.Min64 is null || action.Min64 < 0)
                                action.Min64 = 0;

                            if (action.Max64 is null || action.Max64 == int.MaxValue)
                                action.Max64 = long.MaxValue;
                        }

                        if (   emoteType is ACE.Entity.Enum.EmoteType.InqIntStat
                            || emoteType is ACE.Entity.Enum.EmoteType.InqAttributeStat
                            || emoteType is ACE.Entity.Enum.EmoteType.InqRawAttributeStat
                            || emoteType is ACE.Entity.Enum.EmoteType.InqSecondaryAttributeStat
                            || emoteType is ACE.Entity.Enum.EmoteType.InqRawSecondaryAttributeStat
                            || emoteType is ACE.Entity.Enum.EmoteType.InqSkillStat
                            || emoteType is ACE.Entity.Enum.EmoteType.InqRawSkillStat
                            )
                        {
                            if (action.Min64 is not null)
                            {
                                action.Min = (int)action.Min64;
                                action.Min64 = null;
                            }

                            if (action.Max64 is not null)
                            {
                                action.Max = (int)action.Max64;
                                action.Max64 = null;
                            }

                            if (action.Min is null || action.Min < 0)
                                action.Min = 0;

                            if (action.Max is null)
                                action.Max = int.MaxValue;

                            if (action.Stat is null)
                                action.Stat = 0;
                        }
                    }
                }

                foreach (var item in weenie.WeeniePropertiesCreateList)
                {
                    if (item.TryToBond)
                        item.TryToBond = false;
                }

                var pcapBools = weenie.WeeniePropertiesBool.ToList();
                foreach (var prop in pcapBools)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesBool.Remove(prop);
                }
                var pcapDids = weenie.WeeniePropertiesDID.ToList();
                foreach (var prop in pcapDids)
                {
                    //if (prop.Type == 8044) continue;

                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesDID.Remove(prop);
                }
                var pcapFloats = weenie.WeeniePropertiesFloat.ToList();
                foreach (var prop in pcapFloats)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesFloat.Remove(prop);
                }
                var pcapIids = weenie.WeeniePropertiesIID.ToList();
                foreach (var prop in pcapIids)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesIID.Remove(prop);
                }
                var pcapInts = weenie.WeeniePropertiesInt.ToList();
                foreach (var prop in pcapInts)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesInt.Remove(prop);
                }
                var pcapInt64s = weenie.WeeniePropertiesInt64.ToList();
                foreach (var prop in pcapInt64s)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesInt64.Remove(prop);
                }
                var pcapPoss = weenie.WeeniePropertiesPosition.ToList();
                foreach (var prop in pcapPoss)
                {
                    if (prop.PositionType >= 8000 || prop.PositionType == 0)
                        weenie.WeeniePropertiesPosition.Remove(prop);
                }
                var pcapStrs = weenie.WeeniePropertiesString.ToList();
                foreach (var prop in pcapStrs)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesString.Remove(prop);
                }
                var pcapAtt = weenie.WeeniePropertiesAttribute.ToList();
                foreach (var prop in pcapAtt)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesAttribute.Remove(prop);
                }
                var pcapAtt2nd = weenie.WeeniePropertiesAttribute2nd.ToList();
                foreach (var prop in pcapAtt2nd)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesAttribute2nd.Remove(prop);
                }
                //var pcapBpp = weenie.WeeniePropertiesBodyPart.ToList();
                //foreach (var prop in pcapBpp)
                //{
                //    if (prop.Key == 0)
                //        weenie.WeeniePropertiesBodyPart.Remove(prop);
                //}
                var pcapSkills = weenie.WeeniePropertiesSkill.ToList();
                foreach (var prop in pcapSkills)
                {
                    if (prop.Type >= 8000 || prop.Type == 0)
                        weenie.WeeniePropertiesSkill.Remove(prop);
                }
                var pcapSpells = weenie.WeeniePropertiesSpellBook.ToList();
                foreach (var prop in pcapSpells)
                {
                    if (prop.Spell == 0)
                        weenie.WeeniePropertiesSpellBook.Remove(prop);
                }
                var pcapEmotes = weenie.WeeniePropertiesEmote.ToList();
                foreach (var prop in pcapEmotes)
                {
                    if (prop.Category == 0)
                        weenie.WeeniePropertiesEmote.Remove(prop);
                }
                var pcapEvtFilters = weenie.WeeniePropertiesEventFilter.ToList();
                foreach (var prop in pcapEvtFilters)
                {
                    if (prop.Event == 0)
                        weenie.WeeniePropertiesEventFilter.Remove(prop);
                }
            }
        }

        private void DeDupeWeenies(List<ACE.Database.Models.World.Weenie> cacheWeenies, List<ACE.Database.Models.World.Weenie> weenies, out List<ACE.Database.Models.World.Weenie> deDupedWeenies)
        {
            //var cacheWeenies = Globals.CacheBin.WeenieDefaults.ConvertToACE();

            //Globals.ACEDatabase.ReCacheAllWeeniesInParallel();

            //var results = Globals.ACEDatabase.WorldDatabase.GetAllWeenies();
            //.AsNoTracking()
            //.ToList();

            //var aceTreasureWielded = Globals.ACEDatabase.GetAllTreasureWielded();
            //var aceTreasureDeath = Globals.ACEDatabase.GetAllTreasureDeath();

            //var treasureWielded = new Dictionary<uint, List<ACE.Database.Models.World.TreasureWielded>>();
            //foreach (var item in aceTreasureWielded)
            //{
            //    if (!treasureWielded.ContainsKey(item.TreasureType))
            //        treasureWielded.Add(item.TreasureType, new List<ACE.Database.Models.World.TreasureWielded>());

            //    treasureWielded[item.TreasureType].Add(item);
            //}
            //var treasureDeath = new Dictionary<uint, ACE.Database.Models.World.TreasureDeath>();
            //foreach (var item in aceTreasureDeath)
            //{
            //    if (!treasureDeath.ContainsKey(item.TreasureType))
            //        treasureDeath.Add(item.TreasureType, item);
            //}

            //var deDupedWeenies = new List<ACE.Database.Models.World.Weenie>();
            deDupedWeenies = new List<ACE.Database.Models.World.Weenie>();

            foreach (var weenie in weenies)
            {
                var cW = cacheWeenies.FirstOrDefault(i => i.ClassId == weenie.ClassId);

                if (cW == null)
                {
                    deDupedWeenies.Add(weenie);
                    continue;
                }

                var notIdentical = false;
            addToList:
                if (notIdentical)
                {
                    deDupedWeenies.Add(weenie);
                    continue;
                }

                if (weenie.WeeniePropertiesAttribute.Count != cW.WeeniePropertiesAttribute.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesAttribute)
                {
                    var cWprop = cW.WeeniePropertiesAttribute.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (
                           prop.CPSpent != cWprop.CPSpent
                        || prop.InitLevel != cWprop.InitLevel
                        || prop.LevelFromCP != cWprop.LevelFromCP
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesAttribute2nd.Count != cW.WeeniePropertiesAttribute2nd.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesAttribute2nd)
                {
                    var cWprop = cW.WeeniePropertiesAttribute2nd.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (
                           prop.CPSpent != cWprop.CPSpent
                        || prop.InitLevel != cWprop.InitLevel
                        || prop.LevelFromCP != cWprop.LevelFromCP
                        || prop.CurrentLevel != cWprop.CurrentLevel
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesBodyPart.Count != cW.WeeniePropertiesBodyPart.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesBodyPart)
                {
                    var cWprop = cW.WeeniePropertiesBodyPart.FirstOrDefault(i => i.Key == prop.Key);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (
                           prop.ArmorVsAcid != cWprop.ArmorVsAcid
                        || prop.ArmorVsBludgeon != cWprop.ArmorVsBludgeon
                        || prop.ArmorVsCold != cWprop.ArmorVsCold
                        || prop.ArmorVsElectric != cWprop.ArmorVsElectric
                        || prop.ArmorVsFire != cWprop.ArmorVsFire
                        || prop.ArmorVsNether != cWprop.ArmorVsNether
                        || prop.ArmorVsPierce != cWprop.ArmorVsPierce
                        || prop.ArmorVsSlash != cWprop.ArmorVsSlash
                        || prop.BaseArmor != cWprop.BaseArmor
                        || prop.BH != cWprop.BH
                        || prop.DType != cWprop.DType
                        || prop.DVal != cWprop.DVal
                        || prop.DVar != cWprop.DVar
                        || prop.HLB != cWprop.HLB
                        || prop.HLF != cWprop.HLF
                        || prop.HRB != cWprop.HRB
                        || prop.HRF != cWprop.HRF
                        || prop.LLB != cWprop.LLB
                        || prop.LLF != cWprop.LLF
                        || prop.LRB != cWprop.LRB
                        || prop.LRF != cWprop.LRF
                        || prop.MLB != cWprop.MLB
                        || prop.MLF != cWprop.MLF
                        || prop.MRB != cWprop.MRB
                        || prop.MRF != cWprop.MRF
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesBook != null && cW.WeeniePropertiesBook == null
                    || weenie.WeeniePropertiesBook?.MaxNumCharsPerPage != cW.WeeniePropertiesBook?.MaxNumCharsPerPage
                    || weenie.WeeniePropertiesBook?.MaxNumPages != cW.WeeniePropertiesBook?.MaxNumPages)
                {
                    notIdentical = true;
                    goto addToList;
                }

                if (weenie.WeeniePropertiesBookPageData.Count != cW.WeeniePropertiesBookPageData.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesBookPageData)
                {
                    var cWprop = cW.WeeniePropertiesBookPageData.FirstOrDefault(i => i.PageId == prop.PageId);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (
                           prop.AuthorAccount != cWprop.AuthorAccount
                        || prop.AuthorId != cWprop.AuthorId
                        || prop.AuthorName != cWprop.AuthorName
                        || prop.IgnoreAuthor != cWprop.IgnoreAuthor
                        || prop.PageText != cWprop.PageText
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesBool.Count != cW.WeeniePropertiesBool.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesBool)
                {
                    var cWprop = cW.WeeniePropertiesBool.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (prop.Value != cWprop.Value)
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesCreateList.Count != cW.WeeniePropertiesCreateList.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                //foreach (var prop in weenie.WeeniePropertiesCreateList.OrderBy(x => x.DestinationType))
                //{
                //    var cWprop = cW.WeeniePropertiesBool.FirstOrDefault(i => i.Type == prop.Type);

                //    if (cWprop == null)
                //    {
                //        deDupe = true;
                //        goto deDupe;
                //    }

                //    if (prop.Value != cWprop.Value)
                //    {
                //        deDupe = true;
                //        goto deDupe;
                //    }
                //}

                for (var i = 0; i < weenie.WeeniePropertiesCreateList.Count; i++)
                {
                    var cWprop = cW.WeeniePropertiesCreateList.OrderBy(x => x.DestinationType).ElementAt(i);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    var prop = weenie.WeeniePropertiesCreateList.OrderBy(x => x.DestinationType).ElementAt(i);

                    if (
                           prop.DestinationType != cWprop.DestinationType
                        || prop.Palette != cWprop.Palette
                        || !ApproximatelyEqual(prop.Shade, cWprop.Shade)
                        || prop.StackSize != cWprop.StackSize
                        || prop.TryToBond != cWprop.TryToBond
                        || prop.WeenieClassId != cWprop.WeenieClassId
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesDID.Count != cW.WeeniePropertiesDID.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesDID)
                {
                    var cWprop = cW.WeeniePropertiesDID.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (prop.Value != cWprop.Value)
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesEmote.Count != cW.WeeniePropertiesEmote.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                //foreach (var prop in weenie.WeeniePropertiesEmote)
                //{
                //    var cWprop = cW.WeeniePropertiesDID.FirstOrDefault(i => i.Type == prop.Type);

                //    if (cWprop == null)
                //    {
                //        deDupe = true;
                //        goto deDupe;
                //    }

                //    if (prop.Value != cWprop.Value)
                //    {
                //        deDupe = true;
                //        goto deDupe;
                //    }
                //}

                for (var i = 0; i < weenie.WeeniePropertiesEmote.Count; i++)
                {
                    var cWprop = cW.WeeniePropertiesEmote.OrderBy(x => x.Category).ElementAt(i);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    var prop = weenie.WeeniePropertiesEmote.OrderBy(x => x.Category).ElementAt(i);

                    if (
                           prop.Category != cWprop.Category
                        || !ApproximatelyEqual(prop.MaxHealth, cWprop.MaxHealth) //prop.MaxHealth != cWprop.MaxHealth
                        || !ApproximatelyEqual(prop.MinHealth, cWprop.MinHealth) //prop.MinHealth != cWprop.MinHealth
                        || !ApproximatelyEqual(prop.Probability, cWprop.Probability)
                        || prop.Quest != cWprop.Quest
                        || prop.Style != cWprop.Style
                        || prop.Substyle != cWprop.Substyle
                        || prop.VendorType != cWprop.VendorType
                        || prop.WeenieClassId != cWprop.WeenieClassId
                        || prop.WeeniePropertiesEmoteAction.Count != cWprop.WeeniePropertiesEmoteAction.Count
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    for (var y = 0; y < prop.WeeniePropertiesEmoteAction.Count; y++)
                    {
                        var cWaction = cWprop.WeeniePropertiesEmoteAction.OrderBy(x => x.Order).ElementAt(y);

                        if (cWaction == null)
                        {
                            notIdentical = true;
                            goto addToList;
                        }

                        var action = prop.WeeniePropertiesEmoteAction.OrderBy(x => x.Order).ElementAt(y);

                        if (
                              action.Amount != cWaction.Amount
                           || action.Amount64 != cWaction.Amount64
                           || !ApproximatelyEqual(action.AnglesW, cWaction.AnglesW)
                           || !ApproximatelyEqual(action.AnglesX, cWaction.AnglesX)
                           || !ApproximatelyEqual(action.AnglesY, cWaction.AnglesY)
                           || !ApproximatelyEqual(action.AnglesZ, cWaction.AnglesZ)
                           || !ApproximatelyEqual(action.Delay, cWaction.Delay)
                           || action.DestinationType != cWaction.DestinationType
                           || action.Display != cWaction.Display
                           || !ApproximatelyEqual(action.Extent, cWaction.Extent)
                           || action.HeroXP64 != cWaction.HeroXP64
                           || action.Max != cWaction.Max
                           || action.Max64 != cWaction.Max64
                           || action.MaxDbl != cWaction.MaxDbl
                           || action.Message != cWaction.Message
                           || action.Min != cWaction.Min
                           || action.Min64 != cWaction.Min64
                           || action.MinDbl != cWaction.MinDbl
                           || action.Motion != cWaction.Motion
                           || action.ObjCellId != cWaction.ObjCellId
                           || action.Order != cWaction.Order
                           || !ApproximatelyEqual(action.OriginX, cWaction.OriginX)
                           || !ApproximatelyEqual(action.OriginY, cWaction.OriginY)
                           || !ApproximatelyEqual(action.OriginZ, cWaction.OriginZ)
                           || action.Palette != cWaction.Palette
                           || action.Percent != cWaction.Percent
                           || action.PScript != cWaction.PScript
                           || !ApproximatelyEqual(action.Shade, cWaction.Shade)
                           || action.SpellId != cWaction.SpellId
                           || action.StackSize != cWaction.StackSize
                           || action.Stat != cWaction.Stat
                           || action.TestString != cWaction.TestString
                           || action.TreasureClass != cWaction.TreasureClass
                           || action.TreasureType != cWaction.TreasureType
                           || action.TryToBond != cWaction.TryToBond
                           || action.Type != cWaction.Type
                           || action.WealthRating != cWaction.WealthRating
                           || action.WeenieClassId != cWaction.WeenieClassId
                           )
                        {
                            notIdentical = true;
                            goto addToList;
                        }
                    }
                }

                if (weenie.WeeniePropertiesEventFilter.Count != cW.WeeniePropertiesEventFilter.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesEventFilter)
                {
                    var cWprop = cW.WeeniePropertiesEventFilter.FirstOrDefault(i => i.Event == prop.Event);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    //if (prop.Event != cWprop.Event)
                    //{
                    //    deDupe = true;
                    //    goto deDupe;
                    //}
                }

                if (weenie.WeeniePropertiesFloat.Count != cW.WeeniePropertiesFloat.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesFloat)
                {
                    var cWprop = cW.WeeniePropertiesFloat.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (!ApproximatelyEqual((float)prop.Value, (float)cWprop.Value, 3))
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesGenerator.Count != cW.WeeniePropertiesGenerator.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                //foreach (var prop in weenie.WeeniePropertiesGenerator)
                //{
                //    //var cWprop = cW.WeeniePropertiesDID.FirstOrDefault(i => i.Type == prop.Type);

                //    //if (cWprop == null)
                //    //{
                //    //    deDupe = true;
                //    //    goto deDupe;
                //    //}

                //    //if (prop.Value != cWprop.Value)
                //    //{
                //    //    deDupe = true;
                //    //    goto deDupe;
                //    //}


                //}

                for (var i = 0; i < weenie.WeeniePropertiesGenerator.Count; i++)
                {
                    var cWprop = cW.WeeniePropertiesGenerator.ElementAt(i);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    var prop = weenie.WeeniePropertiesGenerator.ElementAt(i);

                    if (
                           !ApproximatelyEqual(prop.AnglesW, cWprop.AnglesW)
                        || !ApproximatelyEqual(prop.AnglesX, cWprop.AnglesX)
                        || !ApproximatelyEqual(prop.AnglesY, cWprop.AnglesY)
                        || !ApproximatelyEqual(prop.AnglesZ, cWprop.AnglesZ)
                        || !ApproximatelyEqual(prop.Delay, cWprop.Delay)
                        || prop.InitCreate != cWprop.InitCreate
                        || prop.MaxCreate != cWprop.MaxCreate
                        || prop.ObjCellId != cWprop.ObjCellId
                        || !ApproximatelyEqual(prop.OriginX, cWprop.OriginX)
                        || !ApproximatelyEqual(prop.OriginY, cWprop.OriginY)
                        || !ApproximatelyEqual(prop.OriginZ, cWprop.OriginZ)
                        || prop.PaletteId != cWprop.PaletteId
                        || !ApproximatelyEqual(prop.Probability, cWprop.Probability)
                        || !ApproximatelyEqual(prop.Shade, cWprop.Shade)
                        || prop.StackSize != cWprop.StackSize
                        || prop.WeenieClassId != cWprop.WeenieClassId
                        || prop.WhenCreate != cWprop.WhenCreate
                        || prop.WhereCreate != cWprop.WhereCreate
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesIID.Count != cW.WeeniePropertiesIID.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesIID)
                {
                    var cWprop = cW.WeeniePropertiesIID.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (prop.Value != cWprop.Value)
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesInt.Count != cW.WeeniePropertiesInt.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesInt)
                {
                    var cWprop = cW.WeeniePropertiesInt.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (prop.Value != cWprop.Value)
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesInt64.Count != cW.WeeniePropertiesInt64.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesInt64)
                {
                    var cWprop = cW.WeeniePropertiesInt64.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (prop.Value != cWprop.Value)
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesPosition.Count != cW.WeeniePropertiesPosition.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesPosition)
                {
                    var cWprop = cW.WeeniePropertiesPosition.FirstOrDefault(i => i.PositionType == prop.PositionType);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (
                        prop.ObjCellId != cWprop.ObjCellId
                        || !ApproximatelyEqual(prop.AnglesW, cWprop.AnglesW)
                        || !ApproximatelyEqual(prop.AnglesX, cWprop.AnglesX)
                        || !ApproximatelyEqual(prop.AnglesY, cWprop.AnglesY)
                        || !ApproximatelyEqual(prop.AnglesZ, cWprop.AnglesZ)
                        || !ApproximatelyEqual(prop.OriginX, cWprop.OriginX)
                        || !ApproximatelyEqual(prop.OriginY, cWprop.OriginY)
                        || !ApproximatelyEqual(prop.OriginZ, cWprop.OriginZ)
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesSkill.Count != cW.WeeniePropertiesSkill.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesSkill)
                {
                    var cWprop = cW.WeeniePropertiesSkill.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (
                           prop.InitLevel != cWprop.InitLevel
                        || prop.LastUsedTime != cWprop.LastUsedTime
                        || prop.LevelFromPP != cWprop.LevelFromPP
                        || prop.PP != cWprop.PP
                        || prop.ResistanceAtLastCheck != cWprop.ResistanceAtLastCheck
                        || prop.SAC != cWprop.SAC
                        )
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesSpellBook.Count != cW.WeeniePropertiesSpellBook.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesSpellBook)
                {
                    var cWprop = cW.WeeniePropertiesSpellBook.FirstOrDefault(i => i.Spell == prop.Spell);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (prop.Probability != cWprop.Probability)
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }

                if (weenie.WeeniePropertiesString.Count != cW.WeeniePropertiesString.Count)
                {
                    notIdentical = true;
                    goto addToList;
                }

                foreach (var prop in weenie.WeeniePropertiesString)
                {
                    var cWprop = cW.WeeniePropertiesString.FirstOrDefault(i => i.Type == prop.Type);

                    if (cWprop == null)
                    {
                        notIdentical = true;
                        goto addToList;
                    }

                    if (prop.Value != cWprop.Value)
                    {
                        notIdentical = true;
                        goto addToList;
                    }
                }
            }

            //foreach (var thing in deDupedWeenies)
            //    thing.LastModified = new DateTime(2021, 11, 1);
        }

        public static void DeleteEmptySubdirectories(string parentDirectory)
        {
            System.Threading.Tasks.Parallel.ForEach(System.IO.Directory.GetDirectories(parentDirectory), directory => {
                DeleteEmptySubdirectories(directory);
                if (!System.IO.Directory.EnumerateFileSystemEntries(directory).Any()) System.IO.Directory.Delete(directory, false);
            });
        }

        private bool baselineExport = false;

        private void cmdACEAMutationParse_Click(object sender, EventArgs e)
        {
            //taskA = Task.Run(() => Console.WriteLine("Hello from taskA."));

            cmdACEAMutationParse.Enabled = false;

            ////var esFiles = Directory.GetFiles(@"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches", "*.es", new EnumerationOptions { RecurseSubdirectories = true });
            //var esFiles = Directory.EnumerateFiles(@"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches", "*.es", SearchOption.AllDirectories);
            //foreach(var file in esFiles)
            //{
            //    var rootToRemove = @"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches\";
            //    //var x = file.Remove(rootToRemove.Length);
            //    var currentFileNameAndPath = file[rootToRemove.Length..file.Length];
            //    //Console.WriteLine(x);
            //    var newRoot = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
            //    var fileInfo = new FileInfo(currentFileNameAndPath);
            //    var fileName = fileInfo.Name;
            //    var fileDirectory = newRoot + currentFileNameAndPath[0..^(fileName.Length + 1)];

            //    Directory.CreateDirectory(fileDirectory);

            //    var outputFile = fileDirectory + "\\" + fileName;

            //    File.Copy(file, outputFile);
            //}

            if (!baselineExport && txtACEExportwcidStart.Text == "0")
            {
                txtACEDatabaseConnector.Text += Environment.NewLine + $"You must specify which PR to mutate from in the Start WCID text box!!!" + Environment.NewLine;
                cmdACEAMutationParse.Enabled = true;
                return;
            }

            try
            {
                if (!baselineExport)
                {
                    txtACEDatabaseConnector.Text += Environment.NewLine + $"Attempting to grab latest build of PR #{txtACEExportwcidStart.Text}... ";
                    //var url = "https://api.github.com/repos/ACEmulator/ACE-World-16PY-Patches/releases";
                    var ci = File.ReadAllText(@"C:\ACE\avt.txt");
                    var url = "https://ci.appveyor.com/api/projects/LtRipley36706/ACE-World-16PY-Patches/history?recordsNumber=20";
                    using var client = new WebClient();
                    client.AddCIHeaders(ci);
                    var html = Task.Run(() => client.GetStringFromURL(url)).Result;

                    dynamic json = JsonConvert.DeserializeObject(html);
                    //Console.WriteLine();
                    var version = "";
                    foreach (var build in json.builds)
                    {
                        //Console.WriteLine(build);
                        if (build.pullRequestId == txtACEExportwcidStart.Text)
                        {
                            //Console.WriteLine("found pr");                        
                            version = build.version;
                            txtACEDatabaseConnector.Text += Environment.NewLine + $"Found PR #{txtACEExportwcidStart.Text}! version = {version}";
                            break;
                        }
                    }

                    if (version == "")
                    {
                        txtACEDatabaseConnector.Text += Environment.NewLine + $"Unable to find PR #{txtACEExportwcidStart.Text}!" + Environment.NewLine;
                        return;
                    }

                    url = $"https://ci.appveyor.com/api/projects/LtRipley36706/ACE-World-16PY-Patches/build/{version}";
                    html = Task.Run(() => client.GetStringFromURL(url)).Result;
                    json = JsonConvert.DeserializeObject(html);
                    //Console.WriteLine();
                    var jobId = json.build.jobs[0].jobId;
                    txtACEDatabaseConnector.Text += $" | jobId = {jobId}";

                    url = $"https://ci.appveyor.com/api/buildjobs/{jobId}/artifacts";
                    html = Task.Run(() => client.GetStringFromURL(url)).Result;
                    json = JsonConvert.DeserializeObject(html);
                    //Console.WriteLine();
                    var fileName = "";
                    foreach (var file in json)
                    {
                        //Console.WriteLine(file[0].Value);
                        //Console.WriteLine(file[1].Value);
                        if (file.name == "World Database")
                        {
                            //Console.WriteLine("found pr");
                            //version = build.version;
                            fileName = file.fileName;
                            txtACEDatabaseConnector.Text += $" | fileName = {fileName}";
                            break;
                        }
                    }

                    if (fileName == "")
                    {
                        txtACEDatabaseConnector.Text += Environment.NewLine + $"Unable to find World Database attached to PR #{txtACEExportwcidStart.Text}!" + Environment.NewLine;
                        return;
                    }

                    url = $"https://ci.appveyor.com/api/buildjobs/{jobId}/artifacts/{fileName}";

                    client.RemoveCIHeaders();

                    //DownloadAndImportDatabase(url, fileName, "ace_world");
                    if (DatabaseNeedsUpdate("ace_world", "v" + version))
                        DownloadAndImportDatabase(url, fileName, "ace_world");
                    else
                        txtACEDatabaseConnector.Text += Environment.NewLine + $"Found {version} currently installed in ace_world database. Skipping update!";

                    txtACEDatabaseConnector.Text += Environment.NewLine + $"Attempting to grab most recent release for ACE World Database... ";
                    url = "https://api.github.com/repos/ACEmulator/ACE-World-16PY-Patches/releases";
                    html = Task.Run(() => client.GetStringFromURL(url)).Result;

                    json = JsonConvert.DeserializeObject(html);
                    string tag = json[0].tag_name;
                    string dbURL = json[0].assets[0].browser_download_url;
                    string dbFileName = json[0].assets[0].name;

                    txtACEDatabaseConnector.Text += Environment.NewLine + $"Found release {tag}!";

                    //DownloadAndImportDatabase(dbURL, dbFileName, "ace_world_prev");
                    if (DatabaseNeedsUpdate("ace_world_prev", tag))
                        DownloadAndImportDatabase(dbURL, dbFileName, "ace_world_prev");
                    else
                        txtACEDatabaseConnector.Text += Environment.NewLine + $"Found {tag} currently installed in ace_world_prev database. Skipping update!";

                    txtACEDatabaseConnector.Text += Environment.NewLine + $"Attempting to grab most recent release for ACE World Database Base... ";
                    url = "https://api.github.com/repos/ACEmulator/ACE-World-16PY/releases";
                    html = Task.Run(() => client.GetStringFromURL(url)).Result;

                    json = JsonConvert.DeserializeObject(html);
                    tag = json[0].tag_name;
                    dbURL = json[0].assets[0].browser_download_url;
                    dbFileName = json[0].assets[0].name;

                    txtACEDatabaseConnector.Text += Environment.NewLine + $"Found release {tag}!";

                    //DownloadAndImportDatabase(dbURL, dbFileName, "ace_world_prev");
                    if (DatabaseNeedsUpdate("ace_world_16py", tag))
                        DownloadAndImportDatabase(dbURL, dbFileName, "ace_world_16py");
                    else
                        txtACEDatabaseConnector.Text += Environment.NewLine + $"Found {tag} currently installed in ace_world_16py database. Skipping update!";

                    txtACEDatabaseConnector.Text += Environment.NewLine + $"Clearing output directory... ";
                    var di = new DirectoryInfo((string)Settings.Default["GDLESQLOutputFolder"]);
                    foreach (var file in di.EnumerateFiles())
                    {
                        file.Delete();
                    }
                    foreach (var dir in di.EnumerateDirectories())
                    {
                        dir.Delete(true);
                    }
                    txtACEDatabaseConnector.Text += "Cleared!" + Environment.NewLine;                    
                }

                ////var esFiles = Directory.GetFiles(@"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches", "*.es", new EnumerationOptions { RecurseSubdirectories = true });
                //var esFiles = Directory.EnumerateFiles(@"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches", "*.es", SearchOption.AllDirectories);
                //foreach (var file in esFiles)
                //{
                //    var rootToRemove = @"C:\Users\tycon\source\repos\LtRipley36706\ACE-World-16PY-Patches\Database\Patches\";
                //    //var x = file.Remove(rootToRemove.Length);
                //    var currentFileNameAndPath = file[rootToRemove.Length..file.Length];
                //    //Console.WriteLine(x);
                //    var newRoot = Settings.Default["GDLESQLOutputFolder"] + "\\C EmoteScript\\";
                //    var fileInfo = new FileInfo(currentFileNameAndPath);
                //    var fileNameES = fileInfo.Name;
                //    var fileDirectory = newRoot + currentFileNameAndPath[0..^(fileNameES.Length + 1)];

                //    Directory.CreateDirectory(fileDirectory);

                //    var outputFile = fileDirectory + "\\" + fileNameES;

                //    File.Copy(file, outputFile);
                //}

                if (!baselineExport)
                {
                    usePrevVersion = true;
                    writeDeletedFiles = true;
                    //doDateUpdate = true;

                    txtACEDatabaseConnector.Text += Environment.NewLine + "Starting data normalization";

                    if (doDateUpdate)
                        txtACEDatabaseConnector.Text += " and updating last_Modified field for release ... ";
                    else
                        txtACEDatabaseConnector.Text += " ... ";

                    cmdACE9WeeniesParse_Click(sender, e);
                    cmdACE1RegionsParse_Click(sender, e);
                    cmdACE2SpellsParse_Click(sender, e);
                    cmdACE3TreasureParse_Click(sender, e);
                    cmdACE4CraftingParse_Click(sender, e);
                    cmdACE5HousingParse_Click(sender, e);
                    cmdACE6LandblocksParse_Click(sender, e);
                    cmdACE8QuestsParse_Click(sender, e);
                    cmdACEBEventsParse_Click(sender, e);
                    usePrevVersion = false;
                    writeDeletedFiles = false;
                    doDateUpdate = false;
                }

                txtACEDatabaseConnector.Text += "data normalization and updates complete!" + Environment.NewLine;
            }
            catch
            {

            }

            cmdACEAMutationParse.Enabled = true;
        }

        private bool DatabaseNeedsUpdate(string dbName, string version)
        {
            var sqlConnect = new MySql.Data.MySqlClient.MySqlConnection($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};DefaultCommandTimeout=120");

            sqlConnect.Open();

            //string sql = "SELECT COUNT(*) FROM Country";
            //var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, sqlConnect);
            //object result = cmd.ExecuteScalar();
            //if (result != null)
            //{
            //    int r = Convert.ToInt32(result);
            //    Console.WriteLine("Number of countries in the world database is: " + r);
            //}

            try
            {
                var sql = $"SELECT `patch_Version` FROM {dbName}.version;";
                var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, sqlConnect);
                var result = cmd.ExecuteScalar() as string;

                if (result != null && result.Equals(version))
                    return false;

                if (result == null)
                {
                    sql = $"SELECT `base_Version` FROM {dbName}.version;";
                    cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, sqlConnect);
                    result = cmd.ExecuteScalar() as string;

                    if (result != null && result.Equals(version))
                        return false;
                }
            }
            catch (Exception)
            {

            }

            sqlConnect.Close();

            return true;
        }

        private void DownloadAndImportDatabase(string dbURL, string dbFileName, string dbName)
        {
            //Console.WriteLine();
            txtACEDatabaseConnector.Text += Environment.NewLine;

            //if (IsRunningInContainer)
            //{
            //    Console.WriteLine(" ");
            //    Console.WriteLine("This process will take a while, depending on many factors, and may look stuck while reading and importing the world database, please be patient! ");
            //    Console.WriteLine(" ");
            //}

            //Console.Write($"Downloading {dbFileName} .... ");
            txtACEDatabaseConnector.Text += $"Downloading {dbFileName} .... ";
            using (var client = new WebClient())
            {
                try
                {
                    Task.Run(() => client.DownloadFile(dbURL, dbFileName)).Wait();
                }
                catch
                {
                    //Console.Write($"Download for {dbFileName} failed!");
                    txtACEDatabaseConnector.Text += $"Download for {dbFileName} failed!";
                    return;
                }
            }
            //Console.WriteLine("download complete!");
            txtACEDatabaseConnector.Text += "download complete!" + Environment.NewLine;

            //Console.Write($"Extracting {dbFileName} .... ");
            txtACEDatabaseConnector.Text += $"Extracting {dbFileName} .... ";
            ZipFile.ExtractToDirectory(dbFileName, ".", true);
            //Console.WriteLine("extraction complete!");
            txtACEDatabaseConnector.Text += "extraction complete!" + Environment.NewLine;
            //Console.Write($"Deleting {dbFileName} .... ");
            txtACEDatabaseConnector.Text += $"Deleting {dbFileName} .... ";
            File.Delete(dbFileName);
            //Console.WriteLine("Deleted!");
            txtACEDatabaseConnector.Text += "Deleted!" + Environment.NewLine;

            var sqlFile = dbFileName.Substring(0, dbFileName.Length - 4);
            //Console.Write($"Importing {sqlFile} into SQL server at {ConfigManager.Config.MySql.World.Host}:{ConfigManager.Config.MySql.World.Port} (This will take a while, please be patient) .... ");
            txtACEDatabaseConnector.Text += $"Importing {sqlFile} into SQL server at {Settings.Default.ACEWorldServer}:{Settings.Default.ACEWorldPort} as {dbName} (This will take a while, please be patient) .... ";
            using (var sr = File.OpenText(sqlFile))
            {
                //var sqlConnect = new MySql.Data.MySqlClient.MySqlConnection($"server={ConfigManager.Config.MySql.World.Host};port={ConfigManager.Config.MySql.World.Port};user={ConfigManager.Config.MySql.World.Username};password={ConfigManager.Config.MySql.World.Password};DefaultCommandTimeout=120");
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev");
                var sqlConnect = new MySql.Data.MySqlClient.MySqlConnection($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};DefaultCommandTimeout=120");

                var line = string.Empty;
                var completeSQLline = string.Empty;

                //var dbname = ConfigManager.Config.MySql.World.Database;

                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Replace("ace_world", dbName);
                    //do minimal amount of work here
                    if (line.EndsWith(";"))
                    {
                        completeSQLline += line + Environment.NewLine;

                        var script = new MySql.Data.MySqlClient.MySqlScript(sqlConnect, completeSQLline);
                        try
                        {
                            script.StatementExecuted += new MySql.Data.MySqlClient.MySqlStatementExecutedEventHandler(OnStatementExecutedOutputDot);
                            var count = script.Execute();
                        }
                        catch (MySql.Data.MySqlClient.MySqlException)
                        {

                        }
                        completeSQLline = string.Empty;
                    }
                    else
                        completeSQLline += line + Environment.NewLine;
                }
            }
            //Console.WriteLine(" complete!");
            txtACEDatabaseConnector.Text += " complete!" + Environment.NewLine;

            //Console.Write($"Deleting {sqlFile} .... ");
            txtACEDatabaseConnector.Text += $"Deleting {sqlFile} .... ";
            File.Delete(sqlFile);
            //Console.WriteLine("Deleted!");
            txtACEDatabaseConnector.Text += "Deleted!" + Environment.NewLine;
        }

        private void OnStatementExecutedOutputDot(object sender, MySql.Data.MySqlClient.MySqlScriptEventArgs args)
        {
            //Console.Write(".");
            txtACEDatabaseConnector.Text += ".";
        }

        private void cmdACEBEventsParse_Click(object sender, EventArgs e)
        {
            cmdACEBEventsParse.Enabled = false;

            txtACEDatabaseConnector.Text += Environment.NewLine + "Exporting events from database... ";

            var cacheEvents = Globals.CacheBin.GameEventDefDB.ConvertToACE();

            var results = Globals.ACEDatabase.WorldDbContext.Event
                    .AsNoTracking()
                    .ToList();

            DeDupeEvents(cacheEvents, results, out var deDupedEvents);

            //foreach (var thing in deDuped)
            //    thing.LastModified = new DateTime(2021, 11, 1);

            if (deDupedEvents.Count > 0)
                //EventSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\B GameEventDefDB\\SQL\\", true);
                EventSQLWriter.WriteFiles(deDupedEvents, Settings.Default["GDLESQLOutputFolder"] + "\\B GameEventDefDB\\", true);

            if (usePrevVersion)
            {
                var connectionString = $"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database=ace_world_prev;TreatTinyAsBoolean=False";
                var optionsBuilder = new DbContextOptionsBuilder<ACE.Database.Models.World.WorldDbContext>();
                //optionsBuilder.UseMySql($"server={Settings.Default.ACEWorldServer};port={Settings.Default.ACEWorldPort};user={Settings.Default.ACEWorldUser};password={Settings.Default.ACEWorldPassword};database={Settings.Default.ACEWorldDatabase}");
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var prevContext = new ACE.Database.Models.World.WorldDbContext(optionsBuilder.Options);
                prevContext.Event.Load();
                var prevEvents = prevContext.Event.ToList();

                DeDupeEvents(prevEvents, deDupedEvents, out deDupedEvents);

                if (doDateUpdate)
                {
                    foreach (var thing in deDupedEvents)
                        thing.LastModified = GetTimestampForExport();
                }
            }

            if (deDupedEvents.Count > 0)
                //EventSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\B GameEventDefDB\\SQL\\", true);
                EventSQLWriter.WriteFiles(deDupedEvents, Settings.Default["GDLESQLOutputFolder"] + "\\B GameEventDefDB\\", true);

            var cacheIds = cacheEvents.Select(x => x.Name.ToUpper()).ToHashSet();
            var spellIds = results.Select(x => x.Name.ToUpper()).ToHashSet();
            var deDupeIds = deDupedEvents.Select(x => x.Name.ToUpper()).ToHashSet();

            var deletedIds = cacheIds.Except(spellIds).Except(deDupeIds).ToHashSet();

            txtACEDatabaseConnector.Text += $" completed. {deDupedEvents.Count:N0} events exported." + Environment.NewLine;

            txtACEDatabaseConnector.Text += $"Skipped {cacheIds.Except(deDupeIds).Except(deletedIds).Count():N0} unchanged events." + Environment.NewLine;

            cmdACEBEventsParse.Enabled = true;
        }

        private void DeDupeEvents(List<ACE.Database.Models.World.Event> cacheEvents, List<ACE.Database.Models.World.Event> events, out List<ACE.Database.Models.World.Event> deDupedEvents)
        {
            var x = cacheEvents.ToDictionary(z => z.Name.ToLower(), z => z);

            deDupedEvents = new List<ACE.Database.Models.World.Event>();

            foreach (var q in events)
            {
                var z = q.Name.ToLower();

                if (x.ContainsKey(z))
                {
                    if (!q.Name.Equals(x[z].Name))
                        q.Name = x[z].Name;

                    if (x[z].StartTime != q.StartTime || x[z].EndTime != q.EndTime || x[z].State != q.State)
                        deDupedEvents.Add(q);
                }
                else
                    deDupedEvents.Add(q);
            }
        }


        // ====================================================================================
        // =================================== Output Tools ===================================
        // ====================================================================================

        private void cmdOutputTool1_Click(object sender, EventArgs e)
        {
            cmdOutputTool1.Enabled = false;

            txtOutputTool1.Text = null;

            try
            {
                // Get all landblock guids currently in use in the database
                var landblockGuidsUsed = new List<uint>();

                if (Globals.ACEDatabase.WorldDbContext != null)
                {
                    var results = Globals.ACEDatabase.WorldDbContext.LandblockInstance
                        //.Include(r => r.LandblockInstanceLink) // UNCOMMENT THIS IF YOU WANT TO INCLUDE LINKS
                        .AsNoTracking()
                        .ToList();

                    foreach (var result in results)
                        landblockGuidsUsed.Add(result.Guid);
                }


                // Write out the SQL files

                //SpellsSQLWriter.WriteFiles(spells, lblGDLESQLOutputFolder.Text + "\\Database\\Spells\\", weenieNames);

                WeenieSQLWriter.WriteFiles(Globals.GDLE.Weenies, lblGDLESQLOutputFolder.Text + "\\Database\\Weenies\\", Globals.WeenieNames);

                // What about links??
                LandblockSQLWriter.WriteFiles(Globals.GDLE.Instances, lblGDLESQLOutputFolder.Text + "\\Database\\Landblock Instances\\", Globals.WeenieNames);

                // TODO
            }
            catch (Exception ex)
            {
                txtOutputTool1.Text += Environment.NewLine + ex;
            }


            cmdOutputTool1.Enabled = true;
        }

        private void cmdACEExportWeenieAsJson_Click(object sender, EventArgs e)
        {
            cmdACEExportWeenieAsJson.Enabled = false;

            var outputFolder = Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\JSON\\";

            var hasValidStart = uint.TryParse(txtACEExportwcidStart.Text, out uint wcidStart);
            var hasValidEnd = uint.TryParse(txtACEExportwcidEnd.Text, out uint wcidEnd);

            if (hasValidStart && wcidStart > 0)
            {
                if (hasValidEnd && wcidEnd > 0 && wcidStart < wcidEnd)
                {
                    var weenies = Globals.ACEDatabase.GetAllWeeniesBetween(wcidStart, wcidEnd);

                    foreach (var weenie in weenies)
                    {
                        var fullWeenie = Globals.ACEDatabase.GetWeenie(weenie.ClassId);
                        WriteWeenieAsJSON(fullWeenie, outputFolder);
                    }
                }
                else
                {
                    var weenie = Globals.ACEDatabase.GetWeenie(wcidStart);

                    WriteWeenieAsJSON(weenie, outputFolder);
                }
            }
            else if (wcidStart == wcidEnd)
            {
                if (wcidStart == wcidEnd && wcidStart == 0)
                {
                    var weenies = Globals.ACEDatabase.GetAllWeenies();

                    foreach (var weenie in weenies)
                    {
                        var fullWeenie = Globals.ACEDatabase.GetWeenie(weenie.ClassId);
                        WriteWeenieAsJSON(fullWeenie, outputFolder);
                    }
                }
                //else
                //{

                //}
            }

            cmdACEExportWeenieAsJson.Enabled = true;
        }

        private void WriteWeenieAsJSON(ACE.Database.Models.World.Weenie weenie, string outputFolder)
        {
            if (ACE.Adapter.Lifestoned.LifestonedConverter.TryConvertACEWeenieToLSDJSON(weenie, out var jsonString, out var _))
            {
                var sqlWriter = new ACE.Database.SQLFormatters.World.WeenieSQLWriter();
                var filename = sqlWriter.GetDefaultFileName(weenie).Replace(".sql", ".json");

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                File.WriteAllText(outputFolder + filename, jsonString);
            }
        }
    }
}
