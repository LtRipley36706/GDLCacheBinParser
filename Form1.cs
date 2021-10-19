using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;

using Microsoft.EntityFrameworkCore;
using PhatACCacheBinParser.ACE_Helpers;
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
	        txtACEDatabaseConnector.Text += $" completed. {Globals.ACEDatabase.WorldDatabase.GetWeenieCacheCount():N0} weenies cached." + Environment.NewLine;

            cmdACEDatabaseCacheAllWeenies.Enabled = true;
        }

        private void chkHidePassword_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.HideACEWorldPassword = chkHidePassword.Checked;
            Settings.Default.Save();
            txtACEWorldPassword.UseSystemPasswordChar = chkHidePassword.Checked;
        }

        private void cmdACE1RegionsParse_Click(object sender, EventArgs e)
        {
            cmdACE1RegionsParse.Enabled = false;

            var cacheRegion = Globals.CacheBin.RegionDescExtendedData.ConvertToACE(Globals.CacheBin.LandBlockData);

            var results = Globals.ACEDatabase.WorldDbContext.Encounter
                    .AsNoTracking()
                    .ToList();

            var x = new Dictionary<int, List<ACE.Database.Models.World.Encounter>>();

            foreach (var r in cacheRegion)
            {
                if (!x.TryAdd(r.Landblock, new List<ACE.Database.Models.World.Encounter> { r }))
                    x[r.Landblock].Add(r);
            }

            var y = new Dictionary<int, List<ACE.Database.Models.World.Encounter>>();

            foreach (var r in results)
            {
                if (!y.TryAdd(r.Landblock, new List<ACE.Database.Models.World.Encounter> { r }))
                    y[r.Landblock].Add(r);
            }

            var deDuped = new List<ACE.Database.Models.World.Encounter>();

            var deDupedIndex = new HashSet<int>();

            foreach (var q in results)
            {
                //q.LastModified = DateTime.Now;

                if (!x.ContainsKey(q.Landblock))
                {
                    deDuped.Add(q);
                    deDupedIndex.Add(q.Landblock);
                }
                else
                {
                    var a = x[q.Landblock];
                    var b = y[q.Landblock];

                    if (deDupedIndex.Contains(q.Landblock))
                        deDuped.Add(q);
                    else if (a.Count != b.Count)
                    {
                        deDuped.Add(q);
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
                            deDuped.Add(q);
                            deDupedIndex.Add(q.Landblock);
                        }
                    }
                }
            }

            deDuped.Clear();

            foreach (var q in results)
            {
                //q.LastModified = DateTime.Now;

                if (deDupedIndex.Contains(q.Landblock))
                    deDuped.Add(q);
            }

            if (deDuped.Count > 0)
                RegionDescSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\1 RegionDescExtendedData\\SQL\\", Globals.WeenieNames, true);

            cmdACE1RegionsParse.Enabled = true;
        }

        private void cmdACE2SpellsParse_Click(object sender, EventArgs e)
        {
            cmdACE2SpellsParse.Enabled = false;

            var cacheSpells = Globals.CacheBin.SpellTableExtendedData.ConvertToACE();

            var results = Globals.ACEDatabase.WorldDbContext.Spell
                    .AsNoTracking()
                    .ToList();

            var x = cacheSpells.ToDictionary(z => z.Id, z => z);

            var deDuped = new List<ACE.Database.Models.World.Spell>();

            foreach (var q in results)
            {
                //q.LastModified = DateTime.Now;

                if (x.ContainsKey(q.Id))
                {
                    var s = x[q.Id];

                    if (q.Align != s.Align || q.BaseIntensity != s.BaseIntensity || q.Boost != s.Boost || q.BoostVariance != s.BoostVariance || q.CreateOffsetOriginX != s.CreateOffsetOriginX || q.CreateOffsetOriginY != s.CreateOffsetOriginY || q.CreateOffsetOriginZ != s.CreateOffsetOriginZ
                        || q.CritFreq != s.CritFreq || q.CritMultiplier != s.CritMultiplier || q.DamageRatio != s.DamageRatio || q.DamageType != s.DamageType || q.DefaultLaunchAngle != s.DefaultLaunchAngle || q.Destination != s.Destination || q.DimsOriginX != s.DimsOriginX
                        || q.DimsOriginY != s.DimsOriginY || q.DimsOriginZ != s.DimsOriginZ || q.DispelSchool != s.DispelSchool || q.DotDuration != s.DotDuration || q.DrainPercentage != s.DrainPercentage || q.ElementalModifier != s.ElementalModifier || q.EType != s.EType
                        || q.IgnoreMagicResist != s.IgnoreMagicResist || q.ImbuedEffect != s.ImbuedEffect || q.Index != s.Index || q.Link != s.Link || q.LossPercent != s.LossPercent || q.MaxBoostAllowed != s.MaxBoostAllowed || q.MaxPower != s.MaxPower || q.MinPower != s.MinPower
                        || q.Name != s.Name || q.NonTracking != s.NonTracking || q.Number != s.Number || q.NumberVariance != s.NumberVariance || q.NumProjectiles != s.NumProjectiles || q.NumProjectilesVariance != s.NumProjectilesVariance || q.PaddingOriginX != s.PaddingOriginX
                        || q.PaddingOriginY != s.PaddingOriginY || q.PaddingOriginZ != s.PaddingOriginZ || q.PeturbationOriginX != s.PeturbationOriginX || q.PeturbationOriginY != s.PeturbationOriginY || q.PeturbationOriginZ != s.PeturbationOriginZ || q.PositionAnglesW != s.PositionAnglesW
                        || q.PositionAnglesX != s.PositionAnglesX || q.PositionAnglesY != s.PositionAnglesY || q.PositionAnglesZ != s.PositionAnglesZ || q.PositionObjCellId != s.PositionObjCellId || q.PositionOriginX != s.PositionOriginX || q.PositionOriginY != s.PositionOriginY
                        || q.PositionOriginZ != s.PositionOriginZ || q.PowerVariance != s.PowerVariance || q.Proportion != s.Proportion || q.SlayerCreatureType != s.SlayerCreatureType || q.SlayerDamageBonus != s.SlayerDamageBonus || q.Source != s.Source || q.SourceLoss != s.SourceLoss
                        || q.SpreadAngle != s.SpreadAngle || q.StatModKey != s.StatModKey || q.StatModType != s.StatModType || q.StatModVal != s.StatModVal || q.TransferBitfield != s.TransferBitfield || q.TransferCap != s.TransferCap || q.Variance != s.Variance || q.VerticalAngle != s.VerticalAngle
                        || q.Wcid != s.Wcid
                        )
                        deDuped.Add(q);
                }
                else
                    deDuped.Add(q);
            }

            SpellsSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\2 SpellTableExtendedData\\SQL\\", Globals.WeenieNames, true);

            cmdACE2SpellsParse.Enabled = true;
        }

        private void cmdACE3TreasureParse_Click(object sender, EventArgs e)
        {
            cmdACE3TreasureParse.Enabled = false;

            //var cacheSpells = Globals.CacheBin.SpellTableExtendedData.ConvertToACE();

            //var results = Globals.ACEDatabase.WorldDbContext.Spell
            //        .AsNoTracking()
            //        .ToList();

            //var x = cacheSpells.ToDictionary(z => z.Id, z => z);

            //var deDuped = new List<ACE.Database.Models.World.Spell>();

            //foreach (var q in results)
            //{
            //    q.LastModified = DateTime.Now;

            //    if (x.ContainsKey(q.Id))
            //    {
            //        var s = x[q.Id];

            //        if (q.Align != s.Align || q.BaseIntensity != s.BaseIntensity || q.Boost != s.Boost || q.BoostVariance != s.BoostVariance || q.CreateOffsetOriginX != s.CreateOffsetOriginX || q.CreateOffsetOriginY != s.CreateOffsetOriginY || q.CreateOffsetOriginZ != s.CreateOffsetOriginZ
            //            || q.CritFreq != s.CritFreq || q.CritMultiplier != s.CritMultiplier || q.DamageRatio != s.DamageRatio || q.DamageType != s.DamageType || q.DefaultLaunchAngle != s.DefaultLaunchAngle || q.Destination != s.Destination || q.DimsOriginX != s.DimsOriginX
            //            || q.DimsOriginY != s.DimsOriginY || q.DimsOriginZ != s.DimsOriginZ || q.DispelSchool != s.DispelSchool || q.DotDuration != s.DotDuration || q.DrainPercentage != s.DrainPercentage || q.ElementalModifier != s.ElementalModifier || q.EType != s.EType
            //            || q.IgnoreMagicResist != s.IgnoreMagicResist || q.ImbuedEffect != s.ImbuedEffect || q.Index != s.Index || q.Link != s.Link || q.LossPercent != s.LossPercent || q.MaxBoostAllowed != s.MaxBoostAllowed || q.MaxPower != s.MaxPower || q.MinPower != s.MinPower
            //            || q.Name != s.Name || q.NonTracking != s.NonTracking || q.Number != s.Number || q.NumberVariance != s.NumberVariance || q.NumProjectiles != s.NumProjectiles || q.NumProjectilesVariance != s.NumProjectilesVariance || q.PaddingOriginX != s.PaddingOriginX
            //            || q.PaddingOriginY != s.PaddingOriginY || q.PaddingOriginZ != s.PaddingOriginZ || q.PeturbationOriginX != s.PeturbationOriginX || q.PeturbationOriginY != s.PeturbationOriginY || q.PeturbationOriginZ != s.PeturbationOriginZ || q.PositionAnglesW != s.PositionAnglesW
            //            || q.PositionAnglesX != s.PositionAnglesX || q.PositionAnglesY != s.PositionAnglesY || q.PositionAnglesZ != s.PositionAnglesZ || q.PositionObjCellId != s.PositionObjCellId || q.PositionOriginX != s.PositionOriginX || q.PositionOriginY != s.PositionOriginY
            //            || q.PositionOriginZ != s.PositionOriginZ || q.PowerVariance != s.PowerVariance || q.Proportion != s.Proportion || q.SlayerCreatureType != s.SlayerCreatureType || q.SlayerDamageBonus != s.SlayerDamageBonus || q.Source != s.Source || q.SourceLoss != s.SourceLoss
            //            || q.SpreadAngle != s.SpreadAngle || q.StatModKey != s.StatModKey || q.StatModType != s.StatModType || q.StatModVal != s.StatModVal || q.TransferBitfield != s.TransferBitfield || q.TransferCap != s.TransferCap || q.Variance != s.Variance || q.VerticalAngle != s.VerticalAngle
            //            || q.Wcid != s.Wcid
            //            )
            //            deDuped.Add(q);
            //    }
            //    else
            //        deDuped.Add(q);
            //}

            var cacheTreasureDeath = Globals.CacheBin.TreasureTable.DeathTreasure.ConvertToACE();
            var cacheTreasureWielded = Globals.CacheBin.TreasureTable.WieldedTreasure.ConvertToACE();

            var treasureDeath = Globals.ACEDatabase.GetAllTreasureDeath();
            var treasureWielded = Globals.ACEDatabase.GetAllTreasureWielded();

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

            var deDupedTreasureDeath = new List<ACE.Database.Models.World.TreasureDeath>();

            foreach (var x in treasureDeath)
            {
                //x.LastModified = DateTime.Now;

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

            var deDupedTreasureWielded = new List<ACE.Database.Models.World.TreasureWielded>();

            foreach (var x in treasureWielded)
            {
                //x.LastModified = DateTime.Now;

                var y = cacheTreasureWielded.FirstOrDefault(z => z.TreasureType == x.TreasureType);

                if (y == null)
                    deDupedTreasureWielded.Add(x);
                else
                {
                    if (x.ContinuesPreviousSet != y.ContinuesPreviousSet || x.HasSubSet != y.HasSubSet || x.PaletteId != y.PaletteId || x.Probability != y.Probability || x.SetStart != y.SetStart || x.Shade != y.Shade || x.StackSize != y.StackSize || x.StackSizeVariance != y.StackSizeVariance
                        || x.TreasureType != y.TreasureType || x.Unknown1 != y.Unknown1 || x.Unknown10 != y.Unknown10 || x.Unknown11 != y.Unknown11 || x.Unknown12 != y.Unknown12 || x.Unknown3 != y.Unknown3 || x.Unknown4 != y.Unknown4 || x.Unknown5 != y.Unknown5 || x.Unknown9 != y.Unknown9
                        || x.WeenieClassId != y.WeenieClassId
                        )
                        deDupedTreasureWielded.Add(x);
                }
            }

            TreasureSQLWriter.WriteFiles(deDupedTreasureDeath, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\SQL\\Death\\", true);
            TreasureSQLWriter.WriteFiles(deDupedTreasureWielded, Settings.Default["GDLESQLOutputFolder"] + "\\3 TreasureTable\\SQL\\Wielded\\", Globals.WeenieNames, true);

            //SpellsSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\2 SpellTableExtendedData\\SQL\\", Globals.WeenieNames, true);

            cmdACE3TreasureParse.Enabled = true;
        }

        private void cmdACE4CraftingParse_Click(object sender, EventArgs e)
        {
            cmdACE4CraftingParse.Enabled = false;

            var cacheCraftingTables = Globals.CacheBin.CraftingTable.ConvertToACE();

            var cookBooks = Globals.ACEDatabase.WorldDbContext.CookBook
                    .AsNoTracking()
                    .ToList();

            var recipes = Globals.ACEDatabase.WorldDbContext.Recipe
                    .AsNoTracking()
                    .Include(r => r.RecipeMod)
                        .ThenInclude(r => r.RecipeModsBool)
                    .Include(r => r.RecipeMod)
                        .ThenInclude(r => r.RecipeModsDID)
                    .Include(r => r.RecipeMod)
                        .ThenInclude(r => r.RecipeModsFloat)
                    .Include(r => r.RecipeMod)
                        .ThenInclude(r => r.RecipeModsIID)
                    .Include(r => r.RecipeMod)
                        .ThenInclude(r => r.RecipeModsInt)
                    .Include(r => r.RecipeMod)
                        .ThenInclude(r => r.RecipeModsString)
                    .Include(r => r.RecipeRequirementsBool)
                    .Include(r => r.RecipeRequirementsDID)
                    .Include(r => r.RecipeRequirementsFloat)
                    .Include(r => r.RecipeRequirementsIID)
                    .Include(r => r.RecipeRequirementsInt)
                    .Include(r => r.RecipeRequirementsString)
                    .ToList();

            CraftingSQLWriter.WriteFiles(recipes, cookBooks, Globals.WeenieNames, Settings.Default["GDLESQLOutputFolder"] + "\\4 CraftTable\\SQL\\", true);

            cmdACE4CraftingParse.Enabled = true;
        }

        private void cmdACE5HousingParse_Click(object sender, EventArgs e)
        {
            cmdACE5HousingParse.Enabled = false;

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
                        if (b[0].AnglesW != a[0].AnglesW || b[0].AnglesX != a[0].AnglesX || b[0].AnglesY != a[0].AnglesY || b[0].AnglesZ != a[0].AnglesZ || b[0].ObjCellId != a[0].ObjCellId
                            || b[0].OriginX != a[0].OriginX || b[0].OriginY != a[0].OriginY || b[0].OriginZ != a[0].OriginZ)
                        {
                            deDuped.Add(q);
                        }
                        else if (b.Count == a.Count && b.Count == 2)
                        {
                            if (b[1].AnglesW != a[1].AnglesW || b[1].AnglesX != a[1].AnglesX || b[1].AnglesY != a[1].AnglesY || b[1].AnglesZ != a[1].AnglesZ || b[1].ObjCellId != a[1].ObjCellId
                            || b[1].OriginX != a[1].OriginX || b[1].OriginY != a[1].OriginY || b[1].OriginZ != a[1].OriginZ)
                            {
                                deDuped.Add(q);
                            }
                        }
                    }
                }
            }

            if (deDuped.Count > 0)
                HouseSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\5 HousingPortals\\SQL\\", true);

            cmdACE5HousingParse.Enabled = true;
        }

        private void cmdACE6LandblocksParse_Click(object sender, EventArgs e)
        {
            cmdACE6LandblocksParse.Enabled = false;

            var cachelandblockInstances = Globals.CacheBin.LandBlockData.ConvertToACE();

            var landblocks = Globals.ACEDatabase.GetAllLandblockInstances();

            //uint landblockToCloneFrom = 0x01C9;
            //uint landblockToCloneTo = 0x003C;
            //var landblocks = Globals.ACEDatabase.CloneLandblockToAnother(landblockToCloneFrom, landblockToCloneTo);

            var x = new Dictionary<uint, List<ACE.Database.Models.World.LandblockInstance>>();

            foreach (var h in cachelandblockInstances)
            {
                var lbid = h.ObjCellId >> 16;
                if (!x.TryAdd(lbid, new List<ACE.Database.Models.World.LandblockInstance> { h }))
                    x[lbid].Add(h);
            }

            var y = new Dictionary<uint, List<ACE.Database.Models.World.LandblockInstance>>();

            foreach (var h in landblocks)
            {
                var lbid = h.ObjCellId >> 16;
                if (!y.TryAdd(lbid, new List<ACE.Database.Models.World.LandblockInstance> { h }))
                    y[lbid].Add(h);
            }

            var deDuped = new List<ACE.Database.Models.World.LandblockInstance>();
            var deDupedIndex = new HashSet<uint>();

            foreach (var q in landblocks)
            {
                //q.LastModified = DateTime.Now;

                var lbid = q.ObjCellId >> 16;

                if (!x.ContainsKey(lbid))
                {
                    deDuped.Add(q);
                    deDupedIndex.Add(lbid);
                }
                else
                {
                    var a = x[lbid];
                    var b = y[lbid];

                    if (deDupedIndex.Contains(lbid))
                        deDuped.Add(q);
                    else if (a.Count != b.Count)
                    {
                        deDuped.Add(q);
                        deDupedIndex.Add(lbid);
                    }
                    else
                    {
                        var c = a.FirstOrDefault(l => l.Guid == q.Guid);
                        var d = b.FirstOrDefault(l => l.Guid == q.Guid);

                        if (c == null && d != null)
                        {
                            deDuped.Add(q);
                            deDupedIndex.Add(lbid);
                        }
                        else if (c.AnglesW != d.AnglesW || c.AnglesX != d.AnglesX || c.AnglesY != d.AnglesY || c.IsLinkChild != d.IsLinkChild || c.ObjCellId != d.ObjCellId || c.OriginX != d.OriginX || c.OriginY != d.OriginY || c.OriginZ != d.OriginZ || c.WeenieClassId != d.WeenieClassId)
                        {
                            deDuped.Add(q);
                            deDupedIndex.Add(lbid);
                        }
                    }
                }
            }

            deDuped.Clear();

            foreach (var q in landblocks)
            {
                //q.LastModified = DateTime.Now;

                var lbid = q.ObjCellId >> 16;

                if (deDupedIndex.Contains(lbid))
                    deDuped.Add(q);
            }

            LandblockSQLWriter.WriteFiles(landblocks, Settings.Default["GDLESQLOutputFolder"] + "\\6 LandBlockExtendedData\\SQL\\", Globals.WeenieNames, true);

            cmdACE6LandblocksParse.Enabled = true;
        }

        private void cmdACE8QuestsParse_Click(object sender, EventArgs e)
        {
            cmdACE8QuestsParse.Enabled = false;

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

            QuestSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\8 QuestDefDB\\SQL\\", true);

            cmdACE8QuestsParse.Enabled = true;
        }

        private void cmdACE9WeeniesParse_Click(object sender, EventArgs e)
        {
            cmdACE9WeeniesParse.Enabled = false;

            Globals.ACEDatabase.ReCacheAllWeeniesInParallel();

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

            WeenieSQLWriter.WriteFiles(Globals.ACEDatabase.Weenies, Settings.Default["GDLESQLOutputFolder"] + "\\9 WeenieDefaults\\SQL\\", Globals.WeenieNames, treasureWielded, treasureDeath, Globals.ACEDatabase.Weenies.ToDictionary(x => x.ClassId, x => x), true);

            cmdACE9WeeniesParse.Enabled = true;
        }

        private void cmdACEAMutationParse_Click(object sender, EventArgs e)
        {

        }

        private void cmdACEBEventsParse_Click(object sender, EventArgs e)
        {
            cmdACEBEventsParse.Enabled = false;

            var cacheEvents = Globals.CacheBin.GameEventDefDB.ConvertToACE();

            var results = Globals.ACEDatabase.WorldDbContext.Event
                    .AsNoTracking()
                    .ToList();

            var x = cacheEvents.ToDictionary(z => z.Name.ToLower(), z => z);

            var deDuped = new List<ACE.Database.Models.World.Event>();

            foreach (var q in results)
            {
                var z = q.Name.ToLower();

                //q.LastModified = DateTime.Now;

                if (x.ContainsKey(z))
                {
                    if (!q.Name.Equals(x[z].Name))
                        q.Name = x[z].Name;

                    if (x[z].StartTime != q.StartTime || x[z].EndTime != q.EndTime || x[z].State != q.State)
                        deDuped.Add(q);
                }
                else
                    deDuped.Add(q);
            }

            EventSQLWriter.WriteFiles(deDuped, Settings.Default["GDLESQLOutputFolder"] + "\\B GameEventDefDB\\SQL\\", true);

            cmdACEBEventsParse.Enabled = true;
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
