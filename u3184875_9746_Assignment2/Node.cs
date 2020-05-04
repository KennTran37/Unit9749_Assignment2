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

            inventory = new Inventory();
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

    //holds the form elements which will display the site's infomation
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

    public class Inventory
    {
        public MaterialBox wood;
        public MaterialBox plank;
        public MaterialBox ore;
        public MaterialBox ingot;

        public Inventory()
        {
            wood = new MaterialBox();
            plank = new MaterialBox();
            ore = new MaterialBox();
            ingot = new MaterialBox();
        }

        public Inventory(int current, int max)
        {
            wood = new MaterialBox(MaterialType.Wood, current, max);
            plank = new MaterialBox(MaterialType.Plank, current, max);
            ore = new MaterialBox(MaterialType.Ore, current, max);
            ingot = new MaterialBox(MaterialType.Ingot, current, max);
        }

        public Inventory(Inventory invent)
        {
            wood = invent.wood;
            plank = invent.plank;
            ingot = invent.ingot;
            ore = invent.ore;
        }
    }

    public struct MaterialBox
    {
        public MaterialType materialType;
        public GroupBox materialBox;
        public PictureBox icon;
        public Label label;

        private int current;
        private int max;

        //used to assign site inventory
        public MaterialBox(GroupBox materialBox, PictureBox icon, Label label, MaterialType type) : this()
        {
            this.materialBox = materialBox;
            this.icon = icon;
            this.label = label;
            materialType = type;
            Max = 20;
        }

        //used to assign agent inventory
        public MaterialBox(MaterialType type, int current, int max)
        {
            materialBox = null;
            icon = null;
            label = null;
            materialType = type;
            this.current = current;
            this.max = max;
        }

        public int Max
        {
            get => max; set
            {
                max = value;
                if (label != null)
                    SetCountText();
            }
        }
        public int Current
        {
            get => current; set
            {
                current = value;
                if (label != null)
                    SetCountText();
            }
        }

        void SetCountText()
        {
            if (label.InvokeRequired)
                label.Invoke(new Action(SetCountText));
            else
                label.Text = $"{current}/{max}";
        }

        public bool HasSpace() => current < max;
        public bool HasAmount(int value) => current >= value;

        public bool TryPutInMaterial()
        {
            if (current < max)
            {
                Current++;
                return true;
            }
            return false;
        }

        public bool TryTakeOutMaterial()
        {
            if (current > 0)
            {
                Current--;
                return true;
            }
            return false;
        }
    }
}
