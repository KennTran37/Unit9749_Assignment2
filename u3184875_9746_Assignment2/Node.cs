using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    public class Node
    {
        public string name = null;
        public NodeType nodeType;

        public Node(string nodeName, NodeType location)
        {
            name = nodeName;
            nodeType = location;
        }
    }

    public class Site : Node
    {
        public SiteListBox listBox;
        public int maxAgents = 5;
        public List<Agent> currentAgents;

        public Inventory inventory;

        public Site(string name, NodeType nodeType, int maxAgents) : base(name, nodeType)
        {
            currentAgents = new List<Agent>();
            this.maxAgents = maxAgents;
        }

        public void DisplaySiteInformation(object sender, EventArgs e)
        {
            if (!Form1.inst.SiteFormAlreadyOpened(name))
            {
                Form3 form3 = new Form3(this);
                form3.Name = $"Form3_{name}";
                form3.Show();
            }
        }

        public bool HasSpace() => currentAgents.Count < maxAgents;
    }

    public struct SiteListBox
    {
        public GroupBox siteBox;
        public PictureBox siteIcon;
        public Label name;
        public Label workers;

        public SiteListBox(GroupBox siteBox, PictureBox siteIcon, Label name, Label workers)
        {
            this.siteBox = siteBox;
            this.siteIcon = siteIcon;
            this.name = name;
            this.workers = workers;
        }
    }

    public struct MaterialBox
    {
        public GroupBox materialBox;
        public PictureBox icon;
        public Label count;

        int current, max;
        public int Max
        {
            get => max; set
            {
                max = value;
                if (count != null)
                    SetCountText();
            }
        }
        public int Current
        {
            get => current; set
            {
                current = value;
                if (count != null)
                    SetCountText();
            }
        }

        void SetCountText()
        {
            if (count.InvokeRequired)
                count.Invoke(new Action(SetCountText));
            else
                count.Text = $"{current}/{Max}";
        }

        public MaterialBox(GroupBox materialBox, PictureBox icon, Label count) : this()
        {
            this.materialBox = materialBox;
            this.icon = icon;
            this.count = count;
            Max = 20;
        }

        public MaterialBox(GroupBox materialBox, PictureBox icon, Label count, int current, int max) : this(materialBox, icon, count)
        {
            this.current = current;
            this.max = max;
        }

        public bool HasSpace() => current < max;
        public bool HasAmount(int value) => current > value;

        public bool TryPutInMaterial()
        {
            if (current < max)
            {
                Current++;
                return true;
            }
            return false;
        }
    }
}
