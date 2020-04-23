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
    public partial class Form2 : Form
    {
        Color mainJobColor = Color.FromArgb(255, 69, 59);
        Color subJobColor = Color.FromArgb(49, 113, 170);
        Color defaultColor = SystemColors.Control;

        JobInfomation mainJob;
        List<JobInfomation> subJobs = new List<JobInfomation>();
        Agent agent;

        public Form2(in Agent agent)
        {
            InitializeComponent();

            this.agent = agent;
            mainJob = agent.mainJob;
            if (agent.subJobs != null)
                subJobs = agent.subJobs.ToList();
            else
                subJobs = new List<JobInfomation>();

            textBox_AgentName.Text = agent.name;

            HighlightMainJob(agent.mainJob);
            foreach (var job in subJobs)
                HighlightSubJob(job);

            DisplayMaterialBoxes();
        }

        void HighlightMainJob(JobInfomation job)
        {
            switch (job.jobType)
            {
                case Job.Carpenter:
                    groupBox_JobCarpenter.BackColor = mainJobColor;
                    trackBar_Carpenter.Value = job.skillLevel;
                    break;
                case Job.Logger:
                    groupBox_JobLogger.BackColor = mainJobColor;
                    trackBar_Logger.Value = job.skillLevel;
                    break;
                case Job.Blacksmith:
                    groupBox_JobBlacksmith.BackColor = mainJobColor;
                    trackBar_Blacksmith.Value = job.skillLevel;
                    break;
                case Job.Miner:
                    groupBox_JobMiner.BackColor = mainJobColor;
                    trackBar_Miner.Value = job.skillLevel;
                    break;
                case Job.Transporter:
                    groupBox_JobTransporter.BackColor = mainJobColor;
                    trackBar_Transporter.Value = job.skillLevel;
                    break;
                case Job.Constructor:
                    groupBox_JobConstructor.BackColor = mainJobColor;
                    trackBar_Constructor.Value = job.skillLevel;
                    break;
            }
        }

        void HighlightSubJob(JobInfomation job)
        {
            switch (job.jobType)
            {
                case Job.Carpenter:
                    groupBox_JobCarpenter.BackColor = subJobColor;
                    trackBar_Carpenter.Value = job.skillLevel;
                    break;
                case Job.Logger:
                    groupBox_JobLogger.BackColor = subJobColor;
                    trackBar_Logger.Value = job.skillLevel;
                    break;
                case Job.Blacksmith:
                    groupBox_JobBlacksmith.BackColor = subJobColor;
                    trackBar_Blacksmith.Value = job.skillLevel;
                    break;
                case Job.Miner:
                    groupBox_JobMiner.BackColor = subJobColor;
                    trackBar_Miner.Value = job.skillLevel;
                    break;
                case Job.Transporter:
                    groupBox_JobTransporter.BackColor = subJobColor;
                    trackBar_Transporter.Value = job.skillLevel;
                    break;
                case Job.Constructor:
                    groupBox_JobConstructor.BackColor = subJobColor;
                    trackBar_Constructor.Value = job.skillLevel;
                    break;
            }
        }

        #region TrackBar Event
        void SkillLevelChange(Job job, int index, TrackBar bar)
        {
            if (mainJob.jobType == job)
                mainJob.skillLevel = bar.Value;
            else if (SubJobsContains(job))
            {
                JobInfomation jobInfo = subJobs[index];
                jobInfo.skillLevel = bar.Value;
                subJobs[index] = jobInfo;
            }
        }

        private void trackBar_Blacksmith_Scroll(object sender, EventArgs e) => SkillLevelChange(Job.Blacksmith, subJobs.FindIndex(j => j.jobType == Job.Blacksmith), trackBar_Blacksmith);
        private void trackBar_Carpenter_Scroll(object sender, EventArgs e) => SkillLevelChange(Job.Carpenter, subJobs.FindIndex(j => j.jobType == Job.Carpenter), trackBar_Carpenter);
        private void trackBar_Logger_Scroll(object sender, EventArgs e) => SkillLevelChange(Job.Logger, subJobs.FindIndex(j => j.jobType == Job.Logger), trackBar_Logger);
        private void trackBar_Miner_Scroll(object sender, EventArgs e) => SkillLevelChange(Job.Miner, subJobs.FindIndex(j => j.jobType == Job.Miner), trackBar_Miner);
        private void trackBar_Transporter_Scroll(object sender, EventArgs e) => SkillLevelChange(Job.Transporter, subJobs.FindIndex(j => j.jobType == Job.Transporter), trackBar_Transporter);
        private void trackBar_Constructor_Scroll(object sender, EventArgs e) => SkillLevelChange(Job.Constructor, subJobs.FindIndex(j => j.jobType == Job.Constructor), trackBar_Constructor);
        #endregion

        #region SubJob Selection
        void UpdateSubJob(Job job, GroupBox box, TrackBar bar)
        {
            if (mainJob.jobType == job || mainJob.jobType == Job.Transporter)
                return;

            if (subJobs.Count == 0 || !SubJobsContains(job))
            {
                JobInfomation newJob = new JobInfomation(job, bar.Value);
                box.BackColor = subJobColor;
                subJobs.Add(newJob);
            }
            else
            {
                subJobs.Remove(subJobs.Single(s => s.jobType == job));
                box.BackColor = defaultColor;
            }

            DisplayMaterialBoxes();
        }

        private void SubJob_Blacksmith_Select(object sender, EventArgs e) => UpdateSubJob(Job.Blacksmith, groupBox_JobBlacksmith, trackBar_Blacksmith);
        private void SubJob_Carpenter_Select(object sender, EventArgs e) => UpdateSubJob(Job.Carpenter, groupBox_JobCarpenter, trackBar_Carpenter);
        private void SubJob_Logger_Select(object sender, EventArgs e) => UpdateSubJob(Job.Logger, groupBox_JobLogger, trackBar_Logger);
        private void SubJob_Miner_Select(object sender, EventArgs e) => UpdateSubJob(Job.Miner, groupBox_JobMiner, trackBar_Miner);
        private void SubJob_Transporter_Select(object sender, EventArgs e) => UpdateSubJob(Job.Transporter, groupBox_JobTransporter, trackBar_Transporter);
        private void SubJob_Constructor_Select(object sender, EventArgs e) => UpdateSubJob(Job.Constructor, groupBox_JobConstructor, trackBar_Constructor);

        bool SubJobsContains(Job job)
        {
            foreach (var item in subJobs)
                if (item.jobType == job)
                    return true;
            return false;
        }
        #endregion

        #region MainJob Selection
        void UpdateMainJob(Job job, GroupBox box, TrackBar bar)
        {
            if (mainJob.jobType == job)
                return;

            if (job == Job.Transporter)
            {
                subJobs.Clear();
                ClearAllJobs();
            }
            else if (SubJobsContains(job))
                subJobs.Remove(subJobs.Single(s => s.jobType == job));

            ClearOldMainJob(mainJob.jobType);
            mainJob = new JobInfomation(job, bar.Value);
            box.BackColor = mainJobColor;
            DisplayMaterialBoxes();
        }

        private void MainJob_BlackSmith_Select(object sender, EventArgs e) => UpdateMainJob(Job.Blacksmith, groupBox_JobBlacksmith, trackBar_Blacksmith);
        private void MainJob_Carpenter_Select(object sender, EventArgs e) => UpdateMainJob(Job.Carpenter, groupBox_JobCarpenter, trackBar_Carpenter);
        private void MainJob_Logger_Select(object sender, EventArgs e) => UpdateMainJob(Job.Logger, groupBox_JobLogger, trackBar_Logger);
        private void MainJob_Miner_Select(object sender, EventArgs e) => UpdateMainJob(Job.Miner, groupBox_JobMiner, trackBar_Miner);
        private void MainJob_Transporter_Select(object sender, EventArgs e) => UpdateMainJob(Job.Transporter, groupBox_JobTransporter, trackBar_Transporter);
        private void MainJob_Constructor_Select(object sender, EventArgs e) => UpdateMainJob(Job.Constructor, groupBox_JobConstructor, trackBar_Constructor);

        void ClearOldMainJob(Job job)
        {
            switch (job)
            {
                case Job.Carpenter:
                    groupBox_JobCarpenter.BackColor = defaultColor;
                    break;
                case Job.Logger:
                    groupBox_JobLogger.BackColor = defaultColor;
                    break;
                case Job.Blacksmith:
                    groupBox_JobBlacksmith.BackColor = defaultColor;
                    break;
                case Job.Miner:
                    groupBox_JobMiner.BackColor = defaultColor;
                    break;
                case Job.Transporter:
                    groupBox_JobTransporter.BackColor = defaultColor;
                    break;
                case Job.Constructor:
                    groupBox_JobConstructor.BackColor = defaultColor;
                    break;
            }
        }

        void ClearAllJobs()
        {
            groupBox_JobCarpenter.BackColor = defaultColor;
            groupBox_JobLogger.BackColor = defaultColor;
            groupBox_JobBlacksmith.BackColor = defaultColor;
            groupBox_JobMiner.BackColor = defaultColor;
            groupBox_JobTransporter.BackColor = defaultColor;
            groupBox_JobConstructor.BackColor = defaultColor;
        }
        #endregion

        //Check the jobs the agent has that uses the materials
        public void DisplayMaterialBoxes()
        {
            int yPoint = 87;
            //Ore
            if (HasJob(Job.Miner) || HasJob(Job.Transporter))
            {
                groupBox_Ore.Show();
                groupBox_Ore.Location = new Point(6, yPoint);
                numeric_MaxOre.Value = agent.inventory.ore.Max;
                numeric_CurOre.Value = agent.inventory.ore.Current;
                numeric_CurOre.Maximum = numeric_MaxOre.Value;
                yPoint += 45;
            }
            else
                groupBox_Ore.Hide();
            //Ingot
            if (HasJob(Job.Blacksmith) || HasJob(Job.Constructor) || HasJob(Job.Transporter))
            {
                groupBox_Ingot.Show();
                groupBox_Ingot.Location = new Point(6, yPoint);
                numeric_MaxIngot.Value = agent.inventory.ingot.Max;
                numeric_CurIngot.Value = agent.inventory.ingot.Current;
                numeric_CurIngot.Maximum = numeric_MaxIngot.Value;
                yPoint += 45;
            }
            else
                groupBox_Ingot.Hide();
            //Wood
            if (HasJob(Job.Logger) || HasJob(Job.Transporter))
            {
                groupBox_Wood.Show();
                groupBox_Wood.Location = new Point(6, yPoint);
                numeric_MaxWood.Value = agent.inventory.wood.Max;
                numeric_CurWood.Value = agent.inventory.wood.Current;
                numeric_CurWood.Maximum = numeric_MaxWood.Value;
                yPoint += 45;
            }
            else
                groupBox_Wood.Hide();
            //Plank
            if (HasJob(Job.Carpenter) || HasJob(Job.Constructor) || HasJob(Job.Transporter))
            {
                groupBox_Plank.Show();
                groupBox_Plank.Location = new Point(6, yPoint);
                numeric_MaxPlank.Value = agent.inventory.plank.Max;
                numeric_CurPlank.Value = agent.inventory.plank.Current;
                numeric_CurPlank.Maximum = numeric_MaxPlank.Value;
            }
            else
                groupBox_Plank.Hide();
        }

        bool HasJob(Job job)
        {
            if (mainJob.jobType == job)
                return true;
            foreach (var item in subJobs)
                if (item.jobType == job)
                    return true;
            return false;
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            agent.mainJob = mainJob;
            agent.subJobs = subJobs.ToArray();
            Form1.inst.UpdateAgent(agent);
        }

        private void textBox_AgentName_TextChanged(object sender, EventArgs e) => agent.name = textBox_AgentName.Text;

        #region Material Value Change
        private void numeric_CurOre_ValueChanged(object sender, EventArgs e) => agent.inventory.ore.Current = (int)numeric_CurOre.Value;
        private void numeric_MaxOre_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurOre.Maximum = numeric_MaxOre.Value;
            agent.inventory.ore.Max = (int)numeric_MaxOre.Value;
        }

        private void numeric_CurIngot_ValueChanged(object sender, EventArgs e) => agent.inventory.ingot.Current = (int)numeric_CurIngot.Value;
        private void numeric_MaxIngot_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurIngot.Maximum = numeric_MaxIngot.Value;
            agent.inventory.ingot.Max = (int)numeric_MaxIngot.Value;
        }

        private void numeric_CurWood_ValueChanged(object sender, EventArgs e) => agent.inventory.wood.Current = (int)numeric_CurWood.Value;
        private void numeric_MaxWood_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurWood.Maximum = numeric_MaxWood.Value;
            agent.inventory.wood.Max = (int)numeric_MaxWood.Value;
        }

        private void numeric_CurPlank_ValueChanged(object sender, EventArgs e) => agent.inventory.plank.Current = (int)numeric_CurPlank.Value;
        private void numeric_MaxPlank_ValueChanged(object sender, EventArgs e)
        {
            numeric_CurPlank.Maximum = numeric_MaxPlank.Value;
            agent.inventory.wood.Max = (int)numeric_MaxPlank.Value;
        }
        #endregion
    }
}
