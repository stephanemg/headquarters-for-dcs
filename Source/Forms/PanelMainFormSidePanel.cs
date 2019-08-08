﻿using Headquarters4DCS.DefinitionLibrary;
using Headquarters4DCS.Template;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Headquarters4DCS.Forms
{
    public partial class PanelMainFormSidePanel : Form
    {
        private readonly FormMain MainForm = null;
        private MissionTemplate Template { get { return MainForm.Template; } }
        private string SelectedLocationID { get { return MainForm.SelectedLocationID; } }

        private DefinitionTheater Theater { get { return Library.Instance.GetDefinition<DefinitionTheater>(Template.Theater); } }

        private bool DisableTheaterUpdates = false;

        public PanelMainFormSidePanel(FormMain mainForm)
        {
            InitializeComponent();
            MainForm = mainForm;
        }

        private MissionTemplateLocation GetTemplateLocationFromTreeNode(TreeNode node)
        {
            if (node == null) return null;
            string definitionID = (node.Level > 0) ? node.Parent.Name : node.Name;
            if (!Template.Locations.ContainsKey(definitionID)) return null;
            return Template.Locations[definitionID];
        }

        private void Event_Form_Load(object sender, EventArgs e)
        {
            TemplateSettingsPropertyGrid.SelectedObject = Template.Settings;

            SidePanelimageList.Images.Add("airbase", GUITools.GetImageFromResource("MapIcons.airbase.png"));
            SidePanelimageList.Images.Add("airbase_blue", GUITools.GetImageFromResource("MapIcons.airbase_blue.png"));
            SidePanelimageList.Images.Add("airbase_red", GUITools.GetImageFromResource("MapIcons.airbase_red.png"));
            SidePanelimageList.Images.Add("location", GUITools.GetImageFromResource("MapIcons.location.png"));
        }

        public void UpdateTheater(TheaterUpdateType updateType)
        {
            if (DisableTheaterUpdates) return;

            TemplateSettingsPropertyGrid.Refresh();

            if (updateType == TheaterUpdateType.Full)
            {
                LocationsTreeView.Nodes.Clear();
                foreach (DefinitionTheaterLocation n in Theater.Locations.Values)
                    LocationsTreeView.Nodes.Add(n.ID, n.DisplayName);

                LocationsTreeView.Sort();
            }

            if (Theater == null) return;

            foreach (TreeNode n in LocationsTreeView.Nodes)
            {
                if ((updateType == TheaterUpdateType.SelectedLocation) && (n.Name != SelectedLocationID)) continue;

                n.Nodes.Clear();
                if (!Template.Locations.ContainsKey(n.Name)) continue;

                if (n.NodeFont != null) { n.NodeFont.Dispose(); n.NodeFont = null; }
                n.ImageKey = "location";

                if (Template.Locations[n.Name].Definition.LocationType == TheaterLocationType.Airbase)
                {
                    switch (Template.Locations[n.Name].Coalition)
                    {
                        default: n.ImageKey = "airbase"; break;
                        case CoalitionNeutral.Blue: n.ImageKey = "airbase_blue"; break;
                        case CoalitionNeutral.Red: n.ImageKey = "airbase_red"; break;
                    }
                }

                foreach (DefinitionFeature feature in Template.Locations[n.Name].GetFeaturesDefinitions())
                    n.Nodes.Add(feature.GetDisplayNameWithCategory()).Tag = feature.ID;
                // TODO: sort

                for (int i = 0; i < Template.Locations[n.Name].PlayerFlightGroups.Count; i++)
                    n.Nodes.Add(Template.Locations[n.Name].PlayerFlightGroups[i].ToString()).Tag = i;

                n.SelectedImageKey = n.ImageKey;
            }

            if (SelectedLocationID == null)
                LocationsTreeView.SelectedNode = null;
            else if (LocationsTreeView.Nodes.ContainsKey(SelectedLocationID))
            {
                LocationsTreeView.SelectedNode = LocationsTreeView.Nodes[SelectedLocationID];
                LocationsTreeView.Nodes[SelectedLocationID].EnsureVisible();
                LocationsTreeView.Nodes[SelectedLocationID].ExpandAll();
                MainTabControl.SelectedTab = LocationsTabPage;
                LocationsTreeView.Focus();
            }
        }

        private void Event_LocationsTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if ((e.Button != MouseButtons.Right) || (e.Node == null)) return;
            LocationsTreeView.SelectedNode = e.Node;

            LocationContextMenuStrip.Items.Clear();

            MissionTemplateLocation location = GetTemplateLocationFromTreeNode(LocationsTreeView.SelectedNode);
            if (location == null) return;

            if (LocationsTreeView.SelectedNode.Level == 0)
            {
                DefinitionFeature[] availableFeatures =
                    (from DefinitionFeature f in Library.Instance.GetAllDefinitions<DefinitionFeature>()
                     where f.FeatureLocationTypes.Contains(location.Definition.LocationType)
                     select f).ToArray();

                foreach (DefinitionFeature feature in availableFeatures)
                {
                    //if (!
                    ToolStripMenuItem featureParentMenuItem = AddItemToLocationContextMenuStrip(null, "Add feature");

                    AddItemToLocationContextMenuStrip(
                        featureParentMenuItem,
                        feature.GetDisplayNameWithCategory(), feature.ID);

                    // TODO: sort items
                }

                if (location.Definition.LocationType == TheaterLocationType.Airbase)
                {
                    AddItemToLocationContextMenuStrip(null, "Add player flight group", "*PLAYERFLIGHTGROUP*");

                    ToolStripMenuItem coalitionParentMenuItem = AddItemToLocationContextMenuStrip(null, "Coalition");
                    foreach (CoalitionNeutral coalitionID in (CoalitionNeutral[])Enum.GetValues(typeof(CoalitionNeutral)))
                        AddItemToLocationContextMenuStrip(coalitionParentMenuItem, coalitionID.ToString(), coalitionID, location.Coalition == coalitionID);
                }

            }
            else
            {
                if (LocationsTreeView.SelectedNode.Tag is int)
                    AddItemToLocationContextMenuStrip(null, "Remove player flight group", "*REMOVE*");
                else if (LocationsTreeView.SelectedNode.Tag is string)
                    AddItemToLocationContextMenuStrip(null, "Remove feature", "*REMOVE*");
            }

            LocationContextMenuStrip.Show(LocationsTreeView, e.Location);
        }

        private ToolStripMenuItem AddItemToLocationContextMenuStrip(ToolStripDropDownItem parent, string text, object tag = null, bool isChecked = false)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(text) { Checked = isChecked, Tag = tag };
            menuItem.Click += Event_ContextMenuItem_Click;

            if (parent == null)
                LocationContextMenuStrip.Items.Add(menuItem);
            else
                parent.DropDownItems.Add(menuItem);

            return menuItem;
        }

        private void Event_ContextMenuItem_Click(object sender, EventArgs e)
        {
            if (sender == null) return;
            ToolStripItem tsiSender = (ToolStripItem)sender;
            MissionTemplateLocation location = GetTemplateLocationFromTreeNode(LocationsTreeView.SelectedNode);
            if (location == null) return;

            if (tsiSender.Tag is CoalitionNeutral coalitionID)
                Template.Locations[location.Definition.ID].Coalition = coalitionID;
            else if (tsiSender.Tag is string stringTag)
            {
                if (stringTag == "*REMOVE*")
                {
                    if (LocationsTreeView.SelectedNode.Tag is string featureID)
                        Template.Locations[location.Definition.ID].Features.Remove(featureID);
                    else if (LocationsTreeView.SelectedNode.Tag is int playerFlightGroupIndex)
                        Template.Locations[location.Definition.ID].PlayerFlightGroups.RemoveAt(playerFlightGroupIndex);
                }
                else if (stringTag == "*PLAYERFLIGHTGROUP*")
                    Template.Locations[location.Definition.ID].PlayerFlightGroups.Add(MissionTemplatePlayerFlightGroup.CreateDefault());
                else // Default, tag is feature ID
                    Template.Locations[location.Definition.ID].Features.Add(stringTag);
            }

            MainForm.UpdateTheater(TheaterUpdateType.SelectedLocation);
        }

        private void Event_LocationsToolStripButtons_Click(object sender, EventArgs e)
        {
            if (sender == CollapseAllLocationsToolStripButton)
                LocationsTreeView.CollapseAll();
            else if (sender == ExpandAllLocationsToolStripButton)
                LocationsTreeView.ExpandAll();
        }

        private void Event_LocationsTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
                MainForm.SelectedLocationID = null;
            else
                MainForm.SelectedLocationID = GUITools.GetTopLevelNode(e.Node).Name;

            DisableTheaterUpdates = true;
            MainForm.UpdateTheater(TheaterUpdateType.SelectedLocation);
            DisableTheaterUpdates = false;
        }

        private void TemplateSettingsPropertyGrid_Click(object sender, EventArgs e)
        {

        }
    }
}
