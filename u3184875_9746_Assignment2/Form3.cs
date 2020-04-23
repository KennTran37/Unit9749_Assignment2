using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    public partial class Form3 : Form
    {
        Site site;

        public Form3(Site site)
        {
            InitializeComponent();

            this.site = site;
            ShowMaterialBoxes();

            pictureBox_Site.Image = IconPath.GetIcon(site.nodeType);
            label_SiteName.Text = site.name;

            for (int i = 0; i < site.currentAgents.Count; i++)
            {
                int yPoint = i > 1 ? i * 56 : 19;
                groupBox_Agents.Controls.Add(AgentBox(site.currentAgents[i], yPoint));
            }
        }

        GroupBox AgentBox(Agent agent, int yPoint)
        {
            GroupBox agentBox = new GroupBox();
            agentBox.Location = new Point(6, yPoint);
            agentBox.Size = new Size(200, 55);

            PictureBox icon = new PictureBox();
            agentBox.Controls.Add(icon);
            icon.Location = new Point(4, 10);
            icon.SizeMode = PictureBoxSizeMode.Zoom;
            icon.Image = IconPath.GetIcon(agent.currentJob.job);
            icon.Size = new Size(40, 40);

            Label name = new Label();
            agentBox.Controls.Add(name);
            name.Location = new Point(50, 10);
            name.Text = agent.name;

            ProgressBar bar = new ProgressBar();
            agentBox.Controls.Add(bar);
            bar.Location = new Point(50, 32);
            bar.Size = new Size(144, 12);
            agent.siteProgressBar = bar;

            return agentBox;
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
    }
}
