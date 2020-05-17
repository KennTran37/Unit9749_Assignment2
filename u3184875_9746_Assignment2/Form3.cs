using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    //Form3 is used to display the Site's Information
    //Showing it's inventory and the agents that are in the site
    public partial class Form3 : Form
    {
        Site site;
        List<CurrentAgentBox> agentBoxes = new List<CurrentAgentBox>();

        public Form3(Site site)
        {
            InitializeComponent();

            this.site = site;
            ShowMaterialBoxes();

            pictureBox_Site.Image = IconPath.GetIcon(site.nodeType);
            numeric_MaxWorkers.Value = site.maxAgents;
            label_SiteName.Text = site.name;

            this.site.refreshHandler += RefreshCurrentAgentList;

            for (int i = 0; i < site.maxAgents; i++)
            {
                CurrentAgentBox newAgentBox = new CurrentAgentBox(0);
                groupBox_Agents.Controls.Add(newAgentBox.groupBox);
                newAgentBox.groupBox.Hide();
                agentBoxes.Add(newAgentBox);
            }

            for (int i = 0; i < site.currentAgents.Count; i++)
            {
                int yPoint = i > 1 ? i * 56 : 19;
                UpdateListInfo(i, site.currentAgents[i], yPoint);
            }

            if (Form1.inst.IsRunning)
                DisableInteractions();
        }

        void RefreshCurrentAgentList()
        {
            for (int i = 0; i < agentBoxes.Count; i++)
            {
                if (i >= site.currentAgents.Count)
                {
                    agentBoxes[i].groupBox.Invoke(new Action(() => agentBoxes[i].groupBox.Hide()));
                    continue;
                }

                int yPoint = i > 0 ? i * 56 + 19 : 19;
                UpdateListInfo(i, site.currentAgents[i], yPoint);
            }
        }

        void UpdateListInfo(int i, Agent agent, int yPoint)
        {
            if (agentBoxes[i].groupBox.InvokeRequired)
            {
                agentBoxes[i].groupBox.Invoke(new Action(() =>
                {
                    agentBoxes[i].groupBox.Location = new Point(6, yPoint);
                    agentBoxes[i].groupBox.Show();
                }));
            }
            else
            {
                agentBoxes[i].groupBox.Location = new Point(6, yPoint);
                agentBoxes[i].groupBox.Show();
            }

            if (agentBoxes[i].jobIcon.InvokeRequired) agentBoxes[i].jobIcon.Invoke(new Action(() => agentBoxes[i].jobIcon.Image = agent.CurrentJob.jobIcon));
            else agentBoxes[i].jobIcon.Image = agent.CurrentJob.jobIcon;

            if (agentBoxes[i].agentName.InvokeRequired) agentBoxes[i].agentName.Invoke(new Action(() => agentBoxes[i].agentName.Text = agent.name));
            else agentBoxes[i].agentName.Text = agent.name;

            agent.form3Bar = agentBoxes[i].progressBar;
        }

        void DisableInteractions()
        {
            numeric_MaxWorkers.Enabled = false;

            numeric_CurIngot.Enabled = false;
            numeric_MaxIngot.Enabled = false;

            numeric_CurPlank.Enabled = false;
            numeric_MaxPlank.Enabled = false;

            numeric_CurWood.Enabled = false;
            numeric_MaxWood.Enabled = false;

            numeric_CurOre.Enabled = false;
            numeric_MaxOre.Enabled = false;
        }

        void ShowMaterialBoxes()
        {
            int yPoint = 96;
            if (Form1.inst.SiteHoldsOre(site.nodeType))
            {
                groupBox_Ore.Show();
                groupBox_Ore.Location = new Point(3, yPoint);
                numeric_MaxOre.Value = site.inventory.ore.Max;
                numeric_CurOre.Value = site.inventory.ore.Current;
                numeric_CurOre.Maximum = numeric_MaxOre.Value;
                yPoint += 43;
            }

            if (Form1.inst.SiteHoldsIngot(site.nodeType))
            {
                groupBox_Ingot.Show();
                groupBox_Ingot.Location = new Point(3, yPoint);
                numeric_MaxIngot.Value = site.inventory.ingot.Max;
                numeric_CurIngot.Value = site.inventory.ingot.Current;
                numeric_CurIngot.Maximum = numeric_MaxIngot.Value;
                yPoint += 43;
            }

            if (Form1.inst.SiteHoldsWood(site.nodeType))
            {
                groupBox_Wood.Show();
                groupBox_Wood.Location = new Point(3, yPoint);
                numeric_MaxWood.Value = site.inventory.wood.Max;
                numeric_CurWood.Value = site.inventory.wood.Current;
                numeric_CurWood.Maximum = numeric_MaxWood.Value;
                yPoint += 43;
            }

            if (Form1.inst.SiteHoldsPlank(site.nodeType))
            {
                groupBox_Plank.Show();
                groupBox_Plank.Location = new Point(3, yPoint);
                numeric_MaxPlank.Value = site.inventory.plank.Max;
                numeric_CurPlank.Value = site.inventory.plank.Current;
                numeric_CurPlank.Maximum = numeric_MaxPlank.Value;
            }
        }

        #region Materials Value Change
        private void numeric_CurOre_ValueChanged(object sender, EventArgs e) => site.inventory.ore.Current = (int)numeric_CurOre.Value;
        private void numeric_MaxOre_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurOre.Maximum = numeric_MaxOre.Value;
            site.inventory.ore.Max = (int)numeric_MaxOre.Value;
        }

        private void numeric_CurIngot_ValueChanged(object sender, EventArgs e) => site.inventory.ingot.Current = (int)numeric_CurIngot.Value;
        private void numeric_MaxIngot_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurIngot.Maximum = numeric_MaxIngot.Value;
            site.inventory.ingot.Max = (int)numeric_MaxIngot.Value;
        }

        private void numeric_CurWood_ValueChanged(object sender, EventArgs e) => site.inventory.wood.Current = (int)numeric_CurWood.Value;
        private void numeric_MaxWood_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurWood.Maximum = numeric_MaxWood.Value;
            site.inventory.wood.Max = (int)numeric_MaxWood.Value;
        }

        private void numeric_CurPlank_ValueChanged(object sender, EventArgs e) => site.inventory.plank.Current = (int)numeric_CurPlank.Value;
        private void numeric_MaxPlank_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurPlank.Maximum = numeric_MaxPlank.Value;
            site.inventory.plank.Max = (int)numeric_MaxPlank.Value;
        }
        #endregion

        private void numeric_MaxWorkers_ValueChanged(object sender, EventArgs e) => site.maxAgents = (int)numeric_MaxWorkers.Value;

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.site.refreshHandler -= RefreshCurrentAgentList;
        }
    }

    public struct CurrentAgentBox
    {
        public GroupBox groupBox;
        public PictureBox jobIcon;
        public Label agentName;
        public ProgressBar progressBar;

        public CurrentAgentBox(int yPoint)
        {
            groupBox = new GroupBox();
            groupBox.Location = new Point(6, yPoint);
            groupBox.Size = new Size(200, 55);

            jobIcon = new PictureBox();
            groupBox.Controls.Add(jobIcon);
            jobIcon.Location = new Point(4, 10);
            jobIcon.SizeMode = PictureBoxSizeMode.Zoom;
            jobIcon.Size = new Size(40, 40);

            agentName = new Label();
            groupBox.Controls.Add(agentName);
            agentName.Location = new Point(50, 10);
            agentName.Size = new Size(66, 13);

            progressBar = new ProgressBar();
            groupBox.Controls.Add(progressBar);
            progressBar.Location = new Point(50, 32);
            progressBar.Size = new Size(144, 12);
            progressBar.Maximum = 5;
        }
    }
}
